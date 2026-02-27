using AzurGames.Wool.Gameplay;
using Leopotam.EcsLite;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.ReelSlots
{
    public sealed class ReelSlotSyncViewSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly GameContext _ctx;

        // Keeping ctor compatible with GameBootstrap injection.
        public ReelSlotSyncViewSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public ReelSlotSyncViewSystem()
        {
        }

        private EcsWorld _world;
        private EcsFilter _slots;
        private EcsFilter _dragonParts;
        private EcsPool<ReelSlotViewRef> _viewPool;
        private EcsPool<ReelSlotState> _statePool;
        private EcsPool<ReelSpoolData> _spoolPool;
        private EcsPool<ReelSpoolColor> _spoolColorPool;
        private EcsPool<ReelWindState> _windPool;
        private EcsPool<DragonScaleMarkerRef> _markerPool;
        private EcsPool<DragonPart> _dragonPartPool;
        private EcsPool<TransformRef> _transformPool;

        public void Init(IEcsSystems systems)
        {
            _world = systems.GetWorld();
            _viewPool = _world.GetPool<ReelSlotViewRef>();
            _statePool = _world.GetPool<ReelSlotState>();
            _spoolPool = _world.GetPool<ReelSpoolData>();
            _spoolColorPool = _world.GetPool<ReelSpoolColor>();
            _windPool = _world.GetPool<ReelWindState>();
            _markerPool = _world.GetPool<DragonScaleMarkerRef>();
            _dragonPartPool = _world.GetPool<DragonPart>();
            _transformPool = _world.GetPool<TransformRef>();

            _slots = _world.Filter<ReelSlotViewRef>().Inc<ReelSlotState>().End();
            _dragonParts = _world.Filter<DragonPart>().Inc<TransformRef>().End();
        }

        public void Run(IEcsSystems systems)
        {
            var headPosResult = GetDragonHeadPosition();

            var slotEntities = _slots.GetRawEntities();
            var slotCount = _slots.GetEntitiesCount();
            var viewDense = _viewPool.GetRawDenseItems();
            var viewSparse = _viewPool.GetRawSparseItems();
            var stateDense = _statePool.GetRawDenseItems();
            var stateSparse = _statePool.GetRawSparseItems();
            var spoolDense = _spoolPool.GetRawDenseItems();
            var spoolSparse = _spoolPool.GetRawSparseItems();
            var spoolColorDense = _spoolColorPool.GetRawDenseItems();
            var spoolColorSparse = _spoolColorPool.GetRawSparseItems();
            var windDense = _windPool.GetRawDenseItems();
            var windSparse = _windPool.GetRawSparseItems();
            var markerDense = _markerPool.GetRawDenseItems();
            var markerSparse = _markerPool.GetRawSparseItems();

            for (var i = 0; i < slotCount; i++)
            {
                var e = slotEntities[i];
                if (e < 0
                    || e >= viewSparse.Length
                    || e >= stateSparse.Length
                    || e >= spoolSparse.Length
                    || e >= spoolColorSparse.Length
                    || e >= windSparse.Length)
                {
                    continue;
                }

                var viewIdx = viewSparse[e];
                if (viewIdx <= 0)
                {
                    continue;
                }

                var viewRef = viewDense[viewIdx];
                var view = viewRef.View;
                if (view == null)
                {
                    continue;
                }

                var stateIdx = stateSparse[e];
                if (stateIdx <= 0)
                {
                    continue;
                }

                var status = stateDense[stateIdx].Status;
                view.Refresh(status);

                var spoolIdx = spoolSparse[e];
                if (status != SlotStates.Occupied || spoolIdx <= 0)
                {
                    view.ShowSpool(false);
                    view.RemainReelText.Clear();
                    if (view.Rope != null)
                    {
                        view.Rope.Show(false);
                    }
                    continue;
                }

                var spool = spoolDense[spoolIdx];
                var remaining = Mathf.Max(0, spool.Max - spool.Current);
                view.RemainReelText.Set(remaining);
                view.ShowSpool(true);

                var selectedVisual = UpdateVisualSelection(view, spool.BoxType);
                if (selectedVisual == null)
                {
                    if (view.Rope != null)
                    {
                        view.Rope.Show(false);
                    }
                    continue;
                }

                // Apply sprite configuration by color.
                var colorIdx = spoolColorSparse[e];
                SpoolSpriteData spriteData = null;
                Colors colorType = default(Colors);
                if (colorIdx > 0)
                {
                    var spoolColor = spoolColorDense[colorIdx];
                    colorType = spoolColor.Color;
                    if (_ctx != null && _ctx.SpoolSpriteConfig != null)
                    {
                        spriteData = _ctx.SpoolSpriteConfig.TryGetData(spoolColor.Color);
                    }
                }

                if (spriteData != null)
                {
                    if (selectedVisual.SpoolImage != null && spriteData.Icon != null && selectedVisual.SpoolImage.sprite != spriteData.Icon)
                    {
                        selectedVisual.SpoolImage.sprite = spriteData.Icon;
                    }
                }

                var progress = Mathf.Clamp(spool.Current, 0, selectedVisual.CoilsImage != null ? selectedVisual.CoilsImage.Length : 0);
                var images = selectedVisual.CoilsImage;
                if (images != null)
                {
                    for (var j = 0; j < images.Length; j++)
                    {
                        var img = images[j];
                        if (img == null)
                        {
                            continue;
                        }

                        if (spriteData != null && spriteData.CoilSprite != null && img.sprite != spriteData.CoilSprite)
                        {
                            img.sprite = spriteData.CoilSprite;
                        }

                        var shouldBeActive = j < progress;
                        if (img.gameObject.activeSelf != shouldBeActive)
                        {
                            img.gameObject.SetActive(shouldBeActive);
                        }
                    }
                }

                // Rope: from current coil position to dragon head.
                if (view.Rope != null)
                {
                    var windIdx = windSparse[e];
                    var hasWindTarget = false;
                    var windTargetPos = default(Vector3);
                    if (windIdx > 0)
                    {
                        var wind = windDense[windIdx];
                        var targetEntity = wind.TargetEntity;
                        if (targetEntity >= 0 && targetEntity < markerSparse.Length)
                        {
                            var markerIdx = markerSparse[targetEntity];
                            if (markerIdx > 0)
                            {
                                var marker = markerDense[markerIdx].Value;
                                if (marker != null)
                                {
                                    hasWindTarget = true;
                                    windTargetPos = marker.transform.position;
                                }
                            }
                        }
                    }

                    // Rope is visible only while slot actively unwinds a target scale.
                    view.Rope.Show(hasWindTarget);
                    if (hasWindTarget)
                    {
                        view.Rope.SetColor(colorType);
                        var coilIndex = spool.Current;
                        if (images != null && images.Length > 0)
                        {
                            coilIndex = Mathf.Clamp(coilIndex, 0, images.Length - 1);
                        }
                        else
                        {
                            coilIndex = 0;
                        }
                        var startPos = selectedVisual.GetCoilPosition(coilIndex);
                        view.Rope.UpdateRope(startPos, windTargetPos);
                    }
                }
            }
        }

        private static SpoolVisualSet UpdateVisualSelection(SlotView slotView, BoxType boxType)
        {
            SpoolVisualSet selected = null;
            if (slotView.Visuals == null)
            {
                return null;
            }

            for (var i = 0; i < slotView.Visuals.Length; i++)
            {
                var v = slotView.Visuals[i];
                if (v == null || v.Root == null)
                {
                    continue;
                }

                var isSelected = v.Size == boxType;
                if (v.Root.activeSelf != isSelected)
                {
                    v.Root.SetActive(isSelected);
                }

                if (isSelected)
                {
                    selected = v;
                }
            }

            return selected;
        }

        private struct HeadPosResult
        {
            public bool Found;
            public Vector3 Position;
        }

        private HeadPosResult GetDragonHeadPosition()
        {
            var result = new HeadPosResult { Found = false, Position = default(Vector3) };

            if (_dragonParts == null || _dragonParts.GetEntitiesCount() <= 0)
            {
                return result;
            }

            var entities = _dragonParts.GetRawEntities();
            var count = _dragonParts.GetEntitiesCount();
            var partDense = _dragonPartPool.GetRawDenseItems();
            var partSparse = _dragonPartPool.GetRawSparseItems();
            var trDense = _transformPool.GetRawDenseItems();
            var trSparse = _transformPool.GetRawSparseItems();

            for (var i = 0; i < count; i++)
            {
                var e = entities[i];
                if (e < 0 || e >= partSparse.Length || e >= trSparse.Length)
                {
                    continue;
                }

                var partIdx = partSparse[e];
                var trIdx = trSparse[e];
                if (partIdx <= 0 || trIdx <= 0)
                {
                    continue;
                }

                var part = partDense[partIdx];
                if (part.Kind != DragonPartKind.Head)
                {
                    continue;
                }

                var tr = trDense[trIdx].Value;
                if (tr == null)
                {
                    continue;
                }

                result.Found = true;
                result.Position = tr.position;
                return result;
            }

            return result;
        }
    }
}

