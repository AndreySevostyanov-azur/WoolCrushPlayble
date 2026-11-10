using System;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    [CreateAssetMenu(menuName = "Playable/Block Mechanics Config", fileName = "BlockMechanicsConfig")]
    public sealed class BlockMechanicsConfig : ScriptableObject
    {
        [Serializable]
        public struct IceMeshEntry
        {
            public Mesh Source;
            public Mesh Ice;
        }

        [Header("Hidden")]
        [SerializeField] private Material _hiddenMaterial;
        [Min(0.01f)] [SerializeField] private float _hiddenPulseDuration = 3f;
        [Range(1, 20)] [SerializeField] private int _hiddenPulseCount = 3;
        [Min(0f)] [SerializeField] private float _hiddenSolidDuration = 1f;
        [SerializeField] private Vector3 _hiddenOverlayScale = new Vector3(1.02f, 1.01f, 1.02f);

        [Header("Ice")]
        [SerializeField] private Material _iceMaterial;
        [SerializeField] private IceMeshEntry[] _iceMeshes;
        [SerializeField] private Vector3 _iceLocalEuler = new Vector3(0f, 180f, 0f);
        [Min(0.01f)] [SerializeField] private float _meltDuration = 0.5f;
        [SerializeField] private GameObject _unfreezeVfx;

        [Header("Chain")]
        [SerializeField] private Sprite _chainSprite;
        [SerializeField] private Sprite _lockSprite;
        [SerializeField] private Sprite _pinSprite;
        [SerializeField] private Material _spriteMaterial;
        [Min(0.01f)] [SerializeField] private float _chainScale = 0.19f;
        [Range(0.2f, 1.5f)] [SerializeField] private float _chainLinkSpacing = 0.9f;
        [SerializeField] private float _chainAngleOffset;
        [Min(0.01f)] [SerializeField] private float _lockScale = 0.19f;
        [Min(0.01f)] [SerializeField] private float _pinScale = 0.19f;
        [Tooltip("Точка крепления замка/пина в локальных координатах блока")]
        [SerializeField] private Vector3 _anchorLocalOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private int _sortingOrder = 50;
        [SerializeField] private GameObject _chainBreakVfx;
        [SerializeField] private GameObject _lockBreakVfx;

        [Header("Feedback")]
        [Min(0f)] [SerializeField] private float _nudgeDistance = 0.2f;
        [Min(0.01f)] [SerializeField] private float _nudgeDuration = 0.1f;
        [SerializeField] private GameObject _rejectMarkerPrefab;
        [Min(0f)] [SerializeField] private float _vfxLifetime = 2f;

        public Material HiddenMaterial => _hiddenMaterial;
        public float HiddenPulseDuration => _hiddenPulseDuration;
        public int HiddenPulseCount => _hiddenPulseCount;
        public float HiddenSolidDuration => _hiddenSolidDuration;
        public Vector3 HiddenOverlayScale => _hiddenOverlayScale;

        public Material IceMaterial => _iceMaterial;
        public Vector3 IceLocalEuler => _iceLocalEuler;
        public float MeltDuration => _meltDuration;
        public GameObject UnfreezeVfx => _unfreezeVfx;

        public Sprite ChainSprite => _chainSprite;
        public Sprite LockSprite => _lockSprite;
        public Sprite PinSprite => _pinSprite;
        public Material SpriteMaterial => _spriteMaterial;
        public float ChainScale => _chainScale;
        public float ChainLinkSpacing => _chainLinkSpacing;
        public float ChainAngleOffset => _chainAngleOffset;
        public float LockScale => _lockScale;
        public float PinScale => _pinScale;
        public Vector3 AnchorLocalOffset => _anchorLocalOffset;
        public int SortingOrder => _sortingOrder;
        public GameObject ChainBreakVfx => _chainBreakVfx;
        public GameObject LockBreakVfx => _lockBreakVfx;

        public float NudgeDistance => _nudgeDistance;
        public float NudgeDuration => _nudgeDuration;
        public GameObject RejectMarkerPrefab => _rejectMarkerPrefab;
        public float VfxLifetime => _vfxLifetime;

        public Mesh FindIceMesh(Mesh source)
        {
            if (_iceMeshes == null)
            {
                return null;
            }

            for (var i = 0; i < _iceMeshes.Length; i++)
            {
                if (_iceMeshes[i].Source == source)
                {
                    return _iceMeshes[i].Ice;
                }
            }

            return null;
        }
    }
}
