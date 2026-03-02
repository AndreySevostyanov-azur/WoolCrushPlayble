using Cinemachine;
using Leopotam.EcsLite;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Dragon
{
    /// <summary>
    /// Updates dragon head speed using VariableAccelerationSettings.
    /// Must run before DragonMoveAlongPathSystem.
    /// </summary>
    public sealed class DragonVariableAccelerationSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;
        private bool _loggedMissingSettings;
        private bool _loggedFirstTick;

        public DragonVariableAccelerationSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public DragonVariableAccelerationSystem()
        {
        }

        private EcsWorld _world;
        private EcsFilter _headFilter;
        private EcsPool<DragonHeadMove> _headMovePool;
        private EcsPool<SplinePathRef> _pathPool;
        private EcsPool<DragonPart> _partPool;

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _headMovePool = _world.GetPool<DragonHeadMove>();
            _pathPool = _world.GetPool<SplinePathRef>();
            _partPool = _world.GetPool<DragonPart>();

            _headFilter = _world
                .Filter<DragonHeadMove>()
                .Inc<SplinePathRef>()
                .Inc<DragonPart>()
                .End();
        }

        public void Run(IEcsSystems systems)
        {
            var settings = _ctx != null ? _ctx.DragonVariableAccelerationSettings : null;
            if (settings == null)
            {
                if (!_loggedMissingSettings)
                {
                    _loggedMissingSettings = true;
                    Debug.LogWarning($"{nameof(DragonVariableAccelerationSystem)}: settings asset is not assigned in GameBootstrap. Speed will remain constant.");
                }
                return;
            }

            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            var entities = _headFilter.GetRawEntities();
            var count = _headFilter.GetEntitiesCount();

            var headDense = _headMovePool.GetRawDenseItems();
            var headSparse = _headMovePool.GetRawSparseItems();
            var pathDense = _pathPool.GetRawDenseItems();
            var pathSparse = _pathPool.GetRawSparseItems();
            var partDense = _partPool.GetRawDenseItems();
            var partSparse = _partPool.GetRawSparseItems();

            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= headSparse.Length || e >= pathSparse.Length || e >= partSparse.Length)
                {
                    continue;
                }

                var headIdx = headSparse[e];
                var pathIdx = pathSparse[e];
                var partIdx = partSparse[e];
                if (headIdx <= 0 || pathIdx <= 0 || partIdx <= 0)
                {
                    continue;
                }

                var part = partDense[partIdx];
                if (part.Kind != DragonPartKind.Head)
                {
                    continue;
                }

                var head = headDense[headIdx];
                var pathRef = pathDense[pathIdx];
                var path = pathRef.Path;
                if (path == null)
                {
                    continue;
                }

                var length = GetPathLengthSafe(path);
                var baseSpeed = head.BaseSpeed;
                if (baseSpeed <= 0f && _ctx != null && _ctx.DragonHeadSpeed > 0f)
                {
                    baseSpeed = _ctx.DragonHeadSpeed;
                    head.BaseSpeed = baseSpeed;
                }

                head.Speed = settings.EvaluateSpeed(baseSpeed, head.Distance, length);

                if (!_loggedFirstTick)
                {
                    _loggedFirstTick = true;
                    Debug.Log(
                        $"{nameof(DragonVariableAccelerationSystem)} active(step): dist={head.Distance:0.###} len={length:0.###} base={baseSpeed:0.###} speed={head.Speed:0.###} " +
                        $"accelUntil={settings.AccelerationUntilDistance:0.###} accelMul={settings.AccelerationMultiplier:0.###} " +
                        $"decelRemain={settings.DecelerationRemainingDistance:0.###} decelMul={settings.DecelerationMultiplier:0.###}");
                }

                headDense[headIdx] = head;
            }
        }

        private static float GetPathLengthSafe(CinemachinePath path)
        {
            if (path == null)
            {
                return 0f;
            }

            var length = path.PathLength;
            if (length < 0f)
            {
                length = 0f;
            }

            return length;
        }
    }
}

