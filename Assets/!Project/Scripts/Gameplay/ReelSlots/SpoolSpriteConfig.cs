using System;
using System.Collections.Generic;
using Playeble.Scripts.Gameplay;
using UnityEngine;

namespace AzurGames.Wool.Gameplay
{
    public class SpoolSpriteConfig : ScriptableObject
    {
        public SpoolSpriteData[] Entries;

        Dictionary<Colors, SpoolSpriteData> _cache;

        void OnEnable()
        {
            _cache = new Dictionary<Colors, SpoolSpriteData>(Entries?.Length ?? 0);
            foreach (var e in Entries)
                _cache[e.SpoolColor] = e;
        }

        public SpoolSpriteData TryGetData(Colors spoolColor)
        {
            SpoolSpriteData spoolSpriteData;
            if (Entries != null)
            {
                for (int i = 0; i < Entries.Length; i++)
                {
                    if (Entries[i].SpoolColor == spoolColor)
                    {
                        spoolSpriteData = Entries[i];
                        return spoolSpriteData;
                    }
                }
            }

            return null;
        }
    }
    
    [Serializable]
    public class SpoolSpriteData
    {
        public Colors SpoolColor; 
        public Sprite Icon; 
        public Sprite CoilSprite;
    }
}