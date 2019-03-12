namespace UnityEngine.UI.Atlas
{
    [RequireComponent(typeof(Image))]
    public class ImageSpriteAnimationHook : MonoBehaviour
    {
#if AtlaS_ON
        public UnityEngine.Sprite sprite;

        private UnityEngine.UI.Sprite mCurrent;

        private Image mImage;

        void Awake()
        {
            mImage = GetComponent<Image>();
            mCurrent = mImage.sprite;
            sprite = mCurrent.sprite;
        }

        void Update()
        {
            if (mCurrent.sprite != sprite)
            {
                mCurrent = new UnityEngine.UI.Sprite(sprite);
                mImage.sprite = mCurrent;
            }
        }
#endif
    }
}