using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Azur.PlayableTemplate.Helpers
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class AppearOfTextMeshPro : MonoBehaviour
    {
        [SerializeField] private float _endValue = 1f;
        [SerializeField] private float _duration = 1.5f;

        private TextMeshProUGUI _textMeshPro;

        public event Action Showed;

        private void Awake()
        {
            _textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        private void DOKill()
        {
            _textMeshPro.DOKill();
        }

        private void OnEnable()
        {
            DOKill();
            Show();
        }

        private void Show()
        {
            Color old = _textMeshPro.color;
            _textMeshPro.color = new Color(old.r, old.g, old.b, 0f);
            _textMeshPro.DOFade(_endValue, _duration).OnComplete(OnComplete);
        }

        private void OnComplete()
        {
            Showed?.Invoke();
        }
    }
}