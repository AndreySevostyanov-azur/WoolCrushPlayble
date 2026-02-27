using System;
using DG.Tweening;
using UnityEngine;

namespace Azur.PlayableTemplate.Helpers
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AppearOfSpriteRenderer : MonoBehaviour
    {
        [SerializeField] private float _endValue = 1f;
        [SerializeField] private float _duration = 1.5f;

        [SerializeField] private SpriteRenderer _spriteRenderer;

        public event Action Showed;
        
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            DOKill();
            Show();
        }

        private void DOKill()
        {
            _spriteRenderer.DOKill();
        }

        private void Show()
        {
            Color old = _spriteRenderer.color;
            _spriteRenderer.color = new Color(old.r, old.g, old.b, 0f);
            _spriteRenderer.DOFade(_endValue, _duration).OnComplete(OnComplete);
        }

        private void OnComplete()
        {
            Showed?.Invoke();
        }
    }
}