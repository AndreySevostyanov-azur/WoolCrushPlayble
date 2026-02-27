using UnityEngine;

namespace Playeble.Scripts.Gameplay.Selection
{
    public struct BlockClickedEvent
    {
        public ClickableBlockView Block;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
    }
}

