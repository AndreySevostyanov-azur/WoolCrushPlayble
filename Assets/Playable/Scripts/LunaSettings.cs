using Azur.Playable;
using Azur.PlayableTemplate.Input;
using Azur.PlayableTemplate.Sound;
using Luna.Unity;
using UnityEngine;

namespace Azur.PlayableTemplate.LunaPlayable
{
    [RequireComponent(typeof(LunaInspector))]
    public class LunaSettings : MonoBehaviour
    {
        [SerializeField] private InputFacade _input;
        [SerializeField] private Level _level;
        
        private const string _audioKey = "Luna";
        private ClickCounter _clickCounter;
        private LunaInspector _inspector;
        private bool _isGameEnded;

        private void Awake()
        {
            _inspector = GetComponent<LunaInspector>();
            _clickCounter = new ClickCounter(_inspector.ClickCount);
        }

        private void OnEnable()
        {
            _input.DownTouched += _clickCounter.RegistrateClick;
            _clickCounter.LimitReached += InstallFullGame;
			_clickCounter.OneClickLeft += GameEnded;
            _level.GameEnded += GameEnded;
        }

        private void Start()
        {
            Audio.Instance.AddVolumeSettings(_audioKey, _inspector.Music, _inspector.Sound);
			_clickCounter.CheckOneClickLeft();
        }

        public void InstallFullGame()
        {        
            Luna.Unity.Playable.InstallFullGame();
        }

        public void GameEnded()
        {
            if (_isGameEnded)
            {
                Debug.LogWarning("GameEnded method was called more than once. This is not allowed.");
				return;
			}

			_isGameEnded = true;
            _clickCounter.OneClickLeft -= GameEnded;
            _level.GameEnded -= GameEnded;
            _clickCounter.ForceComplete();
            LifeCycle.GameEnded();
        }

        private void OnDisable()
        {
            _input.DownTouched -= _clickCounter.RegistrateClick;
            _clickCounter.LimitReached -= InstallFullGame;
        }
    }
}