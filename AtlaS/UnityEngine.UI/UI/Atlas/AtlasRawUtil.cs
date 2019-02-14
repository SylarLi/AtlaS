using System.Collections.Generic;
using System.Text;

namespace UnityEngine.UI.Atlas
{
    public sealed class AtlasRawUtil
    {
        public static SpriteRaw[] SearchSprites(AtlasRaw atlas, string matchWord)
        {
            matchWord = matchWord.ToLower();
            var list = new List<SpriteRaw>();
            foreach (var bin in atlas.bins)
            {
                foreach (var sprite in bin.sprites)
                {
                    if (sprite.name.ToLower().Contains(matchWord))
                    {
                        list.Add(sprite);
                    }
                }
            }
            return list.ToArray();
        }

        public static void IndexSprite(AtlasRaw atlas, SpriteRaw sprite, out int iBin, out int iSprite)
        {
            iBin = -1;
            iSprite = -1;
            for (int i = 0; i < atlas.bins.Length; i++)
            {
                var bin = atlas.bins[i];
                for (int j = 0; j < bin.sprites.Length; j++)
                {
                    if (bin.sprites[j] == sprite)
                    {
                        iSprite = j;
                        break;
                    }
                }
                if (iSprite != -1)
                {
                    iBin = i;
                    break;
                }
            }
        }

        public static bool CheckTextureTranparency(TextureFormat format)
        {
            return format == TextureFormat.ARGB32 ||
                format == TextureFormat.RGBA32 ||
                format == TextureFormat.ARGB4444 ||
                format == TextureFormat.RGBA4444 ||
                format == TextureFormat.PVRTC_RGBA4 ||
                format == TextureFormat.PVRTC_RGBA2 ||
                format == TextureFormat.ETC2_RGBA1 ||
                format == TextureFormat.ETC2_RGBA8 ||
                format == TextureFormat.DXT5;
        }

        public static string GetDisplayName(BinRaw bin)
        {
            var main = bin.main;
            var size = bin.size;
            var quality = bin.quality;
            var builder = new StringBuilder();
            builder.Append(main.name);
            builder.Append(string.Format("  [{0}x{1}]", (int)size.x, (int)size.y));
            builder.Append(string.Format("  [{0}]", quality.ToString().ToLower()));
            var bits = GetBinCapacity(bin);
            if (bits > 0) builder.Append(string.Format("  [{0}bits]", bits));
            return builder.ToString();
        }

        public static int GetBinCapacity(BinRaw bin)
        {
            var bits = 0;
            var quality = bin.quality;
            var tranparency = bin.transparency;
#if UNITY_EDITOR
            var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
#else
            var buildTarget = Application.platform;
#endif
            switch (buildTarget)
            {
#if UNITY_EDITOR
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
#else
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
#endif
                    {
                        switch (quality)
                        {
                            case BinRaw.Quality.Full:
                                bits = tranparency ? 32 : 24;
                                break;
                            case BinRaw.Quality.RGB16A4:
                                bits = tranparency ? 20 : 16;
                                break;
                            case BinRaw.Quality.Normal:
                                bits = tranparency ? 8 : 4;
                                break;
                            case BinRaw.Quality.Legacy:
                                bits = 8;
                                break;
                        }
                        break;
                    }
#if UNITY_EDITOR
                case UnityEditor.BuildTarget.iOS:
#else
                case RuntimePlatform.IPhonePlayer:
#endif
                    {
                        switch (quality)
                        {
                            case BinRaw.Quality.Full:
                                bits = tranparency ? 32 : 24;
                                break;
                            case BinRaw.Quality.RGB16A4:
                                bits = tranparency ? 20 : 16;
                                break;
                            case BinRaw.Quality.Normal:
                                bits = tranparency ? 8 : 4;
                                break;
                            case BinRaw.Quality.Legacy:
                                bits = 16;
                                break;
                        }
                        break;
                    }
#if UNITY_EDITOR
                case UnityEditor.BuildTarget.Android:
#else
                case RuntimePlatform.Android:
#endif
                    {
                        switch (quality)
                        {
                            case BinRaw.Quality.Full:
                                bits = tranparency ? 32 : 24;
                                break;
                            case BinRaw.Quality.RGB16A4:
                                bits = tranparency ? 20 : 16;
                                break;
                            case BinRaw.Quality.Normal:
                                bits = tranparency ? 8 : 4;
                                break;
                            case BinRaw.Quality.Legacy:
                                bits = 8;
                                break;
                        }
                        break;
                    }
            }
            return bits;
        }
    }
}
