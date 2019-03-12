#if AtlaS_ON
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public sealed class AtlasPacker
    {
        public static AtlasRaw Pack(string targetFolder, IPackSprite[] textures, PackSetting setting)
        {
            EditorUtility.DisplayProgressBar("", "generate atlas...", 0.5f);
            var atlasPath = Path.Combine(targetFolder, PackConst.DefaultAtlasAssetName);
            atlasPath = AssetDatabase.GenerateUniqueAssetPath(atlasPath);
            var atlasRaw = new PackAtlas(setting, atlasPath, textures).GenerateAtlas();
            EditorUtility.ClearProgressBar();
            return atlasRaw;
        }

        public static AtlasRaw Repack(AtlasRaw atlasRaw, IPackSprite[] textures = null, PackSetting setting = null)
        {
            EditorUtility.DisplayProgressBar("", "repack atlas...", 0.5f);
            textures = textures ?? PackAtlasSprite.ListSprites(atlasRaw);
            setting = setting ?? new PackSetting(atlasRaw.maxSize, atlasRaw.padding, atlasRaw.isPOT, atlasRaw.forceSquare);
            var atlasPath = AssetDatabase.GetAssetPath(atlasRaw);
            atlasRaw = new PackAtlas(setting, atlasPath, textures).GenerateAtlas();
            EditorUtility.ClearProgressBar();
            if (atlasRaw == null)
            {
                Debug.LogError("Pack failed.");
                return null;
            }
            return atlasRaw;
        }

        public static string[] Export(AtlasRaw atlas, SpriteRaw[] sprites, string folder)
        {
            var exportSprites = new string[sprites.Length];
            var atlasSprites = sprites.Select(i => PackAtlasSprite.ParseSprite(atlas.bins[i.bin], i)).ToArray();
            PackUtil.LoadAtlasTextures(atlasSprites);
            for (int i = 0; i < atlasSprites.Length; i++)
            {
                var sprite = atlasSprites[i];
                var rect = sprite.rect;
                var texture = PackUtil.CreateBlankTexture((int)rect.width, (int)rect.height, sprite.transparency ? TextureFormat.RGBA32 : TextureFormat.RGB24);
                texture.SetPixels(sprite.Read());
                var bytes = sprite.transparency ? texture.EncodeToPNG() : texture.EncodeToJPG();
                var path = Path.Combine(folder, sprite.name) + (sprite.transparency ? ".png" : ".jpg");
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, bytes);
                exportSprites[i] = path;
                if (EditorUtility.DisplayCancelableProgressBar("", "export: " + sprite.name, (float)i / sprites.Length))
                {
                    break;
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return exportSprites;
        }
    }
}
#endif