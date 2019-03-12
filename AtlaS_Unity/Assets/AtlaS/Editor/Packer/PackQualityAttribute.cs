#if AtlaS_ON
using System;

namespace UnityEditor.UI.Atlas
{
    public class PackQualityAttribute : Attribute
    {
        private PackQuality mQuality;
        public PackQuality quality { get { return mQuality; } }

        public PackQualityAttribute(PackQuality quality)
        {
            mQuality = quality;
        }
    }
}
#endif