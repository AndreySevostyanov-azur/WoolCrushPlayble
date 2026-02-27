using System;
using UnityEngine;

namespace Playeble.Scripts.Gameplay
{
    [CreateAssetMenu(menuName = "Playable/Rope Color Offsets", fileName = "RopeColorOffsetConfig")]
    public sealed class RopeColorOffsetConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Colors Color;
            public float RopeColorOffset;
        }

        [SerializeField] private Entry[] _entries;

        public float GetRopeColorOffset(Colors color)
        {
            if (_entries != null)
            {
                for (var i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i].Color == color)
                    {
                        return _entries[i].RopeColorOffset;
                    }
                }
            }

            // Fallback: use enum bit-index offset.
            return color.GetOffset();
        }
    }
}

