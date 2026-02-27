using AzurGames.Wool.Gameplay.Rope;
using UnityEngine;
using UnityEngine.UI;

namespace AzurGames.Wool.Gameplay
{
    public sealed class SlotView: MonoBehaviour
    {
        [SerializeField]
        private CounterView _remaining;
        public CounterView RemainReelText => _remaining;
        public GameObject SpoolObject;
        public RectTransform Rect;
        public Button UnlockButton;
        public GameObject LockVisual;
        public SpoolVisualSet[] Visuals;
        public RopeSpriteShape Rope;

        [SerializeField] private ParticleSystem _unlockVfx;
        [SerializeField] private ParticleSystem _amplifyBackVfx;
        [SerializeField] private ParticleSystem _amplifyFrontVfx;
        [SerializeField] private ParticleSystem _newReelVfx;
        [SerializeField] private ParticleSystem _removeReelVfx;
        [SerializeField] private ParticleSystem _newCoilVfx;

        private int? _remainingValue = null;
        private SlotStates _status;

        public ParticleSystem UnlockVfx => _unlockVfx;
        public ParticleSystem AmplifyBackVfx => _amplifyBackVfx;
        public ParticleSystem AmplifyFrontVfx => _amplifyFrontVfx;
        public ParticleSystem NewReelVfx => _newReelVfx;
        public ParticleSystem RemoveReelVfx => _removeReelVfx;
        public ParticleSystem NewCoilVfx => _newCoilVfx;

        public void ShowSpool(bool isVisible = true)
        {
            if (SpoolObject != null && SpoolObject.activeSelf != isVisible)
            {
                SpoolObject.SetActive(isVisible);
            }
        }

        public void Refresh(SlotStates value)
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            switch (value)
            {
                case SlotStates.Empty:
                    break;

                case SlotStates.Reserved:
                    break;

                case SlotStates.Occupied:
                    NewReelVfx.Play();
                    break;

                case SlotStates.Dispose:
                    Rope.Show(false);
                    RemoveReelVfx.Play();
                    break;
            }
        }
    }
}