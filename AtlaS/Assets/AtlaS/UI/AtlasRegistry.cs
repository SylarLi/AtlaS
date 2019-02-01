using UnityEngine;
using System.Collections.Generic;

namespace AtlaS.UI
{
    public sealed class AtlasRegistry
    {
        private static Dictionary<AtlasRaw, AtlasSpriteSet> mSpriteSet = new Dictionary<AtlasRaw, AtlasSpriteSet>();

        private static Material mAlphaSplitCanvasMaterial;

        public static Sprite RetrieveAtlasSprite(AtlasRaw atlasRaw, string spriteRawName)
        {
            if (!mSpriteSet.ContainsKey(atlasRaw))
            {
                mSpriteSet.Add(atlasRaw, new AtlasSpriteSet(atlasRaw));
                atlasRaw.RegisterDestroyCallBack(OnAtlasRawDestroyed);
            }
            return mSpriteSet[atlasRaw].FindSprite(spriteRawName);
        }

        public static Material GetAlphaSplitCanvasMaterial()
        {
            if (mAlphaSplitCanvasMaterial == null)
            {
                mAlphaSplitCanvasMaterial = new Material(Shader.Find("AtlaS/Default"));
                mAlphaSplitCanvasMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
                mAlphaSplitCanvasMaterial.EnableKeyword("ATLAS_UI_ALPHASPLIT");
            }
            return mAlphaSplitCanvasMaterial;
        }

        public static void Clear()
        {
            mSpriteSet.Clear();
            mAlphaSplitCanvasMaterial = null;
        }

        private static void OnAtlasRawDestroyed(AtlasRaw atlasRaw)
        {
            if (mSpriteSet.ContainsKey(atlasRaw))
            {
                mSpriteSet[atlasRaw].Dispose();
                mSpriteSet.Remove(atlasRaw);
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

            public Sprite FindSprite(string spriteRawName)
            {
                if (mSprites.ContainsKey(spriteRawName))
                {
                    return mSprites[spriteRawName];
                }
                var spriteRaw = mAtlasRaw.FindSprite(spriteRawName);
                if (spriteRaw != null)
                {
                    var sprite = Sprite.Create(mAtlasRaw, spriteRaw);
                    mSprites[sprite.name] = sprite;
                    return sprite;
                }
                return null;
            }

            public void Dispose()
            {
                mSprites = null;
                mAtlasRaw = null;
            }
        }
    }
}
