using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Azur.PlayableTemplate.Helpers
{
    [RequireComponent(typeof(Text))]
    public class AppearOfText : MonoBehaviour
    {
        [SerializeField] private float _endValue = 1f;
        [SerializeField] private float _duration = 1.5f;

        private Text _text;

        public event Action Showed;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        private void OnEnable()
        {
            DOKill();
            Show();
        }

        private void DOKill()
        {
            _text.DOKill();
        }

        private void Show()
        {
            Color old = _text.color;
            _text.color = new Color(old.r, old.g, old.b, 0f);
            _text.DOFade(_endValue, _duration).OnComplete(OnComplete);
        }

        private void OnComplete()
        {
            Showed?.Invoke();
        }
    }
}