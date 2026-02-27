using Leopotam.EcsLite;
using Playeble.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Playeble.Scripts.Gameplay.Selection
{
    public sealed class ClickRaycastToEcsEvent : MonoBehaviour
    {
        [SerializeField] private GameBootstrap _bootstrap;
        [SerializeField] private Camera _camera;
        [Min(0f)]
        [SerializeField] private float _maxDistance = 200f;
        [SerializeField] private bool _ignoreUI = true;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_bootstrap == null)
            {
                _bootstrap = FindObjectOfType<GameBootstrap>();
            }
        }

        private void Update()
        {
            if (_bootstrap == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            if (!TryGetPointerDown(out var pointerPos, out var pointerId))
            {
                return;
            }

            var ray = _camera.ScreenPointToRay(pointerPos);
            if (!Physics.Raycast(ray, out var hit, _maxDistance))
            {
                return;
            }

            var block = hit.collider != null ? hit.collider.GetComponentInParent<ClickableBlockView>() : null;
            if (block == null)
            {
                return;
            }

            Debug.Log($"[ClickRaycastToEcsEvent] Block clicked: {block.name} @ {hit.point}");

            var world = _bootstrap.GetWorld();
            if (world == null || !world.IsAlive())
            {
                return;
            }

            var e = world.NewEntity();
            var pool = world.GetPool<BlockClickedEvent>();
            pool.Add(e);
            var dense = pool.GetRawDenseItems();
            var sparse = pool.GetRawSparseItems();
            if (e >= 0 && e < sparse.Length)
            {
                var idx = sparse[e];
                if (idx > 0)
                {
                    var evt = dense[idx];
                    evt.Block = block;
                    evt.HitPoint = hit.point;
                    evt.HitNormal = hit.normal;
                    dense[idx] = evt;
                }
            }
        }

        private static bool TryGetPointerDown(out Vector2 pointerPos, out int pointerId)
        {
            // Touch has priority (mobile).
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    pointerPos = touch.position;
                    pointerId = touch.fingerId;
                    return true;
                }
            }

            // Mouse (editor / desktop).
            if (Input.GetMouseButtonDown(0))
            {
                pointerPos = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            pointerPos = Vector2.zero;
            pointerId = -1;
            return false;
        }
    }
}

