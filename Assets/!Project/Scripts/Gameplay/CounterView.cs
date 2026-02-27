using TMPro;
using UnityEngine;

namespace AzurGames.Wool.Gameplay
{
    public sealed class CounterView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        private int? _value;

        public void Clear()
        {
            _value = null;
            _text.text = string.Empty;
        }

        public void Set(int value)
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            _text.text = value.ToString();
        }
    }
}