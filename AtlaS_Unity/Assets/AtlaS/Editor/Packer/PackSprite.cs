#if AtlaS_ON
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Atlas;

namespace UnityEditor.UI.Atlas
{
    public interface IPackSprite
    {
        string name { get; set; }

        PackQuality quality { get; set; }

        Rect rect { get; }

        Vector4 border { get; }

        Vector2 pivot { get; }

        bool transparency { get; }

        Color[] Read();
    }

    public class PackAssetSprite : IPackSprite
    {
        private string mName;
        public string name { get { return mName; } set { mName = value; } }

        private PackQuality mQuality;
        public PackQuality quality { get { return mQuality; } set { mQuality = value; } }

        private string mAssetPath;
        public string assetPath { get { return mAssetPath; } }

        private Texture2D mAsset;
        public Texture2D asset { get { return mAsset; } }

        private Rect mRect;
        public Rect rect { get { return mRect; } }

        private Vector4 mBorder;
        public Vector4 border { get { return mBorder; } }

        private Vector2 mPivot;
        public Vector2 pivot { get { return mPivot; } }

        private bool mTransparency;
        public bool transparency { get { return mTransparency; } }

        public PackAssetSprite(string assetPath)
        {
            mAssetPath = assetPath;
            var sprite = mAssetPath.StartsWith("Assets/") ? 
                AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(mAssetPath) : null;
            if (sprite != null)
            {
                mBorder = sprite.border;
                mPivot = sprite.pivot;
            }
            else
            {
                mBorder = Vector4.zero;
                mPivot = new Vector2(0.5f, 0.5f);
            }
            mAsset = new Texture2D(1, 1);
            mAsset.LoadImage(File.ReadAllBytes(mAssetPath));
            mName = Path.GetFileNameWithoutExtension(mAssetPath);
            mRect = new Rect(0, 0, mAsset.width, mAsset.height);
            mTransparency = PackUtil.CheckTextureTranparency(mAsset.format);
            mQuality = PackQuality.AlphaSplit;
        }

        public Color[] Read()
        {
            return mAsset.GetPixels();
        }
    }

    public class PackAtlasSprite : IPackSprite
    {
        public static PackAtlasSprite ParseSprite(BinRaw binRaw, SpriteRaw spriteRaw)
        {
            return new PackAtlasSprite(binRaw, spriteRaw);
        }

        public static IPackSprite[] ListSprites(AtlasRaw atlasRaw)
        {
            var list = new List<PackAtlasSprite>();
            foreach (var binRaw in atlasRaw.bins)
            {
                foreach (var spriteRaw in binRaw.sprites)
                {
                    list.Add(ParseSprite(binRaw, spriteRaw));
                }
            }
            return list.ToArray();
        }

        private string mName;
        public string name { get { return mName; } set { mName = value; } }

        private PackQuality mQuality;
        public PackQuality quality { get { return mQuality; } set { mQuality = value; } }

        private Texture2D mMain;
        public Texture2D main { get { return mMain; } set { mMain = value; } }

        private Texture2D mAddition;
        public Texture2D addition { get { return mAddition; } set { mAddition = value; } }

        private Rect mRect;
        public Rect rect { get { return mRect; } }

        private Vector4 mBorder;
        public Vector4 border { get { return mBorder; } }

        private Vector2 mPivot;
        public Vector2 pivot { get { return mPivot; } }

        private bool mTransparency;
        public bool transparency { get { return mTransparency; } }

        public PackAtlasSprite(BinRaw binRaw, SpriteRaw spriteRaw)
        {
            mMain = binRaw.main;
            Debug.Assert(mMain != null, "Can not find main texture.");
            mAddition = binRaw.addition;
            mName = spriteRaw.name;
            mRect = spriteRaw.rect;
            mBorder = spriteRaw.border;
            mPivot = spriteRaw.pivot;
            mTransparency = PackUtil.CheckAtlasBinTranparency(binRaw);
            mQuality = (PackQuality)binRaw.quality;
        }

        public Color[] Read()
        {
            var colors = main.GetPixels(
                (int)rect.x, (int)rect.y,
                (int)rect.width, (int)rect.height);
            if (addition != null)
            {
                var alphas = addition.GetPixels(
                    (int)rect.x, (int)rect.y,
                    (int)rect.width, (int)rect.height);
                for (int c = 0; c < colors.Length; c++)
                {
                    var color = colors[c];
                    color.a = alphas[c].r;
                    colors[c] = color;
                }
            }
            return colors;
        }
    }
}
#endif