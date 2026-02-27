using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Azur.PlayableTemplate.Helpers
{
    [RequireComponent(typeof(Image))]
    public class AppearOfImage : MonoBehaviour
    {
        [SerializeField] private float _endValue = 1f;
        [SerializeField] private float _duration = 1.5f;

        private Image _image;

        public event Action Showed;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            DOKill();
            Show();
        }

        private void DOKill()
        {
            _image.DOKill();
        }

        private void Show()
        {
            Color old = _image.color;
            _image.color = new Color(old.r, old.g, old.b, 0f);
            _image.DOFade(_endValue, _duration).OnComplete(OnComplete);
        }

        private void OnComplete()
        {
            Showed?.Invoke();
        }
    }
}
