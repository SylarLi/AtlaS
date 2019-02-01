using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace AtlaS
{
    public class AtlasPackSprite
    {
        public static AtlasPackSprite ParseSpriteRaw(BinRaw bin, SpriteRaw sprite)
        {
            return new AtlasPackSprite()
            {
                main = bin.main,
                addition = bin.addition,
                rawQuality = bin.quality,
                transparency = bin.transparency,
                quality = bin.quality,
                name = sprite.name,
                rect = sprite.rect,
                border = sprite.border,
                pivot = sprite.pivot,
            };
        }

        public static AtlasPackSprite[] ListSprites(AtlasRaw atlas)
        {
            var list = new List<AtlasPackSprite>();
            foreach (var bin in atlas.bins)
            {
                foreach (var sprite in bin.sprites)
                {
                    list.Add(ParseSpriteRaw(bin, sprite));
                }
            }
            return list.ToArray();
        }

        private string mPath;

        public string path
        {
            get
            {
                return mPath;
            }
        }

        private string mName;

        public string name
        {
            get
            {
                if (!string.IsNullOrEmpty(mName))
                    return mName;
                else if (main != null)
                    return main.name;
                return "undefined";
            }
            set
            {
                mName = value;
            }
        }

        private Texture2D mMain;

        public Texture2D main
        {
            get
            {
                if (mMain == null &&
                    !string.IsNullOrEmpty(path))
                {
                    mMain = new Texture2D(2, 2);
                    mMain.LoadImage(File.ReadAllBytes(path));
                }
                return mMain;
            }
            set
            {
                mMain = value;
            }
        }

        private Rect mRect;

        public Rect rect
        {
            get
            {
                if (mRect.width == 0 ||
                    mRect.height == 0)
                {
                    if (main != null)
                    {
                        return new Rect(0, 0, main.width, main.height);
                    }
                    return default(Rect);
                }
                return mRect;
            }
            set
            {
                mRect = value;
            }
        }

        private int mTransparency = -1;
        
        /// <summary>
        /// if exist alpha channel.
        /// </summary>
        public bool transparency
        {
            get
            {
                if (mTransparency == -1 && 
                    main != null)
                {
                    return AtlasRawUtil.CheckTextureTranparency(main.format);
                }
                return mTransparency <= 0;
            }
            set
            {
                mTransparency = value ? 0 : 1;
            }
        }

        /// <summary>
        /// Raw quality type
        /// </summary>
        public BinRaw.Quality rawQuality { get; set; }

        /// <summary>
        /// Target quality type
        /// </summary>
        public BinRaw.Quality quality { get; set; }

        public Texture2D addition { get; set; }

        public Vector4 border { get; set; }

        public Vector2 pivot { get; set; }

        public AtlasPackSprite()
        {

        }

        public AtlasPackSprite(string path)
        {
            mPath = path;
        }
    }
}
