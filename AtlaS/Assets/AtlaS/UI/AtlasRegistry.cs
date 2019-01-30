using UnityEngine;
using System.Collections.Generic;

namespace AtlaS.UI
{
    public sealed class AtlasRegistry
    {
        private static Dictionary<AtlasRaw, AtlasSpriteSet> mSpriteSet = new Dictionary<AtlasRaw, AtlasSpriteSet>();

        private static Dictionary<AtlasRaw, AtlasMaterialSet> mMaterialSet = new Dictionary<AtlasRaw, AtlasMaterialSet>();

        public static Sprite RegisterAtlasSprite(AtlasRaw atlasRaw, string sprite)
        {
            if (!mSpriteSet.ContainsKey(atlasRaw))
            {
                mSpriteSet.Add(atlasRaw, new AtlasSpriteSet(atlasRaw));
                atlasRaw.RegisterDestroyCallBack(OnAtlasRawDestroyed);
            }
            return mSpriteSet[atlasRaw].FindSprite(sprite);
        }

        public static Material RegisterAtlasMaterial(AtlasRaw atlasRaw, string sprite)
        {
            if (!mMaterialSet.ContainsKey(atlasRaw))
            {
                mMaterialSet.Add(atlasRaw, new AtlasMaterialSet(atlasRaw));
            }
            var spriteRaw = atlasRaw.FindSprite(sprite);
            return spriteRaw == null ? null :
                mMaterialSet[atlasRaw].FindMaterial(spriteRaw.bin);
        }

        private static void OnAtlasRawDestroyed(AtlasRaw atlasRaw)
        {
            if (mSpriteSet.ContainsKey(atlasRaw))
            {
                mSpriteSet[atlasRaw].Dispose();
                mSpriteSet.Remove(atlasRaw);
            }
            if (mMaterialSet.ContainsKey(atlasRaw))
            {
                mMaterialSet[atlasRaw].Dispose();
                mMaterialSet.Remove(atlasRaw);
            }
        }

        private class AtlasSpriteSet
        {
            private AtlasRaw mAtlasRaw;

            private Dictionary<string, Sprite> mSprites;

            public AtlasSpriteSet(AtlasRaw atlasRaw)
            {
                mAtlasRaw = atlasRaw;
                mSprites = new Dictionary<string, Sprite>();
            }

            public Sprite FindSprite(string spriteName)
            {
                if (mSprites.ContainsKey(spriteName))
                {
                    return mSprites[spriteName];
                }
                var spriteRaw = mAtlasRaw.FindSprite(spriteName);
                if (spriteRaw != null)
                {
                    var bin = mAtlasRaw.bins[spriteRaw.bin];
                    var sprite = Sprite.Create(
                        bin.main, spriteRaw.rect, spriteRaw.pivot, 100, 1,
                        SpriteMeshType.Tight, spriteRaw.border);
                    sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    sprite.name = spriteRaw.name;
                    mSprites[spriteRaw.name] = sprite;
                    return sprite;
                }
                return null;
            }

            public void Dispose()
            {
                foreach (var sprite in mSprites)
                {
                    if (sprite.Value != null)
                    {
#if UNITY_EDITOR
                        Object.DestroyImmediate(sprite.Value);
#else
                        Object.Destroy(sprite.Value);
#endif
                    }
                }
                mSprites = null;
                mAtlasRaw = null;
            }
        }

        private class AtlasMaterialSet
        {
            private AtlasRaw mAtlasRaw;

            private Material[] mMaterials;

            public AtlasMaterialSet(AtlasRaw atlasRaw)
            {
                mAtlasRaw = atlasRaw;
                mMaterials = new Material[mAtlasRaw.bins.Length];
            }

            public Material FindMaterial(int binIndex)
            {
                if (binIndex >= 0 && 
                    binIndex < mMaterials.Length)
                {
                    var material = mMaterials[binIndex];
                    if (material == null)
                    {
                        var binRaw = mAtlasRaw.bins[binIndex];
                        material = new Material(Shader.Find("AtlaS/Default"));
                        material.hideFlags = HideFlags.DontUnloadUnusedAsset;
                        material.SetTexture("_MainTex", binRaw.main);
                        if ((binRaw.quality == BinRaw.Quality.Normal ||
                            binRaw.quality == BinRaw.Quality.RGB16A4) &&
                            binRaw.transparency &&
                            binRaw.addition != null)
                        {
                            material.SetTexture("_AlphaTex", binRaw.addition);
                            material.EnableKeyword("ATLAS_UI_ALPHASPLIT");
                        }
                    }
                    return material;
                }
                return null;
            }

            public void Dispose()
            {
                foreach (var material in mMaterials)
                {
                    if (material != null)
                    {
#if UNITY_EDITOR
                        Object.DestroyImmediate(material);
#else
                        Object.Destroy(material);
#endif
                    }
                }
                mMaterials = null;
                mAtlasRaw = null;
            }
        }
    }
}
