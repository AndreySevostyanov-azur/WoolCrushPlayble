using AzurGames.Wool.Gameplay;
using UnityEngine;

namespace AzurGames.Wool.UI
{
    public sealed class SlotFeedbacksListener : MonoBehaviour
    {
        [SerializeField] private SlotView _target;

        private void Awake()
        {
            OnSpoolDisappearFinished();
        }

        private void OnDestroy()
        {
        }

        private void OnSpoolDisappearFinished()
        {
            if (_target != null)
            {
                _target.SpoolObject.SetActive(false);
                
                foreach (var visual in _target.Visuals)
                {
                    if (visual?.Root != null)
                    {
                        visual.Root.SetActive(false);
                    }
                }
            }
        }
    }
}