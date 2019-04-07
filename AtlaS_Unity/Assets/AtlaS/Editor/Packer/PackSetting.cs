#if AtlaS_ON
using System;

namespace UnityEditor.UI.Atlas
{
    [Serializable]
    public class PackSetting
    {
        public int maxAtlasSize = 1024;

        public int padding = 1;

        public bool isPOT = false;

        public bool forceSquare = false;

        public PackSetting() { }

        public PackSetting(int maxAtlasSize, int padding, bool isPOT, bool forceSquare)
        {
            this.maxAtlasSize = maxAtlasSize;
            this.padding = padding;
            this.isPOT = isPOT;
            this.forceSquare = forceSquare;
        }

        public PackSetting(PackSetting data)
        {
            maxAtlasSize = data.maxAtlasSize;
            padding = data.padding;
            isPOT = data.isPOT;
            forceSquare = data.forceSquare;
        }
    }
}
#endif