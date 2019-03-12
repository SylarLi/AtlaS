#if AtlaS_ON
using UnityEngine;

namespace UnityEditor.UI.Atlas
{
    [PackQuality(PackQuality.Half)]
    public class HalfPP : PackPostProcessor
    {
        protected override void GetStandaloneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = TextureImporterFormat.RGB16,
            };
            add = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = TextureImporterFormat.DXT1,
            };
        }

        protected override void GetiPhoneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = TextureImporterFormat.RGB16,
            };
            add = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = TextureImporterFormat.PVRTC_RGB4,
            };
        }

        protected override void GetAndroidSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = TextureImporterFormat.RGB16,
            };
            add = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = TextureImporterFormat.ETC2_RGB4,
            };
        }
    }
}
#endif