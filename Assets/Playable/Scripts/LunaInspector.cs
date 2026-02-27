using UnityEngine;

namespace Azur.PlayableTemplate.LunaPlayable
{
    public class LunaInspector : MonoBehaviour
    {
        [Header("Effects")] 
        
        [LunaPlaygroundField("Громкость музыки", 0, "Эффекты")]
        [Range(0f, 1f)]
        [SerializeField] private float _music = 1f;
        
        [LunaPlaygroundField("Громкость звуков", 1, "Эффекты")]
        [Range(0f, 1f)]
        [SerializeField] private float _sound = 1f;
        
        [Header("Redirect")] 
        
        [LunaPlaygroundField("Количество нажатий до перехода в стор", 0, "Редирект")]
        [SerializeField] private int _clickCount = 9999;

        public float Music => _music;
        public float Sound => _sound;
        public int ClickCount => _clickCount;
    }
}