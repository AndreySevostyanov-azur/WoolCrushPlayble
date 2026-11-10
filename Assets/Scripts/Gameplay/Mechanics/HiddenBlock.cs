using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    [RequireComponent(typeof(DragonColorBlock))]
    public sealed class HiddenBlock : MonoBehaviour
    {
        [SerializeField] private BlockMechanicsConfig _config;
        [Tooltip("Блок скрыт с первого кадра, без пульсации после старта")]
        [SerializeField] private bool _instant;
        [HideInInspector] [SerializeField] private MeshRenderer _overlay;

        public BlockMechanicsConfig Config => _config;
        public bool Instant => _instant;
        public MeshRenderer Overlay => _overlay;

#if UNITY_EDITOR
        public void EditorSetup(BlockMechanicsConfig config, MeshRenderer overlay)
        {
            _config = config;
            _overlay = overlay;
        }

        [ContextMenu("Build View")]
        public void EditorBuildView()
        {
            UnityEditor.Undo.RecordObject(this, "Build Hidden View");
            if (_config == null)
            {
                _config = BlockMechanicViews.EditorFindConfig();
            }

            _overlay = BlockMechanicViews.EditorEnsureOverlay(GetComponent<DragonColorBlock>(), BlockMechanicViews.HiddenName, true, _config);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void Reset()
        {
            UnityEditor.EditorApplication.delayCall += BuildViewDelayed;
        }

        private void BuildViewDelayed()
        {
            if (this != null)
            {
                EditorBuildView();
            }
        }
#endif
    }
}
