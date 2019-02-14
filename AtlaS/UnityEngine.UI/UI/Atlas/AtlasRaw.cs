using System;
using System.Collections.Generic;

namespace UnityEngine.UI.Atlas
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

        public void Dispose()
        {
            foreach (var bin in bins)
            {
                bin.Dispose();
            }
            bins = null;
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

        public void Dispose()
        {
            if (sprites != null)
            {
                foreach (var sprite in sprites)
                {
                    sprite.Dispose();
                }
                sprites = null;
            }
            main = null;
            addition = null;
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

        public void Dispose()
        {
            bin = -1;
            name = null;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SpriteRaw s = (SpriteRaw)obj;
            if (ReferenceEquals(s, null) || s.IsNull())
                return IsNull();
            return ReferenceEquals(this, s);
        }

        public static bool operator ==(SpriteRaw s1, SpriteRaw s2)
        {
            if (ReferenceEquals(s1, null))
                return ReferenceEquals(s2, null);
            return s1.Equals(s2);
        }

        public static bool operator !=(SpriteRaw s1, SpriteRaw s2)
        {
            return !(s1 == s2);
        }

        private bool IsNull()
        {
            return bin < 0 || string.IsNullOrEmpty(name);
        }
    }
}
