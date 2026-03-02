using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    [CreateAssetMenu(menuName = "Playable/Dragon/Variable Acceleration", fileName = "DragonVariableAccelerationSettings")]
    public sealed class VariableAccelerationSettings : ScriptableObject
    {
        [Header("Step multipliers (no smoothing)")]
        [Min(0f)] [SerializeField] private float _accelerationUntilDistance = 2f;
        [Min(0f)] [SerializeField] private float _accelerationMultiplier = 1.35f;

        [Min(0f)] [SerializeField] private float _decelerationRemainingDistance = 2f;
        [Min(0f)] [SerializeField] private float _decelerationMultiplier = 0.6f;

        [Header("Hard stop")]
        [Min(0f)] [SerializeField] private float _endStopDistance = 0.01f;

        public float AccelerationUntilDistance => _accelerationUntilDistance;
        public float AccelerationMultiplier => _accelerationMultiplier;
        public float DecelerationRemainingDistance => _decelerationRemainingDistance;
        public float DecelerationMultiplier => _decelerationMultiplier;
        public float EndStopDistance => _endStopDistance;

        private void OnValidate()
        {
            if (_accelerationUntilDistance < 0f) _accelerationUntilDistance = 0f;
            if (_accelerationMultiplier < 0f) _accelerationMultiplier = 0f;

            if (_decelerationRemainingDistance < 0f) _decelerationRemainingDistance = 0f;
            if (_decelerationMultiplier < 0f) _decelerationMultiplier = 0f;
            if (_endStopDistance < 0f) _endStopDistance = 0f;
        }

        public float EvaluateSpeed(float baseSpeed, float distance, float pathLength)
        {
            if (baseSpeed < 0f) baseSpeed = 0f;
            if (pathLength <= 0.0001f)
            {
                return baseSpeed;
            }

            if (distance < 0f) distance = 0f;
            if (distance > pathLength) distance = pathLength;

            var remaining = pathLength - distance;
            if (_endStopDistance > 0f && remaining <= _endStopDistance)
            {
                return 0f;
            }

            var speed = baseSpeed;

            if (_accelerationMultiplier > 0f
                && _accelerationUntilDistance > 0f
                && distance < _accelerationUntilDistance)
            {
                speed *= _accelerationMultiplier;
            }

            if (_decelerationMultiplier > 0f
                && _decelerationRemainingDistance > 0f
                && remaining <= _decelerationRemainingDistance)
            {
                speed *= _decelerationMultiplier;
            }

            if (speed < 0f) speed = 0f;
            return speed;
        }
    }
}

