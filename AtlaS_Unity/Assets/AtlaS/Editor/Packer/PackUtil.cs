#if AtlaS_ON
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public sealed class PackUtil
    {
        public static void LoadAtlasTextures(PackAtlasSprite[] sprites)
        {
            var texMap = new Dictionary<string, Texture2D>();
            foreach (var sprite in sprites)
            {
                var mainAsset = sprite.main;
                var mainPath = AssetDatabase.GetAssetPath(mainAsset);
                if (!string.IsNullOrEmpty(mainPath) &&
                    File.Exists(mainPath))
                {
                    if (!texMap.ContainsKey(mainPath))
                    {
                        mainAsset = new Texture2D(1, 1);
                        mainAsset.LoadImage(File.ReadAllBytes(mainPath));
                        texMap[mainPath] = mainAsset;
                    }
                    sprite.main = texMap[mainPath];
                }
                var addAsset = sprite.addition;
                var addPath = AssetDatabase.GetAssetPath(addAsset);
                if (!string.IsNullOrEmpty(addPath) &&
                    File.Exists(addPath))
                {
                    if (!texMap.ContainsKey(addPath))
                    {
                        addAsset = new Texture2D(1, 1);
                        addAsset.LoadImage(File.ReadAllBytes(addPath));
                        texMap[addPath] = addAsset;
                    }
                    sprite.addition = texMap[addPath];
                }
            }
        }

        public static Texture2D CreateBlankTexture(int width, int height, TextureFormat format)
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

        public static int Scale2POT(int size)
        {
            var sizeList = PackConst.AtlasSizeList;
            var i = sizeList[0];
            while (i < size) i *= 2;
            i = Mathf.Min(i, sizeList[sizeList.Length - 1]);
            return i;
        }

        public static bool CheckTextureCompressed(string assetPath)
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

        public static bool CheckAtlasBinTranparency(BinRaw binRaw)
        {
            return binRaw.addition != null || CheckTextureTranparency(binRaw.main.format);
        }

        public static bool CheckTextureTranparency(TextureFormat format)
        {
            return format == TextureFormat.ARGB32 ||
                format == TextureFormat.RGBA32 ||
                format == TextureFormat.ARGB4444 ||
                format == TextureFormat.RGBA4444 ||
                format == TextureFormat.PVRTC_RGBA4 ||
                format == TextureFormat.PVRTC_RGBA2 ||
                format == TextureFormat.ETC2_RGBA1 ||
                format == TextureFormat.ETC2_RGBA8 ||
                format == TextureFormat.DXT5;
        }

        public static SpriteRaw[] SearchSprites(AtlasRaw atlas, string matchWord)
        {
            matchWord = matchWord.ToLower();
            var list = new List<SpriteRaw>();
            foreach (var bin in atlas.bins)
            {
                foreach (var sprite in bin.sprites)
                {
                    if (sprite.name.ToLower().Contains(matchWord))
                    {
                        list.Add(sprite);
                    }
                }
            }
            return list.ToArray();
        }

        public static void IndexSprite(AtlasRaw atlas, SpriteRaw sprite, out int iBin, out int iSprite)
        {
            iBin = -1;
            iSprite = -1;
            for (int i = 0; i < atlas.bins.Length; i++)
            {
                var bin = atlas.bins[i];
                for (int j = 0; j < bin.sprites.Length; j++)
                {
                    if (bin.sprites[j] == sprite)
                    {
                        iSprite = j;
                        break;
                    }
                }
                if (iSprite != -1)
                {
                    iBin = i;
                    break;
                }
            }
        }

        public static string GetDisplayName(BinRaw bin)
        {
            var main = bin.main;
            var size = bin.size;
            var quality = bin.quality;
            var builder = new StringBuilder();
            builder.Append(main.name);
            builder.Append(string.Format("  [{0}x{1}]", (int)size.x, (int)size.y));
            builder.Append(string.Format("  [{0}]", Enum.GetName(typeof(PackQuality), quality).ToLower()));
            return builder.ToString();
        }

        public static int GenerateAtlasId()
        {
            return UnityEngine.Random.Range(1, int.MaxValue);
        }
    }
}
#endif