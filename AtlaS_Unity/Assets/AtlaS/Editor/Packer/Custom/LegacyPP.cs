#if AtlaS_ON
using UnityEngine;

namespace UnityEditor.UI.Atlas
{
    [PackQuality(PackQuality.Legacy)]
    public class LegacyPP : PackPostProcessor
    {
        protected override void GetStandaloneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = TextureImporterFormat.DXT5,
            };
            add = null;
        }

        protected override void GetiPhoneSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = transparency ? TextureImporterFormat.RGBA16 : TextureImporterFormat.RGB16,
            };
            add = null;
        }

        protected override void GetAndroidSettings(out TextureImporterPlatformSettings main, out TextureImporterPlatformSettings add)
        {
            main = new TextureImporterPlatformSettings()
            {
                overridden = true,
                maxTextureSize = maxTextureSize,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                format = transparency ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4,
            };
            add = null;
        }
    }
}
#endif