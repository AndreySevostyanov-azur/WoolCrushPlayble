using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Playable/DragonBreathConfig", fileName = "DragonBreathConfig")]
    public class DragonBreathConfig : ScriptableObject
    {
        [SerializeField] private float _breathDuration;
        [SerializeField] private float _breathInterval;
        
        public float BreathDuration => _breathDuration;
        public float BreathInterval => _breathInterval;
    }
}