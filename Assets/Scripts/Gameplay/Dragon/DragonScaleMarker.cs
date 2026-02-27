using Playeble.Scripts.Gameplay;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class DragonScaleMarker : MonoBehaviour
    {
        [SerializeField] private Colors _color = Colors.White;
        [SerializeField] private bool _isUnwinding;
        [SerializeField] private MeshRenderer _renderer;

        private static readonly int CutoutIdLower = Shader.PropertyToID("_cutout");
        private static readonly int CutoutIdUpper = Shader.PropertyToID("_Cutoff");
        private MaterialPropertyBlock _mpb;

        public Colors Color
        {
            get { return _color; }
        }

        public bool IsUnwinding
        {
            get { return _isUnwinding; }
        }

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
        }

        public void Setup(Colors color)
        {
            _color = color;
            _isUnwinding = false;
            SetCutout(1f);
        }

        public void SetUnwinding(bool isUnwinding)
        {
            _isUnwinding = isUnwinding;
        }

        public void SetCutout(float value01)
        {
            if (_renderer == null)
            {
                return;
            }

            var v = Mathf.Clamp01(value01);
            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(CutoutIdLower, v);
            _mpb.SetFloat(CutoutIdUpper, 1f - v);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}

