using System;

namespace AtlaS
{
    [Serializable]
    public class AtlasPackData
    {
        public int maxAtlasSize;

        public int padding;

        public bool isPOT;

        public bool forceSquare;

        public bool removeFragmentWhenPackingOver;

        public AtlasPackData()
        {
            maxAtlasSize = 1024;
            padding = 1;
            isPOT = true;
            forceSquare = false;
            removeFragmentWhenPackingOver = false;
        }

        public AtlasPackData(int maxAtlasSize, int padding, bool isPOT, bool forceSquare, bool removeFragmentWhenPackingOver)
        {
            this.maxAtlasSize = maxAtlasSize;
            this.padding = padding;
            this.isPOT = isPOT;
            this.forceSquare = forceSquare;
            this.removeFragmentWhenPackingOver = removeFragmentWhenPackingOver;
        }

        public AtlasPackData(AtlasPackData data)
        {
            maxAtlasSize = data.maxAtlasSize;
            padding = data.padding;
            isPOT = data.isPOT;
            forceSquare = data.forceSquare;
            removeFragmentWhenPackingOver = data.removeFragmentWhenPackingOver;
        }
    }
}
