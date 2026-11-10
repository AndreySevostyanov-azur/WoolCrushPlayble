using System.Collections.Generic;
using Playeble.Scripts.Gameplay.Dragon;
using UnityEngine;

namespace Playeble.Scripts.Gameplay.Mechanics
{
    public static class BlockMechanicViews
    {
        public const string HiddenName = "Hidden";
        public const string IceName = "Ice";

        public static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        public static readonly int RainbowAlphaId = Shader.PropertyToID("_RainbowAlpha");

        public static MeshRenderer CreateHiddenOverlay(DragonColorBlock block, BlockMechanicsConfig config)
        {
            var filter = FindMeshFilter(block);
            if (filter == null || filter.sharedMesh == null || config == null || config.HiddenMaterial == null)
            {
                return null;
            }

            var overlay = CreateOverlay(filter.transform, HiddenName, filter.sharedMesh, config.HiddenMaterial);
            overlay.transform.localScale = config.HiddenOverlayScale;
            return overlay;
        }

        public static MeshRenderer CreateIceOverlay(DragonColorBlock block, BlockMechanicsConfig config)
        {
            var filter = FindMeshFilter(block);
            if (filter == null || config == null || config.IceMaterial == null)
            {
                return null;
            }

            var iceMesh = config.FindIceMesh(filter.sharedMesh);
            if (iceMesh == null)
            {
                return null;
            }

            var overlay = CreateOverlay(filter.transform, IceName, iceMesh, config.IceMaterial);
            overlay.transform.localRotation = Quaternion.Euler(config.IceLocalEuler);
            return overlay;
        }

        public static bool IsOverlayOf(DragonColorBlock block, Renderer renderer)
        {
            return renderer != null
                   && renderer.transform != block.transform
                   && renderer.transform.IsChildOf(block.transform);
        }

#if UNITY_EDITOR
        public static BlockMechanicsConfig EditorFindConfig()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:BlockMechanicsConfig");
            if (guids.Length == 0)
            {
                Debug.LogError("BlockMechanicsConfig asset not found. Create one: Assets > Create > Playable > Block Mechanics Config.");
                return null;
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath<BlockMechanicsConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static MeshRenderer EditorEnsureOverlay(DragonColorBlock block, string name, bool hidden, BlockMechanicsConfig config)
        {
            var renderers = block.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name == name && IsOverlayOf(block, renderers[i]))
                {
                    return renderers[i];
                }
            }

            var created = hidden ? CreateHiddenOverlay(block, config) : CreateIceOverlay(block, config);
            if (created == null)
            {
                Debug.LogWarning($"Could not build the '{name}' view for '{block.name}': check the config (material, ice mesh for this tile mesh).", block);
                return null;
            }

            UnityEditor.Undo.RegisterCreatedObjectUndo(created.gameObject, "Create " + name);
            return created;
        }
#endif

        public static Vector3 AnchorOf(Transform block, BlockMechanicsConfig config)
        {
            var offset = config != null ? config.AnchorLocalOffset : new Vector3(0f, 0.9f, 0f);
            return block.TransformPoint(offset);
        }

        public static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Material material, float scale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale, scale, scale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static void SpawnVfx(GameObject prefab, Vector3 position, float lifetime)
        {
            if (prefab == null)
            {
                return;
            }

            var instance = Object.Instantiate(prefab, position, Quaternion.identity);
            if (lifetime > 0f)
            {
                Object.Destroy(instance, lifetime);
            }
        }

        private static MeshFilter FindMeshFilter(DragonColorBlock block)
        {
            if (block == null)
            {
                return null;
            }

            var filters = block.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var name = filters[i].gameObject.name;
                if (name != HiddenName && name != IceName)
                {
                    return filters[i];
                }
            }

            return null;
        }

        private static MeshRenderer CreateOverlay(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }

    public sealed class ChainVisual
    {
        private readonly List<SpriteRenderer> _links = new List<SpriteRenderer>(16);
        private readonly BlockMechanicsConfig _config;
        private readonly Transform _root;
        private readonly float _linkLength;

        public ChainVisual(BlockMechanicsConfig config, Transform root)
        {
            _config = config;
            _root = root;
            var sprite = config != null ? config.ChainSprite : null;
            var width = sprite != null ? sprite.bounds.size.x : 1f;
            _linkLength = Mathf.Max(0.01f, width * config.ChainScale * config.ChainLinkSpacing);
        }

        public void Update(Vector3 from, Vector3 to, Camera camera)
        {
            if (_config == null || _config.ChainSprite == null || camera == null)
            {
                return;
            }

            var distance = Vector3.Distance(from, to);
            var count = Mathf.Max(1, Mathf.RoundToInt(distance / _linkLength));
            while (_links.Count < count)
            {
                _links.Add(BlockMechanicViews.CreateSprite(
                    _root, "ChainLink", _config.ChainSprite, _config.SpriteMaterial, _config.ChainScale, _config.SortingOrder));
            }

            var screenFrom = camera.WorldToScreenPoint(from);
            var screenTo = camera.WorldToScreenPoint(to);
            var angle = Mathf.Atan2(screenTo.y - screenFrom.y, screenTo.x - screenFrom.x) * Mathf.Rad2Deg + _config.ChainAngleOffset;
            var rotation = camera.transform.rotation * Quaternion.Euler(0f, 0f, angle);

            for (var i = 0; i < _links.Count; i++)
            {
                var link = _links[i];
                var active = i < count;
                if (link.gameObject.activeSelf != active)
                {
                    link.gameObject.SetActive(active);
                }

                if (!active)
                {
                    continue;
                }

                var t = (i + 0.5f) / count;
                link.transform.SetPositionAndRotation(Vector3.Lerp(from, to, t), rotation);
            }
        }

        public void Destroy()
        {
            for (var i = 0; i < _links.Count; i++)
            {
                if (_links[i] != null)
                {
                    Object.Destroy(_links[i].gameObject);
                }
            }

            _links.Clear();
        }
    }
}
