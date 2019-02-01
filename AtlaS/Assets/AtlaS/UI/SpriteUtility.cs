using UnityEngine;

namespace AtlaS.UI
{
    public sealed class SpriteUtility
    {
        public static Vector4 GetInnerUV(Sprite sprite)
        {
            var border = sprite.border;
            var rect = sprite.rect;
            var main = sprite.main;
            return new Vector4(
                (rect.x + border.x) / main.width,
                (rect.y + border.y) / main.height,
                (rect.xMax - border.z) / main.width,
                (rect.yMax - border.w) / main.height);
        }

        public static Vector4 GetOuterUV(Sprite sprite)
        {
            var border = sprite.border;
            var rect = sprite.rect;
            var main = sprite.main;
            return new Vector4(
                rect.x / main.width,
                rect.y / main.height,
                rect.xMax / main.width,
                rect.yMax / main.height);
        }

        public static Vector2 GetMinSize(Sprite sprite)
        {
            var border = sprite.border;
            return new Vector2(
                border.x + border.z,
                border.y + border.w);
        }

        public static Vector4 GetPadding(Sprite sprite)
        {
            return Vector4.zero;
        }
    }
}
