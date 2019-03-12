#if AtlaS_ON
using System;

namespace UnityEditor.UI.Atlas
{
    [Serializable]
    public class PackSetting
    {
        public int maxAtlasSize;

        public int padding;

        public bool isPOT;

        public bool forceSquare;

        public PackSetting()
        {
            maxAtlasSize = 1024;
            padding = 1;
            isPOT = true;
            forceSquare = false;
        }

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