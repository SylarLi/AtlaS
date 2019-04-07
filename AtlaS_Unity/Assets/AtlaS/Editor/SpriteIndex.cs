#if AtlaS_ON
namespace UnityEditor.UI.Atlas
{
    [System.Serializable]
    public class SpriteIndex
    {
        public int atlas; 

        public int bin;

        public int sprite;

        public SpriteIndex(int atlas, int bin, int sprite)
        {
            this.atlas = atlas;
            this.bin = bin;
            this.sprite = sprite;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var s = (SpriteIndex)obj;
            if (ReferenceEquals(s, null))
                return false;
            return s.atlas == atlas &&
                s.bin == bin &&
                s.sprite == sprite;
        }
    }
}
#endif