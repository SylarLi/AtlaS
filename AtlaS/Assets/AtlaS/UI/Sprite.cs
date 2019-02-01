using UnityEngine;

namespace AtlaS.UI
{
    public sealed class Sprite
    {
        private string mName;

        private Texture2D mMain;

        private Texture2D mAddition;

        private Rect mRect;

        private Vector4 mBorder;

        private Vector2 mPivot;

        public Sprite(string name, Texture2D main, Texture2D addition, Rect rect, Vector2 pivot, Vector4 border)
        {
            mName = name;
            mMain = main;
            mAddition = addition;
            mRect = rect;
            mPivot = pivot;
            mBorder = border;
        }

        public string name { get { return mName; } }

        public Texture2D main { get { return mMain; } }

        public Texture2D addition { get { return mAddition; } }

        public Vector4 border { get { return mBorder; } }

        public Vector2 pivot { get { return mPivot; } }
        
        public float pixelsPerUnit { get { return 100; } }

        public Rect rect { get { return mRect; } }

        public static Sprite Create(AtlasRaw atlasRaw, SpriteRaw spriteRaw)
        {
            var binRaw = atlasRaw.bins[spriteRaw.bin];
            return new Sprite(spriteRaw.name, binRaw.main, binRaw.addition, spriteRaw.rect, spriteRaw.pivot, spriteRaw.border);
        }
    }
}
