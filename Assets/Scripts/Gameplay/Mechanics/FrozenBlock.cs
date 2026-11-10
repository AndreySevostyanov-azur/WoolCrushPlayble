using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    [RequireComponent(typeof(DragonColorBlock))]
    public sealed class FrozenBlock : MonoBehaviour
    {
        [SerializeField] private BlockMechanicsConfig _config;
        [Tooltip("Дочерний объект Ice. Заполняется командой Build View в контекстном меню компонента")]
        [SerializeField] private MeshRenderer _ice;
        [Tooltip("Точка для VFX разморозки. Пусто = центр блока")]
        [SerializeField] private Transform _vfxPoint;

        public BlockMechanicsConfig Config => _config;
        public MeshRenderer Ice => _ice;
        public Transform VfxPoint => _vfxPoint;

#if UNITY_EDITOR
        public void EditorSetup(BlockMechanicsConfig config, MeshRenderer ice)
        {
            _config = config;
            _ice = ice;
        }

        [ContextMenu("Build View")]
        public void EditorBuildView()
        {
            UnityEditor.Undo.RecordObject(this, "Build Ice View");
            if (_config == null)
            {
                _config = BlockMechanicViews.EditorFindConfig();
            }

            _ice = BlockMechanicViews.EditorEnsureOverlay(GetComponent<DragonColorBlock>(), BlockMechanicViews.IceName, false, _config);
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
