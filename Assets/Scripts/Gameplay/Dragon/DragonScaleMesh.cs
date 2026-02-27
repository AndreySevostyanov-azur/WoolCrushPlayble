using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class DragonScaleMesh : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private MeshFilter _meshFilter;

        private static readonly int ColorOffsetId = Shader.PropertyToID("_ColorOffset");
        private static readonly int ColorOffsetIdLower = Shader.PropertyToID("_colorOffset");
        private MaterialPropertyBlock _mpb;

        public MeshRenderer Renderer => _renderer;
        public Mesh Mesh => _meshFilter != null ? _meshFilter.sharedMesh : null;

        private void Reset()
        {
            AutoBind();
        }

        private void OnValidate()
        {
            AutoBind();
        }

        private void AutoBind()
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<MeshRenderer>(true);
            }

            if (_meshFilter == null)
            {
                _meshFilter = GetComponentInChildren<MeshFilter>(true);
            }
        }

        public void ApplyColorOffset(int offset)
        {
            if (_renderer == null)
            {
                return;
            }

            if (offset < 0) offset = 0;
            if (offset > 9) offset = 9;

            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ColorOffsetId, offset);
            _mpb.SetFloat(ColorOffsetIdLower, offset);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}

