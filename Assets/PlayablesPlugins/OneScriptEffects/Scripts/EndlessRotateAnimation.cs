using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Azur.PlayableTemplate.Effects
{
    public class EndlessRotateAnimation : MonoBehaviour
    {
        private enum Axis
        {
            X, Y, Z, rX, rY, rZ
        }

        [SerializeField] private Axis _axis;
        [SerializeField] private float _rotationPeriod = 0.5f;
        [SerializeField] private Ease _ease = Ease.Linear;

        [Space]
        [Tooltip("If is null, set this transform")]
        [SerializeField] private Transform _target;

        [Space]
        [SerializeField] private bool _playOnEnable = true;

        private Sequence _endlessRotate;

        private Vector3 _rotationVector;

        private Dictionary<Axis, Vector3> _axisRotationMap = new Dictionary<Axis, Vector3>()
        {
            { Axis.X, new Vector3(360f, 0f, 0f)},
            { Axis.Y, new Vector3(0f, 360f, 0f)},
            { Axis.Z, new Vector3(0f, 0f, 360f)},
            { Axis.rX, new Vector3(-360f, 0f, 0f)},
            { Axis.rY, new Vector3(0f, -360f, 0f)},
            { Axis.rZ, new Vector3(0f, 0f, -360f)}
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
            if (_endlessRotate.IsPlaying())
                Stop();
        }

        public void Play(float delay = 0)
        {
            _rotationVector = _axisRotationMap[_axis];

            _endlessRotate = DOTween.Sequence();
            _endlessRotate.AppendCallback(() =>
            {
                _target.DOLocalRotate(_rotationVector, _rotationPeriod).
                    SetRelative().SetLoops(-1).SetEase(_ease);
            });
            _endlessRotate.SetDelay(delay);
            
            _endlessRotate.Play();
        }

        public void Stop()
        {
            _endlessRotate.Kill();
        }
    }
}
