using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AtlaS
{
    public sealed class AtlasPackerUtil
    {
        public static string DefaultAtlasAssetName = "_atlas.asset";

        public static int MaxNumberOfSpriteInAtlas = 65535;

        public static AtlasRaw PackAssetFolder(AtlasPackData packData, string texFolder)
        {
            var textures = AssetDatabase.FindAssets("t:Texture2D", new string[] { texFolder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => new AtlasPackSprite(path)).ToArray();
            if (textures.Length == 0) return null;
            for (int i = 0; i < textures.Length; i++)
            {
                var texPath = textures[i].path;
                var assetPath = texPath.Replace(texFolder + "/", "");
                var assetDir = Path.GetDirectoryName(assetPath);
                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                var assetLabel = string.IsNullOrEmpty(assetDir) ? assetName : assetDir + "/" + assetName;
                textures[i].name = assetLabel;
                var spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
                textures[i].border = spriteAsset != null ? spriteAsset.border : Vector4.zero;
                textures[i].pivot = spriteAsset != null ? spriteAsset.pivot : new Vector2(0.5f, 0.5f);
                textures[i].rawQuality = BinRaw.Quality.Full;
                if (CheckTextureCompressed(texPath)) textures[i].quality = BinRaw.Quality.Normal;
                var texRect = textures[i].rect;
                if (EditorUtility.DisplayCancelableProgressBar("", 
                    string.Format("load texture: {0} [{1}x{2}]", assetLabel, (int)texRect.width, (int)texRect.height), 
                    (float)i / textures.Length))
                {
                    return null;
                }
            }
            EditorUtility.ClearProgressBar();
            Array.Sort(textures, SortAtlasTextures);
            var atlasRaw = PackTextures(packData, textures);
            if (atlasRaw == null) return null;
            EditorUtility.DisplayProgressBar("", "generate atlas...", 0.5f);
            var atlasPath = Path.Combine(texFolder, DefaultAtlasAssetName);
            atlasPath = AssetDatabase.GenerateUniqueAssetPath(atlasPath);
            SaveAtlasTextures(atlasRaw, atlasPath);
            atlasRaw = SaveAtlas(atlasRaw, atlasPath);
            if (packData.removeFragmentWhenPackingOver)
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    var texPath = textures[i].path;
                    File.Delete(texPath);
                    if (EditorUtility.DisplayCancelableProgressBar("", "delete texture: " + texPath, (float)i / textures.Length))
                    {
                        break;
                    }
                }
                AssetDatabase.Refresh();
            }
            Selection.activeObject = atlasRaw;
            EditorUtility.ClearProgressBar();
            return atlasRaw;
        }

        public static AtlasRaw PackTextures(AtlasPackData packData, AtlasPackSprite[] textures)
        {
            var sizeList = AtlasPacker.AtlasSizeList;
            var maxSize = packData.maxAtlasSize;
            var padding = packData.padding;
            var isPOT = packData.isPOT;
            var forceSquare = packData.forceSquare;
            if (textures.Length > MaxNumberOfSpriteInAtlas)
            {
                Debug.LogError(string.Format("Sprites in excess of {0}.", MaxNumberOfSpriteInAtlas));
                return null;
            }
            for (int i = 0; i < textures.Length; i++)
            {
                var texName = textures[i].name;
                var texAsset = textures[i].main;
                var texRect = textures[i].rect;
                if (texRect.width > maxSize ||
                    texRect.height > maxSize)
                {
                    Debug.LogError(string.Format("Texture size should be lower than {0}: {1}.", maxSize, texName), texAsset);
                    return null;
                }
            }

            var groups = textures.GroupBy(texture => new { texture.quality, texture.transparency })
                .OrderBy(group => group.Key.quality)
                .ThenBy(group => group.Key.transparency);
            var pieces = groups.Select(group =>
            {
                AtlasRaw atlasRaw;
                List<SpriteRaw> spriteRaws;
                var quality = group.Key.quality;
                var transparency = group.Key.transparency;
                var texList = group.ToArray();
                var texAreas = texList.Select(texture => new AtlasPacker.Area((int)texture.rect.width, (int)texture.rect.height)).ToArray();
                var groupPackData = new AtlasPackData(packData);
                if (quality == BinRaw.Quality.RGB16A4 || 
                    quality == BinRaw.Quality.Normal)
                {
                    groupPackData.isPOT = true;
                    groupPackData.forceSquare = true;
                }
                if (!PackTextures(groupPackData, texAreas, out atlasRaw, out spriteRaws))
                {
                    Debug.LogError("Pack failed.");
                    return null;
                }
                for (int i = 0; i < texList.Length; i++)
                {
                    var texAsset = texList[i].main;
                    var texAdd = texList[i].addition;
                    var texName = texList[i].name;
                    var texRect = texList[i].rect;
                    var spriteRaw = spriteRaws[i];
                    spriteRaw.name = texName;
                    spriteRaw.id = texList[i].id;
                    spriteRaw.border = texList[i].border;
                    spriteRaw.pivot = texList[i].pivot;
                    var rect = spriteRaw.rect;
                    var binRaw = atlasRaw.bins[spriteRaw.bin];
                    binRaw.quality = quality;
                    binRaw.sprites = binRaw.sprites ?? new SpriteRaw[0];
                    Array.Resize(ref binRaw.sprites, binRaw.sprites.Length + 1);
                    binRaw.sprites[binRaw.sprites.Length - 1] = spriteRaw;
                    var main = binRaw.main;
                    var add = binRaw.addition;
                    var colors = texAsset.GetPixels(
                        (int)texRect.x, (int)texRect.y,
                        (int)texRect.width, (int)texRect.height);
                    switch (texList[i].rawQuality)
                    {
                        case BinRaw.Quality.RGB16A4:
                        case BinRaw.Quality.Normal:
                            {
                                if (transparency && texAdd != null)
                                {
                                    var alphas = texAdd.GetPixels(
                                        (int)texRect.x, (int)texRect.y,
                                        (int)texRect.width, (int)texRect.height);
                                    for (int c = 0; c < colors.Length; c++)
                                    {
                                        var color = colors[c];
                                        color.a = alphas[c].r;
                                        colors[c] = color;
                                    }
                                }
                                break;
                            }
                    }
                    switch (quality)
                    {
                        case BinRaw.Quality.RGB16A4:
                        case BinRaw.Quality.Normal:
                            {
                                if (main == null)
                                {
                                    main = CreateBlankTexture((int)binRaw.size.x, (int)binRaw.size.y, TextureFormat.RGB24);
                                    main.hideFlags = HideFlags.DontUnloadUnusedAsset;
                                    binRaw.main = main;
                                }
                                if (transparency && add == null)
                                {
                                    add = CreateBlankTexture((int)binRaw.size.x, (int)binRaw.size.y, TextureFormat.RGB24);
                                    add.hideFlags = HideFlags.DontUnloadUnusedAsset;
                                    binRaw.addition = add;
                                }
                                main.SetPixels(
                                    (int)rect.x, (int)rect.y,
                                    (int)rect.width, (int)rect.height,
                                    colors);
                                if (transparency)
                                {
                                    for (int c = 0; c < colors.Length; c++)
                                    {
                                        colors[c] = new Color(colors[c].a, 0, 0);
                                    }
                                    add.SetPixels(
                                        (int)rect.x, (int)rect.y,
                                        (int)rect.width, (int)rect.height,
                                        colors);
                                }
                                break;
                            }
                        case BinRaw.Quality.Full:
                        case BinRaw.Quality.Legacy:
                            {
                                if (main == null)
                                {
                                    main = CreateBlankTexture((int)binRaw.size.x, (int)binRaw.size.y, transparency ? TextureFormat.RGBA32 : TextureFormat.RGB24);
                                    main.hideFlags = HideFlags.DontUnloadUnusedAsset;
                                    binRaw.main = main;
                                }
                                main.SetPixels(
                                    (int)rect.x, (int)rect.y,
                                    (int)rect.width, (int)rect.height,
                                    colors);
                                break;
                            }
                    }
                    if (EditorUtility.DisplayCancelableProgressBar("", "copy texture: " + texName, (float)i / textures.Length))
                    {
                        return null;
                    }
                }
                EditorUtility.ClearProgressBar();
                return atlasRaw;
            });

            var binList = new List<BinRaw>();
            foreach (var piece in pieces)
            {
                binList.AddRange(piece.bins);
            }
            var combined = ScriptableObject.CreateInstance<AtlasRaw>();
            combined.hideFlags = HideFlags.DontUnloadUnusedAsset;
            combined.bins = binList.ToArray();
            combined.maxSize = maxSize;
            combined.padding = padding;
            combined.isPOT = isPOT;
            combined.forceSquare = forceSquare;
            GenerateAtlasData(combined);
            return combined;
        }

        private static void GenerateAtlasData(AtlasRaw atlasRaw)
        {
            var spriteList = new List<SpriteRaw>();
            var consumedIds = new HashSet<int>();
            var bins = atlasRaw.bins;
            for (int i = 0; i < bins.Length; i++)
            {
                foreach (var sprite in bins[i].sprites)
                {
                    sprite.bin = i;
                    spriteList.Add(sprite);
                    if (sprite.id > 0)
                    {
                        consumedIds.Add(sprite.id);
                    }
                }
            }
            foreach (var sprite in spriteList)
            {
                if (sprite.id == 0)
                {
                    ushort id = 1;
                    while (id++ < MaxNumberOfSpriteInAtlas)
                    {
                        if (!consumedIds.Contains(id))
                        {
                            consumedIds.Add(id);
                            sprite.id = id;
                            break;
                        }
                    }
                }
            }
            var repeatIds = new HashSet<int>();
            foreach (var sprite in spriteList)
            {
                if (repeatIds.Contains(sprite.id))
                {
                    ushort id = 1;
                    while (id++ < MaxNumberOfSpriteInAtlas)
                    {
                        if (!consumedIds.Contains(id))
                        {
                            consumedIds.Add(id);
                            sprite.id = id;
                            break;
                        }
                    }
                }
                else
                {
                    repeatIds.Add(sprite.id);
                }
            }
            if (atlasRaw.id == 0)
            {
                atlasRaw.id = UnityEngine.Random.Range(1, int.MaxValue);
            }
        }

        public static bool PackTextures(AtlasPackData packData, AtlasPacker.Area[] textures, out AtlasRaw atlasRaw, out List<SpriteRaw> spriteRaws)
        {
            atlasRaw = null;
            spriteRaws = new List<SpriteRaw>();
            var packers = new List<AtlasPacker>();
            var sizeList = AtlasPacker.AtlasSizeList;
            var maxSize = packData.maxAtlasSize;
            var padding = packData.padding;
            var isPOT = packData.isPOT;
            var forceSquare = packData.forceSquare;
            var index = 0;
            while (index < textures.Length)
            {
                var texArea = textures[index];
                if (texArea.width > maxSize ||
                    texArea.height > maxSize)
                {
                    return false;
                }
                AtlasPacker.Rect texRect = null;
                for (int i = 0; i < packers.Count; i++)
                {
                    var packer = packers[i];
                    while (true)
                    {
                        if (packer.Push(texArea, out texRect))
                        {
                            spriteRaws.Add(new SpriteRaw()
                            {
                                bin = i,
                                rect = new Rect(texRect.x, texRect.y, texRect.width, texRect.height),
                            });
                            break;
                        }
                        var width = packer.bin.width;
                        var height = packer.bin.height;
                        if (forceSquare)
                        {
                            width *= 2;
                            height *= 2;
                        }
                        else
                        {
                            if (width > height) height *= 2;
                            else width *= 2;
                        }
                        if (width > maxSize || height > maxSize) break;
                        packer.Extend(new AtlasPacker.Bin(width, height));
                    }
                    if (texRect != null)
                    {
                        break;
                    }
                }
                if (texRect == null)
                {
                    packers.Add(new AtlasPacker(AtlasPacker.Algorithm.AdvancedHorizontalSkyline,
                        new AtlasPacker.Bin(sizeList[0], sizeList[0]), padding));
                }
                else
                {
                    index += 1;
                }
            }

            var bins = new BinRaw[packers.Count];
            for (int i = 0; i < packers.Count; i++)
            {
                bins[i] = new BinRaw();
                bins[i].size = !isPOT && !forceSquare ? 
                    Vector2.zero : new Vector2(packers[i].bin.width, packers[i].bin.height);
            }
            if (!isPOT && !forceSquare)
            {
                for (int i = 0; i < spriteRaws.Count; i++)
                {
                    var sprite = spriteRaws[i];
                    var bin = bins[sprite.bin];
                    bin.size = new Vector2(
                        Mathf.Max(bin.size.x, sprite.rect.xMax),
                        Mathf.Max(bin.size.y, sprite.rect.yMax));
                }
                // 为保证能够使用ETC2，这里强制扩充贴图宽高为4的倍数
                foreach (var bin in bins)
                {
                    bin.size = new Vector2(
                        Mathf.CeilToInt(bin.size.x / 4) * 4,
                        Mathf.CeilToInt(bin.size.y / 4) * 4);
                }
            }
            atlasRaw = ScriptableObject.CreateInstance<AtlasRaw>();
            atlasRaw.hideFlags = HideFlags.DontUnloadUnusedAsset;
            atlasRaw.maxSize = maxSize;
            atlasRaw.padding = padding;
            atlasRaw.isPOT = isPOT;
            atlasRaw.forceSquare = forceSquare;
            atlasRaw.bins = bins;

            return true;
        }

        public static AtlasRaw Repack(AtlasRaw atlasRaw, AtlasPackSprite[] textures = null, AtlasPackData packData = null)
        {
            if (textures == null)
            {
                textures = AtlasPackSprite.ListSprites(atlasRaw);
            }
            if (packData == null)
            {
                packData = new AtlasPackData(atlasRaw.maxSize, atlasRaw.padding, atlasRaw.isPOT, atlasRaw.forceSquare, false);
            }
            Array.Sort(textures, SortAtlasTextures);
            PrepareAtlasTextures(atlasRaw);
            var newAtlasRaw = PackTextures(packData, textures.ToArray());
            if (newAtlasRaw == null)
            {
                RevertAtlasTextures(atlasRaw);
                Debug.LogError("Pack failed.");
                return null;
            }

            EditorUtility.DisplayProgressBar("", "rebuild atlas...", 0.5f);
            RemoveAtlasTextures(atlasRaw);
            var atlasPath = AssetDatabase.GetAssetPath(atlasRaw);
            SaveAtlasTextures(newAtlasRaw, atlasPath);
            atlasRaw.bins = newAtlasRaw.bins;
            atlasRaw.maxSize = packData.maxAtlasSize;
            atlasRaw.padding = packData.padding;
            atlasRaw.isPOT = packData.isPOT;
            atlasRaw.forceSquare = packData.forceSquare;
            GenerateAtlasData(atlasRaw);
            EditorUtility.SetDirty(atlasRaw);
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();
            return atlasRaw;
        }

        public static void Export(AtlasRaw atlas, SpriteRaw[] sprites, string folder)
        {
            PrepareAtlasTextures(atlas);
            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var bin = atlas.bins[sprite.bin];
                var rect = sprite.rect;
                var texture = CreateBlankTexture((int)rect.width, (int)rect.height, bin.transparency ? TextureFormat.RGBA32 : TextureFormat.RGB24);
                var colors = bin.main.GetPixels(
                    (int)rect.x, (int)rect.y,
                    (int)rect.width, (int)rect.height);
                if ((bin.quality == BinRaw.Quality.RGB16A4 ||
                    bin.quality == BinRaw.Quality.Normal) &&
                    bin.transparency && 
                    bin.addition != null)
                {
                    var alphas = bin.addition.GetPixels(
                         (int)rect.x, (int)rect.y,
                         (int)rect.width, (int)rect.height);
                    for (int c = 0; c < colors.Length; c++)
                    {
                        var color = colors[c];
                        color.a = alphas[c].r;
                        colors[c] = color;
                    }
                }
                texture.SetPixels(colors);
                var bytes = bin.transparency ? texture.EncodeToPNG() : texture.EncodeToJPG();
                var path = Path.Combine(folder, sprite.name) + (bin.transparency ? ".png" : ".jpg");
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, bytes);
                if (EditorUtility.DisplayCancelableProgressBar("", "export: " + sprite.name, (float)i / sprites.Length))
                {
                    break;
                }
            }
            RevertAtlasTextures(atlas);
        }

        private static Texture2D CreateBlankTexture(int width, int height, TextureFormat format)
        {
            var transparentColor = new Color(0, 0, 0, 0);
            var texture = new Texture2D(width, height, format, false);
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(x, y, transparentColor);
                }
            }
            return texture;
        }

        private static void PrepareAtlasTextures(AtlasRaw atlasRaw)
        {
            var texAssets = new List<Texture2D>();
            foreach (var binRaw in atlasRaw.bins)
            {
                if (binRaw.main != null) texAssets.Add(binRaw.main);
                if (binRaw.addition != null) texAssets.Add(binRaw.addition);
            }
            for (int i = 0; i < texAssets.Count; i++)
            {
                var texAsset = texAssets[i];
                var texPath = AssetDatabase.GetAssetPath(texAsset);
                var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (importer != null && (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed))
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                    {
                        name = "Standalone",
                        overridden = false,
                    });
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                    {
                        name = "iPhone",
                        overridden = false,
                    });
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                    {
                        name = "Android",
                        overridden = false,
                    });
                    importer.SaveAndReimport();
                }
            }
        }

        private static void RevertAtlasTextures(AtlasRaw atlasRaw)
        {
            for (int i = 0; i < atlasRaw.bins.Length; i++)
            {
                var binRaw = atlasRaw.bins[i];
                if (binRaw.main != null)
                {
                    var binPath = AssetDatabase.GetAssetPath(binRaw.main);
                    UpdateBinTextureSetttings(binPath, (int)(Mathf.Max(binRaw.size.x, binRaw.size.y)), binRaw.quality, binRaw.transparency);
                }
                if ((binRaw.quality == BinRaw.Quality.RGB16A4 ||
                    binRaw.quality == BinRaw.Quality.Normal) &&
                    binRaw.transparency && binRaw.addition != null)
                {
                    var binPath = AssetDatabase.GetAssetPath(binRaw.addition);
                    UpdateBinTextureSetttings(binPath, (int)(Mathf.Max(binRaw.size.x, binRaw.size.y)), BinRaw.Quality.Normal, false);
                }
            }
        }

        private static void SaveAtlasTextures(AtlasRaw atlasRaw, string atlasPath)
        {
            var atlasFolder = Path.GetDirectoryName(atlasPath);
            var atlasName = Path.GetFileNameWithoutExtension(atlasPath);
            for (int i = 0; i < atlasRaw.bins.Length; i++)
            {
                var binRaw = atlasRaw.bins[i];
                var transparency = binRaw.transparency;
                if (binRaw.main != null)
                {
                    var binBytes = binRaw.main.EncodeToPNG();
                    var binPath = Path.Combine(atlasFolder, atlasName + "_" + i + ".png");
                    File.WriteAllBytes(binPath, binBytes);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    UnityEngine.Object.DestroyImmediate(binRaw.main);
                    binRaw.main = AssetDatabase.LoadAssetAtPath<Texture2D>(binPath);
                    UpdateBinTextureSetttings(binPath, (int)(Mathf.Max(binRaw.size.x, binRaw.size.y)), binRaw.quality, transparency);
                }
                if ((binRaw.quality == BinRaw.Quality.RGB16A4 || 
                    binRaw.quality == BinRaw.Quality.Normal) && 
                    binRaw.transparency && binRaw.addition != null)
                {
                    var binBytes = binRaw.addition.EncodeToPNG();
                    var binPath = Path.Combine(atlasFolder, atlasName + "_" + i + "_add.png");
                    File.WriteAllBytes(binPath, binBytes);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    UnityEngine.Object.DestroyImmediate(binRaw.addition);
                    binRaw.addition = AssetDatabase.LoadAssetAtPath<Texture2D>(binPath);
                    UpdateBinTextureSetttings(binPath, (int)(Mathf.Max(binRaw.size.x, binRaw.size.y)), BinRaw.Quality.Normal, false);
                }
            }
        }

        public static AtlasRaw SaveAtlas(AtlasRaw atlasRaw, string assetPath)
        {
            AssetDatabase.CreateAsset(atlasRaw, assetPath);
            return AssetDatabase.LoadAssetAtPath<AtlasRaw>(assetPath);
        }

        private static void RemoveAtlasTextures(AtlasRaw atlasRaw)
        {
            var texAssets = new List<Texture2D>();
            foreach (var bin in atlasRaw.bins)
            {
                if (bin.main != null) texAssets.Add(bin.main);
                if (bin.addition != null) texAssets.Add(bin.addition);
            }
            foreach (var texAsset in texAssets)
            {
                var texPath = AssetDatabase.GetAssetPath(texAsset);
                if (!string.IsNullOrEmpty(texPath))
                {
                    File.Delete(texPath);
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static int SortAtlasTextures(AtlasPackSprite aps1, AtlasPackSprite aps2)
        {
            if (aps1.rect.width != aps2.rect.width)
            {
                return (int)aps2.rect.width - (int)aps1.rect.width;
            }
            else if (aps1.rect.height != aps2.rect.height)
            {
                return (int)aps2.rect.height - (int)aps1.rect.height;
            }
            else
            {
                return string.Compare(aps1.name, aps2.name);
            }
        }

        private static void UpdateBinTextureSetttings(string assetPath, int maxSize, BinRaw.Quality quality, bool transparency)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            importer.isReadable = false;
            importer.maxTextureSize = NPOTScale(maxSize);
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            switch (quality)
            {
                case BinRaw.Quality.Normal:
                    {
                        importer.alphaIsTransparency = false;
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Standalone",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = TextureImporterFormat.DXT1,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "iPhone",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = TextureImporterFormat.PVRTC_RGB4,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Android",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = TextureImporterFormat.ETC2_RGB4,
                        });
                        break;
                    }
                case BinRaw.Quality.RGB16A4:
                    {
                        importer.alphaIsTransparency = false;
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Standalone",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = TextureImporterFormat.RGB16,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "iPhone",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = TextureImporterFormat.RGB16,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Android",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = TextureImporterFormat.RGB16,
                        });
                        break;
                    }
                case BinRaw.Quality.Full:
                    {
                        importer.alphaIsTransparency = transparency;
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Standalone",
                            overridden = false
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "iPhone",
                            overridden = false
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Android",
                            overridden = false
                        });
                        break;
                    }
                case BinRaw.Quality.Legacy:
                    {
                        importer.alphaIsTransparency = transparency;
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Standalone",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = TextureImporterFormat.DXT5,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "iPhone",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = transparency ? TextureImporterFormat.RGBA16 : TextureImporterFormat.RGB16,
                        });
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                        {
                            name = "Android",
                            overridden = true,
                            compressionQuality = (int)TextureCompressionQuality.Normal,
                            allowsAlphaSplitting = false,
                            maxTextureSize = importer.maxTextureSize,
                            format = transparency ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4,
                        });
                        break;
                    }
            }
            importer.SaveAndReimport();
        }

        private static bool CheckTextureCompressed(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                var pcSetttings = importer.GetPlatformTextureSettings("Standalone");
                var iphoneSettings = importer.GetPlatformTextureSettings("iPhone");
                var androidSetttings = importer.GetPlatformTextureSettings("Android");
                if (importer != null &&
                    (importer.textureCompression == TextureImporterCompression.Compressed ||
                    (pcSetttings.overridden && pcSetttings.format != TextureImporterFormat.RGBA32 && pcSetttings.format != TextureImporterFormat.RGB24) ||
                    (iphoneSettings.overridden && iphoneSettings.format != TextureImporterFormat.RGBA32 && iphoneSettings.format != TextureImporterFormat.RGB24) ||
                    (androidSetttings.overridden && androidSetttings.format != TextureImporterFormat.RGBA32 && androidSetttings.format != TextureImporterFormat.RGB24)))
                {
                    return true;
                }
            }
            return false;
        }

        private static int NPOTScale(int size)
        {
            var sizeList = AtlasPacker.AtlasSizeList;
            var i = sizeList[0];
            while (i < size) i *= 2;
            i = Mathf.Min(i, sizeList[sizeList.Length - 1]);
            return i;
        }
    }
}
