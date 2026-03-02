using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public class DragonHeadView : MonoBehaviour
    {
        [SerializeField] private Animator _dragonHeadAnimator;
        [SerializeField] private GameObject _dragonBreathObject;
        [SerializeField] private GameObject _dragonDeathObject;

        public Animator DragonHeadAnimator => _dragonHeadAnimator;

        public GameObject DragonBreathObject => _dragonBreathObject;

        public GameObject DragonDeathObject => _dragonDeathObject;
    }
}