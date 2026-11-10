using System.Collections.Generic;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    [RequireComponent(typeof(DragonColorBlock))]
    public sealed class ChainedBlock : MonoBehaviour
    {
        [SerializeField] private BlockMechanicsConfig _config;
        [Tooltip("Блоки, которые нужно убрать с поля, чтобы снять цепи с этого блока")]
        [SerializeField] private List<DragonColorBlock> _blockers = new List<DragonColorBlock>();

        public BlockMechanicsConfig Config => _config;
        public IList<DragonColorBlock> Blockers => _blockers;

#if UNITY_EDITOR
        public void EditorSetConfig(BlockMechanicsConfig config)
        {
            _config = config;
        }

        private void OnDrawGizmos()
        {
            if (_blockers == null)
            {
                return;
            }

            var offset = _config != null ? _config.AnchorLocalOffset : new Vector3(0f, 0.9f, 0f);
            var lockPosition = transform.TransformPoint(offset);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lockPosition, 0.15f);
            for (var i = 0; i < _blockers.Count; i++)
            {
                var blocker = _blockers[i];
                if (blocker == null)
                {
                    continue;
                }

                var pinPosition = blocker.transform.TransformPoint(offset);
                Gizmos.DrawLine(lockPosition, pinPosition);
                Gizmos.DrawWireCube(pinPosition, Vector3.one * 0.18f);
            }
        }
#endif
    }
}
