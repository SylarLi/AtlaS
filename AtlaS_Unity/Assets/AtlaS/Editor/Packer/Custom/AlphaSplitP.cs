#if AtlaS_ON
namespace UnityEditor.UI.Atlas
{
    [PackQuality(PackQuality.AlphaSplit)]
    public class AlphaSplitP : PackProcessor
    {
        public override bool alphaSplit
        {
            get
            {
                return true;
            }
        }

        public override bool forcePowerOf2
        {
            get
            {
                return true;
            }
        }

        public override bool forceSquare
        {
            get
            {
                return true;
            }
        }

        public override bool forceMultiplyOf4
        {
            get
            {
                return true;
            }
        }
    }
}
#endif