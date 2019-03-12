#if AtlaS_ON
namespace UnityEditor.UI.Atlas
{
    [PackQuality(PackQuality.Legacy)]
    public class LegacyP : PackProcessor
    {
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