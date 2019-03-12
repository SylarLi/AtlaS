#if AtlaS_ON
namespace UnityEditor.UI.Atlas
{
    public sealed class PackConst
    {
        public static readonly int[] AtlasSizeList = new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };

        public const int MaxNumberOfSpriteInAtlas = 65535;

        public const string DefaultAtlasAssetName = "_atlas.asset";
    }
}
#endif