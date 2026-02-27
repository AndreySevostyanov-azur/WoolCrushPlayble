using System;
using DG.Tweening;
using UnityEngine;

namespace Azur.PlayableTemplate.Effects
{
    public class PulseObjectAnimation : MonoBehaviour
    {
        [SerializeField] private Vector3 _sizeEnd = new Vector3(0.8f, 0.8f, 0.8f);
        [SerializeField] private float _speed = 0.5f;
        [SerializeField] private Ease _ease = Ease.Linear;

        [Space]
        [Tooltip("If is null, set this transform")]
        [SerializeField] private Transform _target;
        
        [Space]
        [SerializeField] private bool _playOnEnable = true;
        
        private Sequence _pulse;

        private void Awake()
        {
            if (_target == null)
                _target = transform;
        }

        private void OnEnable()
        {
            if (_playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Play(float delay = 0f)
        {
            _pulse = DOTween.Sequence();
            _pulse.AppendCallback(() =>
            {
                _target.DOScale(_sizeEnd, _speed).
                    SetEase(_ease).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            });
            _pulse.SetDelay(delay);

            _pulse.Play();
        }

        public void Stop()
        {
            _pulse.Kill();
        }
    }
}