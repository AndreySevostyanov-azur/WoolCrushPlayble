using System;
using Azur.Playable.UI;
using Azur.PlayableTemplate.Input;
using UnityEngine;

namespace Azur.Playable
{
    public class Level : MonoBehaviour
    {
        [SerializeField] private Screens _screens;
        [SerializeField] private InputFacade _input;
        
        public event Action GameEnded;

        private void StopLevel()
        {
            GameEnded?.Invoke();
        }
    }
}