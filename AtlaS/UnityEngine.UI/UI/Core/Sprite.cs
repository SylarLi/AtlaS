using System;
using UnityEngine.UI.Atlas;

namespace UnityEngine.UI
{
    [Serializable]
    public sealed class Sprite
    {
        [Serializable]
        public enum Type
        {
            Sprite = 0,
            Atlas = 1,
        }

        [SerializeField]
        private Type m_Type;

        [SerializeField]
        private UnityEngine.Sprite m_Sprite;

        [SerializeField]
        private AtlasRaw m_AtlasRaw;

        [SerializeField]
        private string m_SpriteName;

        [NonSerialized]
        private SpriteRaw m_SpriteRaw;

        public Sprite(UnityEngine.Sprite sprite)
        {
            m_Type = Type.Sprite;
            m_Sprite = sprite;
            Debug.Assert(!IsNull());
        }

        public Sprite(AtlasRaw atlasRaw, string spriteName)
        {
            m_Type = Type.Atlas;
            m_AtlasRaw = atlasRaw;
            m_SpriteName = spriteName;
            Debug.Assert(!IsNull());
            m_SpriteRaw = m_AtlasRaw.FindSprite(m_SpriteName);
            Debug.Assert(m_SpriteRaw != null);
        }

        public Type type { get { return m_Type; } }

        public UnityEngine.Sprite sprite { get { return m_Sprite; } }

        public AtlasRaw atlasRaw { get { return m_AtlasRaw; } }

        public string spriteName { get { return m_SpriteName; } }

        public SpriteRaw spriteRaw { get { if (m_SpriteRaw == null) m_SpriteRaw = atlasRaw.FindSprite(spriteName); return m_SpriteRaw; } }

        public string name { get { return type == Type.Atlas ? spriteRaw.name : sprite.name; } }

        public Texture2D texture { get { return type == Type.Atlas ? atlasRaw.bins[spriteRaw.bin].main : sprite.texture; } }

        public Texture2D associatedAlphaSplitTexture { get { return type == Type.Atlas ? atlasRaw.bins[spriteRaw.bin].addition : sprite.associatedAlphaSplitTexture; } }

        public Vector4 border { get { return type == Type.Atlas ? spriteRaw.border : sprite.border; } }

        public Vector2 pivot { get { return type == Type.Atlas ? spriteRaw.pivot : sprite.pivot; } }

        public float pixelsPerUnit { get { return type == Type.Atlas ? 100 : sprite.pixelsPerUnit; } }

        public Rect rect { get { return type == Type.Atlas ? spriteRaw.rect : sprite.rect; } }

        public Rect textureRect { get { return type == Type.Atlas ? spriteRaw.rect : sprite.textureRect; } }

        public bool packed { get { return type == Type.Atlas ? true : sprite.packed; } }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Sprite s = (Sprite)obj;
            if (ReferenceEquals(s, null) || s.IsNull())
                return IsNull();
            return s.type == type &&
                ((type == Type.Sprite && s.sprite == sprite) ||
                (type == Type.Atlas && s.atlasRaw == atlasRaw && s.spriteName == spriteName));
        }

        public static bool operator == (Sprite s1, Sprite s2)
        {
            if (ReferenceEquals(s1, null))
                return ReferenceEquals(s2, null);
            return s1.Equals(s2);
        }

        public static bool operator != (Sprite s1, Sprite s2)
        {
            return !(s1 == s2);
        }

        private bool IsNull()
        {
            return (type == Type.Sprite && sprite == null) ||
                (type == Type.Atlas && (atlasRaw == null || string.IsNullOrEmpty(spriteName)));
        }
    }
}
