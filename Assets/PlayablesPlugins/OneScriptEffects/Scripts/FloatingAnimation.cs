using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Azur.PlayableTemplate.Effects
{
    public class FloatingAnimation : MonoBehaviour
    {
        private enum Axis
        {
            X, Y, Z
        }
        
        [SerializeField] private Axis _axis = Axis.Y;
        [SerializeField] private float _amplitude;
        [SerializeField] private float _period;
        [SerializeField] private Ease _ease = Ease.Linear;

        [Space]
        [Tooltip("If is null, set this transform")]
        [SerializeField] private Transform _target;

        [Space]
        [SerializeField] private bool _playOnEnable = true;

        private Sequence _floating;

        private Dictionary<Axis, Vector3> _directionsMap = new Dictionary<Axis, Vector3>()
        {
            { Axis.X, Vector3.right },
            { Axis.Y, Vector3.up },
            { Axis.Z, Vector3.forward}
        };

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
            if (_floating.IsPlaying())
                Stop();
        }

        public void Play(float delay = 0)
        {
            Vector3 upperPosition = _target.localPosition + _directionsMap[_axis] * _amplitude;
            Vector3 bottomPosition = _target.localPosition + _directionsMap[_axis] * -1 * _amplitude;
            
            _floating = DOTween.Sequence();
            _floating.AppendCallback(() =>
            {
                _target.DOLocalMove(upperPosition, _period).
                    SetLoops(-1, LoopType.Yoyo).SetEase(_ease).From(bottomPosition);
            });
            _floating.SetDelay(delay);

            _floating.Play();
        }

        public void Stop()
        {
            _floating.Kill();
        }
    }
}