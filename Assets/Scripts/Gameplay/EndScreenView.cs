using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class EndScreenView : MonoBehaviour
    {
        [SerializeField] private Button _goToStoreButton;

        private void Awake()
        {
            _goToStoreButton.onClick.AddListener(GoToStore);
        }

        private void GoToStore()
        {
            _goToStoreButton.onClick.RemoveListener(GoToStore);
            Luna.Unity.Playable.InstallFullGame();
        }
    }
}