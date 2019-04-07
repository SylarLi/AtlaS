#if AtlaS_ON
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public class PackAtlas
    {
        private PackSetting mSetting;
        public PackSetting setting { get { return mSetting; } }

        private string mAtlasPath;
        public string atlasPath { get { return mAtlasPath; } }

        private IPackSprite[] mSprites;
        public IPackSprite[] sprites { get { return mSprites; } }

        public PackAtlas(PackSetting setting, string atlasPath, IPackSprite[] sprites)
        {
            mSetting = setting;
            mAtlasPath = atlasPath;
            mSprites = sprites;
        }

        public AtlasRaw GenerateAtlas()
        {
            if (sprites.Length > PackConst.MaxNumberOfSpriteInAtlas)
            {
                Debug.LogError(string.Format("Sprites in excess of {0}.", PackConst.MaxNumberOfSpriteInAtlas));
                return null;
            }
            for (int i = 0; i < sprites.Length; i++)
            {
                var texName = sprites[i].name;
                var texRect = sprites[i].rect;
                if (texRect.width > setting.maxAtlasSize ||
                    texRect.height > setting.maxAtlasSize)
                {
                    Debug.LogError(string.Format("Texture size should be lower than {0}: {1}.", setting.maxAtlasSize, texName));
                    return null;
                }
            }

            var atlasSprites = sprites.Where(i => i is PackAtlasSprite).Select(i => i as PackAtlasSprite).ToArray();
            PackUtil.LoadAtlasTextures(atlasSprites);

            var groups = sprites.GroupBy(texture => new { texture.quality, texture.transparency })
                .OrderBy(group => group.Key.quality)
                .ThenBy(group => group.Key.transparency);
            var bins = groups.Select(group => 
            {
                var groupQuality = group.Key.quality;
                var groupTransparency = group.Key.transparency;
                var groupSprites = group.ToArray();
                var groupProcessor = PackProcessor.Fetch(groupQuality);
                groupProcessor.Setup(setting, groupSprites, groupTransparency);
                var groupBins = groupProcessor.Execute();
                foreach (var groupBin in groupBins)
                    groupBin.quality = (int)groupQuality;
                return groupBins;
            }).SelectMany(i => i).ToArray();
            for (int i = 0; i < bins.Length; i++)
            {
                foreach (var sprite in bins[i].sprites)
                {
                    sprite.bin = i;
                }
            }

            var atlasRaw = AssetDatabase.LoadAssetAtPath<AtlasRaw>(atlasPath);
            var atlasExist = atlasRaw != null;
            if (atlasExist) RemoveAtlasTextures(atlasRaw);
            else atlasRaw = ScriptableObject.CreateInstance<AtlasRaw>();
            atlasRaw.hideFlags = HideFlags.DontUnloadUnusedAsset;
            atlasRaw.bins = bins;
            atlasRaw.maxSize = setting.maxAtlasSize;
            atlasRaw.padding = setting.padding;
            atlasRaw.isPOT = setting.isPOT;
            atlasRaw.forceSquare = setting.forceSquare;
            if (atlasRaw.id == 0) atlasRaw.id = PackUtil.GenerateAtlasId();
            CreateAtlasTextures(atlasRaw);
            if (atlasExist)
            {
                EditorUtility.SetDirty(atlasRaw);
                AssetDatabase.SaveAssets();
            }
            else
            {
                AssetDatabase.CreateAsset(atlasRaw, atlasPath);
            }

            return atlasRaw;
        }

        private void CreateAtlasTextures(AtlasRaw atlasRaw)
        {
            var atlasFolder = Path.GetDirectoryName(atlasPath);
            var atlasName = Path.GetFileNameWithoutExtension(atlasPath);
            var bins = atlasRaw.bins;
            for (int i = 0; i < bins.Length; i++)
            {
                var binRaw = bins[i];
                var mainBytes = binRaw.main.EncodeToPNG();
                var mainPath = Path.Combine(atlasFolder, atlasName + "_" + i + ".png");
                File.WriteAllBytes(mainPath, mainBytes);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UnityEngine.Object.DestroyImmediate(binRaw.main);
                binRaw.main = AssetDatabase.LoadAssetAtPath<Texture2D>(mainPath);
                if (binRaw.addition != null)
                {
                    var addBytes = binRaw.addition.EncodeToPNG();
                    var addPath = Path.Combine(atlasFolder, atlasName + "_" + i + "_add.png");
                    File.WriteAllBytes(addPath, addBytes);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    UnityEngine.Object.DestroyImmediate(binRaw.addition);
                    binRaw.addition = AssetDatabase.LoadAssetAtPath<Texture2D>(addPath);
                }
                var pp = PackPostProcessor.Fetch((PackQuality)binRaw.quality);
                pp.Setup(setting, binRaw);
                pp.Execute();
            }
        }

        private void RemoveAtlasTextures(AtlasRaw atlasRaw)
        {
            var texAssets = new List<Texture2D>();
            foreach (var bin in atlasRaw.bins)
            {
                if (bin.main != null)
                    texAssets.Add(bin.main);
                if (bin.addition != null)
                    texAssets.Add(bin.addition);
            }
            foreach (var texAsset in texAssets)
            {
                var texPath = AssetDatabase.GetAssetPath(texAsset);
                if (!string.IsNullOrEmpty(texPath))
                {
                    File.Delete(texPath);
                    var metaPath = texPath + ".meta";
                    if (File.Exists(metaPath))
                        File.Delete(metaPath);
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
#endif