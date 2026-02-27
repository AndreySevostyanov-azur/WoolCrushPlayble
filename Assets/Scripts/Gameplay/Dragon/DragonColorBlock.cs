using AzurGames.Wool.Gameplay;
using Playeble.Scripts.Gameplay;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    public sealed class DragonColorBlock : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Colors _type = Colors.White;
        [SerializeField] private BoxType _boxType = BoxType.Small;
        [Min(0)]
        [SerializeField] private int _turns;
        [Range(0, 9)]
        [SerializeField] private int _colorOffset;
        [SerializeField] private bool _randomizeColorOnAwake = true;

        [Header("Visual")]
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private MeshFilter _meshFilter;

        [Header("Movement")]
        [SerializeField] private DragonColorBlock[] _blockingBlocks;

        private static readonly int ColorOffsetId = Shader.PropertyToID("_ColorOffset");
        private static readonly int ColorOffsetIdLower = Shader.PropertyToID("_colorOffset");
        private static readonly Colors[] RandomColors =
        {
            Colors.Red,
            Colors.Yellow,
            Colors.Cyan,
            Colors.Purple,
            Colors.Pink,
            Colors.Green,
            Colors.Blue,
            Colors.Orange,
        };
        private MaterialPropertyBlock _mpb;

        public Colors Type
        {
            get { return _type; }
        }

        public BoxType BoxType
        {
            get { return _boxType; }
        }

        public int Turns
        {
            get { return _turns; }
        }

        public int ColorOffset
        {
            get { return _colorOffset; }
        }

        public Mesh Mesh
        {
            get { return _meshFilter != null ? _meshFilter.sharedMesh : null; }
        }

        public DragonColorBlock[] BlockingBlocks
        {
            get { return _blockingBlocks; }
        }

#if UNITY_EDITOR
        public void EditorSetBlockingBlocks(DragonColorBlock[] blocks)
        {
            _blockingBlocks = blocks;
        }
#endif

        private void Reset()
        {
            AutoBind();
        }

        private void Awake()
        {
            if (_randomizeColorOnAwake)
            {
                RandomizeColorAndApply();
            }
            else
            {
                ApplyVisual();
            }
        }

        private void OnValidate()
        {
            AutoBind();
            if (!Application.isPlaying)
            {
                ApplyVisual();
            }
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

        public void ApplyVisual()
        {
            if (_renderer == null)
            {
                return;
            }

            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ColorOffsetId, Mathf.Clamp(_colorOffset, 0, 9));
            _mpb.SetFloat(ColorOffsetIdLower, Mathf.Clamp(_colorOffset, 0, 9));
            _renderer.SetPropertyBlock(_mpb);
        }

        public void RandomizeColorAndApply()
        {
            var next = PickRandomColor();
            _type = next;
            _colorOffset = Mathf.Clamp(next.GetOffset(), 0, 9);
            ApplyVisual();
        }

        private static Colors PickRandomColor()
        {
            if (RandomColors == null || RandomColors.Length == 0)
            {
                return Colors.White;
            }

            var idx = Random.Range(0, RandomColors.Length);
            if (idx < 0) idx = 0;
            if (idx >= RandomColors.Length) idx = RandomColors.Length - 1;
            return RandomColors[idx];
        }
    }
}

