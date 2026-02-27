using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Azur.PlayableTemplate.Helpers
{
    public class BounceAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float _intensity = -0.1f;
        [SerializeField] private float _durationDown = 0.1f;
        [SerializeField] private float _durationUp = 0.4f;
        [SerializeField] private ButtonAnimationtype _animationType = ButtonAnimationtype.Elastic;

        private Vector3 _startScale;

        private enum ButtonAnimationtype
        {
            Linear,
            Elastic,
        }

        private void Awake()
        {
            _startScale = transform.localScale;
        }

        public void OnMouseDown()
        {
            Scale();
        }

        public void OnMouseUp()
        {
            UnScale();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Scale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UnScale();
        }

        public void OnPointerUp(PointerEventData data)
        {
            UnScale();
        }

        private void Scale()
        {
            transform.DOKill();
            transform.DOScale(_startScale + _intensity * Vector3.one, _durationDown).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void UnScale()
        {
            transform.DOKill();

            switch (_animationType)
            {
                case ButtonAnimationtype.Elastic:
                    transform.DOScale(_startScale, _durationUp).SetEase(Ease.OutElastic).SetUpdate(true);
                    break;

                case ButtonAnimationtype.Linear:
                    transform.DOScale(_startScale, _durationUp).SetEase(Ease.OutCubic).SetUpdate(true);
                    break;
            }
        }
    }
}