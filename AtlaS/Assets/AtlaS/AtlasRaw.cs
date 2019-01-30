using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtlaS
{
    [Serializable]
    public class AtlasRaw : ScriptableObject
    {
        public int id;

        public int maxSize;

        public int padding;

        public bool isPOT;

        public bool forceSquare;

        public BinRaw[] bins;

        public SpriteRaw FindSprite(string spriteName)
        {
            foreach (var bin in bins)
            {
                var spriteRaw = bin.FindSprite(spriteName);
                if (spriteRaw != null)
                {
                    return spriteRaw;
                }
            }
            return null;
        }

        public delegate void DestoryCallBack(AtlasRaw atlasRaw);

        private List<DestoryCallBack> mDestoryCallBacks;

        public void RegisterDestroyCallBack(DestoryCallBack callback)
        {
            mDestoryCallBacks = mDestoryCallBacks ?? new List<DestoryCallBack>();
            mDestoryCallBacks.Add(callback);
        }

        private void OnDestroy()
        {
            if (mDestoryCallBacks != null &&
                mDestoryCallBacks.Count > 0)
            {
                mDestoryCallBacks.ForEach(callback => callback(this));
                mDestoryCallBacks = null;
            }
        }
    }

    [Serializable]
    public class BinRaw
    {
        public enum Quality
        {
            Normal,                 // RGB4 with Alpha4 (Recommend)
            Legacy,                 // PC DXT5, iOS ARGB16, Android ETC2-RGBA8
            RGB16A4,                // RGB16 with Alpha4
            Full,                   // RGBA32 or ARGB32 | RGB24
        }

        public Quality quality = Quality.Full;

        public Vector2 size;

        public SpriteRaw[] sprites;

        public Texture2D main;

        public Texture2D addition;

        public bool transparency
        {
            get
            {
                switch (quality)
                {
                    case Quality.Full:
                    case Quality.Legacy:
                        {
                            return AtlasRawUtil.CheckTextureTranparency(main.format);
                        }
                    case Quality.RGB16A4:
                    case Quality.Normal:
                        {
                            return addition != null;
                        }
                }
                return false;
            }
        }

        public SpriteRaw FindSprite(string spriteName)
        {
            if (sprites != null)
            {
                foreach (var sprite in sprites)
                {
                    if (sprite.name == spriteName)
                    {
                        return sprite;
                    }
                }
            }
            return null;
        }
    }

    [Serializable]
    public class SpriteRaw
    {
        /// <summary>
        /// Index of sprite's bin in atlas.
        /// </summary>
        public int bin;

        /// <summary>
        /// Unique id.
        /// </summary>
        public ushort id;

        /// <summary>
        /// Sprite asset name.
        /// </summary>
        public string name;

        /// <summary>
        /// Sprite's rect in bin.
        /// </summary>
        public Rect rect;

        /// <summary>
        /// Sprite's border, for 9 slice.
        /// </summary>
        public Vector4 border;

        /// <summary>
        /// Sprite's pivot
        /// </summary>
        public Vector2 pivot;
    }
}
