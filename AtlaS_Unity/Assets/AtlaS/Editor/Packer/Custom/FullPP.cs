#if AtlaS_ON
namespace UnityEditor.UI.Atlas
{
    [PackQuality(PackQuality.Full)]
    public class FullPP : PackPostProcessor
    {
        protected override void GetStandaloneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = false,
                maxTextureSize = maxTextureSize,
                format = transparency ? TextureImporterFormat.RGBA32 : TextureImporterFormat.RGB24,
            };
            add = null;
        }

        protected override void GetiPhoneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = false,
                maxTextureSize = maxTextureSize,
                format = transparency ? TextureImporterFormat.RGBA32 : TextureImporterFormat.RGB24,
            };
            add = null;
        }

        protected override void GetAndroidSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = false,
                maxTextureSize = maxTextureSize,
                format = transparency ? TextureImporterFormat.RGBA32 : TextureImporterFormat.RGB24,
            };
            add = null;
        }
    }
}
#endif