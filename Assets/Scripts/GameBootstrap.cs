using System;
using System.Collections.Generic;
using AzurGames.Wool.Gameplay;
using Cinemachine;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay;
using Playeble.Scripts.Gameplay.Dragon;
using Playeble.Scripts.Gameplay.Movements;
using Playeble.Scripts.Gameplay.ReelSlots;
using Playeble.Scripts.Gameplay.Selection;
using UnityEngine;
using UnityEngine.UI;

namespace Playeble.Scripts
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _showBootOverlay = true;

        [Header("Dragon (LeoECSLite)")] [SerializeField]
        private CinemachinePath _dragonPath;

        [SerializeField] private Transform _dragonRoot;
        [SerializeField] private GameObject _dragonHeadPrefab;
        [SerializeField] private GameObject _dragonBodyPrefab;
        [SerializeField] private GameObject _dragonTailPrefab;
        [Min(0)] [SerializeField] private int _dragonBodySegmentsCount = 10;
        [Min(0f)] [SerializeField] private float _dragonSegmentSpacing = 0.5f;
        [Min(0f)] [SerializeField] private float _dragonHeadSpeed = 2f;
        [Min(0f)] [SerializeField] private float _dragonInitialHeadDistance = 0f;

        [Header("Dragon spawn")] [SerializeField]
        private bool _spawnProgressively = true;

        [Header("Dragon scales colors")] [SerializeField]
        private GameContext.DragonScaleColorSlot[] _dragonScaleColors;

        [Header("Blocks (source of turns/colors)")] [SerializeField]
        private DragonColorBlock[] _blocks;

        [Header("Field borders")] [Min(0f)] [SerializeField]
        private float _fieldBordersOffset = 0f;

        [Header("Reel slots (UI)")] [SerializeField]
        private SlotView[] _reelSlots;

        [SerializeField] private SpoolSpriteConfig _spoolSpriteConfig;
        [SerializeField] private RopeColorOffsetConfig _ropeColorOffsetConfig;
        [Min(0.01f)] [SerializeField] private float _windSecondsPerScale = 0.35f;
        [Min(0.01f)] [SerializeField] private float _dragonRebukeDuration = 0.08f;

        [Header("Blocks movement")] [Min(0f)] [SerializeField]
        private float _blockMoveSpeed = 8f;

        public bool IsPaused { get; set; }
        private bool IsActive { get; set; }
        private EcsWorld _worldGame;
        private EcsWorld _worldPhysics;
        private IEcsSystems _gameplaySystems;
        private IEcsSystems _collisionSystems;

        private readonly List<Type> _gameplaySystemsTypes = new List<Type>();
        private readonly List<Type> _collisionSystemsTypes = new List<Type>();
        private readonly HashSet<Type> _boundSystemTypes = new HashSet<Type>();

        private GameObject _bootOverlay;
        private Playeble.Scripts.Gameplay.Dragon.GameContext _gameContext;

        private void Awake()
        {
            // MonoBehaviour constructors are not a reliable initialization place (especially for Web/Luna pipelines).
            BindSystems();
        }

        private void Start()
        {
            Debug.Log("GameBootstrap.Start()");
            InitEcsGame();
            Debug.Log("GameBootstrap.InitEcsGame() completed");
            ApplyRopeColorOffsetConfig();

            if (_showBootOverlay)
            {
                ShowBootOverlay("Loaded");
            }
        }

        private void ApplyRopeColorOffsetConfig()
        {
            if (_ropeColorOffsetConfig == null || _reelSlots == null)
            {
                return;
            }

            for (var i = 0; i < _reelSlots.Length; i++)
            {
                var slot = _reelSlots[i];
                if (slot == null || slot.Rope == null)
                {
                    continue;
                }

                slot.Rope.SetColorOffsetConfig(_ropeColorOffsetConfig);
            }
        }

        private void BindSystems()
        {
            BindSystem<Playeble.Scripts.Gameplay.Dragon.DragonSpawnInitSystem>(BindType.Game);
            BindSystem<ComputeGameBordersInitSystem>(BindType.Game);
            BindSystem<BlocksInitSystem>(BindType.Game);
            BindSystem<Playeble.Scripts.Gameplay.Dragon.DragonMoveAlongPathSystem>(BindType.Game);
            BindSystem<Playeble.Scripts.Gameplay.Dragon.DragonGrowSpawnSystem>(BindType.Game);
            BindSystem<Playeble.Scripts.Gameplay.Dragon.DragonEventCleanupSystem>(BindType.Game);
            BindSystem<ReelSlotsInitSystem>(BindType.Game);
            BindSystem<StartBlockMoveOnClickSystem>(BindType.Game);
            BindSystem<BlockPathMoveSystem>(BindType.Game);
            BindSystem<AssignReelToFirstEmptySlotSystem>(BindType.Game);
            BindSystem<ReelWindSystem>(BindType.Game);
            BindSystem<ReelSlotSyncViewSystem>(BindType.Game);
            BindSystem<BlockClickEventCleanupSystem>(BindType.Game);
        }

        private void BindSystem<T>(BindType bindType)
        {
            var type = typeof(T);

            if (bindType == BindType.Game)
            {
                _gameplaySystemsTypes.Add(type);
            }
            else
            {
                _collisionSystemsTypes.Add(type);
            }

            // Bind only once per type (allows adding same system multiple times to pipeline)
            if (!_boundSystemTypes.Contains(type))
            {
                _boundSystemTypes.Add(type);
            }
        }

        private enum BindType
        {
            Game,
            Collision
        }

        private EcsSystems CreateEcsSystems(EcsWorld world, List<Type> systemsTypes)
        {
            var ecsSystems = new EcsSystems(world);
            for (var i = 0; i < systemsTypes.Count; i++)
            {
                var type = systemsTypes[i];
                if (!typeof(IEcsSystem).IsAssignableFrom(type))
                {
                    Debug.LogError($"ECS system type does not implement IEcsSystem: {type.FullName}");
                    continue;
                }

                IEcsSystem instance;
                instance = TryCreateSystem(type) ?? (IEcsSystem)Activator.CreateInstance(type);

                ecsSystems.Add(instance);
            }

            return ecsSystems;
        }

        private IEcsSystem TryCreateSystem(Type type)
        {
            if (_gameContext == null)
            {
                return null;
            }

            return (IEcsSystem)Activator.CreateInstance(type, _gameContext);
        }

        public void InitEcsGame()
        {
            //_gamePlaySubContainer.BindInstance(someBinds);
            SetupLevel();

            var scaleColors = _dragonScaleColors;
            if (_blocks != null && _blocks.Length > 0)
            {
                var totalTurns = 0;
                var slots = new List<GameContext.DragonScaleColorSlot>(_blocks.Length);
                for (var i = 0; i < _blocks.Length; i++)
                {
                    if (_blocks[i] == null)
                    {
                        continue;
                    }

                    var turns = Mathf.Max(0, _blocks[i].Turns);
                    totalTurns += turns;
                    _blocks[i].ApplyVisual();

                    if (turns > 0)
                    {
                        slots.Add(new GameContext.DragonScaleColorSlot
                        {
                            Type = _blocks[i].Type,
                            Count = turns,
                            ColorOffset = _blocks[i].ColorOffset
                        });
                    }
                }

                _dragonBodySegmentsCount = Mathf.Max(0, totalTurns);
                scaleColors = slots.ToArray();
            }

            if (_spawnProgressively)
            {
                if (_dragonInitialHeadDistance < 0f)
                {
                    _dragonInitialHeadDistance = 0f;
                }
            }
            else if (_dragonInitialHeadDistance <= 0f)
            {
                _dragonInitialHeadDistance = (_dragonBodySegmentsCount + 1) * _dragonSegmentSpacing;
            }

            _gameContext = new Playeble.Scripts.Gameplay.Dragon.GameContext(
                _dragonPath,
                _dragonRoot,
                _dragonHeadPrefab,
                _dragonBodyPrefab,
                _dragonTailPrefab,
                _dragonBodySegmentsCount,
                _dragonSegmentSpacing,
                _dragonHeadSpeed,
                _dragonInitialHeadDistance,
                scaleColors,
                _blocks,
                _fieldBordersOffset,
                _reelSlots,
                _spoolSpriteConfig,
                _windSecondsPerScale,
                _dragonRebukeDuration,
                _blockMoveSpeed);

            var configGame = new EcsWorld.Config
            {
                Entities = 4096,
                RecycledEntities = 4096,
                Pools = 4096,
                Filters = 4096,
                PoolDenseSize = 4096,
                PoolRecycledSize = 4096,
                EntityComponentsSize = 8,
            };

            var configPhysics = new EcsWorld.Config
            {
                Entities = 16328,
                RecycledEntities = 16328,
                Pools = 128,
                Filters = 128,
                PoolDenseSize = 128,
                PoolRecycledSize = 128,
                EntityComponentsSize = 4,
            };

            _worldGame = new EcsWorld(configGame);
            _worldPhysics = new EcsWorld(configPhysics);

            _gameplaySystems = CreateEcsSystems(_worldGame, _gameplaySystemsTypes);
            _collisionSystems = CreateEcsSystems(_worldPhysics, _collisionSystemsTypes);

            _gameplaySystems.Init();
            _collisionSystems.Init();
            IsActive = true;
            IsPaused = false;
        }

        /// <summary>
        /// Получает ECS мир для доступа к данным боя
        /// </summary>
        public EcsWorld GetWorld()
        {
            return _worldGame;
        }

        private void SetupLevel()
        {
        }


        public void Update()
        {
            if (!IsPaused)
            {
                if (IsActive)
                {
                    _gameplaySystems?.Run();
                    _collisionSystems?.Run();
                }
            }
        }

        private void ShowBootOverlay(string message)
        {
            if (_bootOverlay != null)
            {
                return;
            }

            _bootOverlay = new GameObject("BootOverlay");
            DontDestroyOnLoad(_bootOverlay);

            var canvas = _bootOverlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _bootOverlay.AddComponent<CanvasScaler>();
            _bootOverlay.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("BootOverlayText");
            textGo.transform.SetParent(_bootOverlay.transform, false);

            var text = textGo.AddComponent<Text>();
            text.text = message;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 48;

            var rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            _worldGame?.Destroy();
            _worldPhysics?.Destroy();
            _gameplaySystems?.Destroy();
            _collisionSystems?.Destroy();

            if (_bootOverlay != null)
            {
                Destroy(_bootOverlay);
                _bootOverlay = null;
            }

            IsActive = false;
        }
    }
}