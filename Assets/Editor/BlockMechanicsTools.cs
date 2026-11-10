using System.Collections.Generic;
using Playeble.Scripts.Gameplay.Dragon;
using Playeble.Scripts.Gameplay.Mechanics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Playeble.EditorTools
{
    public static class BlockMechanicsTools
    {
        private const string MenuRoot = "Tools/Blocks/Mechanics/";

        [MenuItem(MenuRoot + "Add Hidden To Selected")]
        public static void AddHidden()
        {
            ForSelectedBlocks(AddHidden);
        }

        [MenuItem(MenuRoot + "Add Ice To Selected")]
        public static void AddIce()
        {
            ForSelectedBlocks(AddIce);
        }

        [MenuItem(MenuRoot + "Add Chain To Selected")]
        public static void AddChain()
        {
            ForSelectedBlocks(AddChain);
        }

        [MenuItem(MenuRoot + "Rebuild Views (Scene)")]
        public static void RebuildViews()
        {
            var config = FindConfig();
            if (config == null)
            {
                return;
            }

            var blocks = Object.FindObjectsOfType<DragonColorBlock>(true);
            for (var i = 0; i < blocks.Length; i++)
            {
                var hidden = blocks[i].GetComponent<HiddenBlock>();
                if (hidden != null)
                {
                    hidden.EditorSetup(config, BlockMechanicViews.EditorEnsureOverlay(blocks[i], BlockMechanicViews.HiddenName, true, config));
                    EditorUtility.SetDirty(hidden);
                }

                var frozen = blocks[i].GetComponent<FrozenBlock>();
                if (frozen != null)
                {
                    frozen.EditorSetup(config, BlockMechanicViews.EditorEnsureOverlay(blocks[i], BlockMechanicViews.IceName, false, config));
                    EditorUtility.SetDirty(frozen);
                }
            }

            MarkSceneDirty();
        }

        [MenuItem(MenuRoot + "Validate (Scene)")]
        public static void Validate()
        {
            var blocks = Object.FindObjectsOfType<DragonColorBlock>(true);
            var errors = 0;
            var inScene = new HashSet<DragonColorBlock>(blocks);
            var chainEdges = new Dictionary<DragonColorBlock, List<DragonColorBlock>>();

            for (var i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                if (block.GetComponent<FrozenBlock>() != null && !HasStriker(block, blocks))
                {
                    Debug.LogError($"Frozen '{block.name}': no block lists it in BlockingBlocks, nothing can hit it. Re-bake blocking blocks or move it.", block);
                    errors++;
                }

                var chained = block.GetComponent<ChainedBlock>();
                if (chained == null)
                {
                    continue;
                }

                var edges = new List<DragonColorBlock>();
                var seen = new HashSet<DragonColorBlock>();
                var blockers = chained.Blockers;
                if (blockers.Count == 0)
                {
                    Debug.LogError($"Chained '{block.name}': blockers list is empty.", block);
                    errors++;
                }

                for (var j = 0; j < blockers.Count; j++)
                {
                    var blocker = blockers[j];
                    if (blocker == null || blocker == block || !inScene.Contains(blocker) || !seen.Add(blocker))
                    {
                        Debug.LogError($"Chained '{block.name}': blockers[{j}] is empty, itself, duplicated or not in the scene.", block);
                        errors++;
                        continue;
                    }

                    edges.Add(blocker);
                }

                chainEdges[block] = edges;
            }

            errors += CountChainCycles(chainEdges);
            Debug.Log($"Block mechanics validation finished: {errors} error(s), {blocks.Length} block(s) checked.");
        }

        private static void AddHidden(DragonColorBlock block, BlockMechanicsConfig config)
        {
            var component = block.GetComponent<HiddenBlock>() ?? Undo.AddComponent<HiddenBlock>(block.gameObject);
            Undo.RecordObject(component, "Setup Hidden");
            component.EditorSetup(config, BlockMechanicViews.EditorEnsureOverlay(block, BlockMechanicViews.HiddenName, true, config));
            EditorUtility.SetDirty(component);
        }

        private static void AddIce(DragonColorBlock block, BlockMechanicsConfig config)
        {
            var component = block.GetComponent<FrozenBlock>() ?? Undo.AddComponent<FrozenBlock>(block.gameObject);
            Undo.RecordObject(component, "Setup Ice");
            component.EditorSetup(config, BlockMechanicViews.EditorEnsureOverlay(block, BlockMechanicViews.IceName, false, config));
            EditorUtility.SetDirty(component);
        }

        private static void AddChain(DragonColorBlock block, BlockMechanicsConfig config)
        {
            var component = block.GetComponent<ChainedBlock>() ?? Undo.AddComponent<ChainedBlock>(block.gameObject);
            Undo.RecordObject(component, "Setup Chain");
            component.EditorSetConfig(config);
            EditorUtility.SetDirty(component);
        }

        private static bool HasStriker(DragonColorBlock frozen, DragonColorBlock[] blocks)
        {
            for (var i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] == frozen || blocks[i].BlockingBlocks == null)
                {
                    continue;
                }

                if (System.Array.IndexOf(blocks[i].BlockingBlocks, frozen) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountChainCycles(Dictionary<DragonColorBlock, List<DragonColorBlock>> edges)
        {
            var errors = 0;
            var state = new Dictionary<DragonColorBlock, int>();
            foreach (var pair in edges)
            {
                if (HasCycle(pair.Key, edges, state))
                {
                    Debug.LogError($"Chain deadlock: '{pair.Key.name}' is part of a chain cycle, none of its blockers can ever leave.", pair.Key);
                    errors++;
                }
            }

            return errors;
        }

        private static bool HasCycle(DragonColorBlock node, Dictionary<DragonColorBlock, List<DragonColorBlock>> edges, Dictionary<DragonColorBlock, int> state)
        {
            int current;
            if (state.TryGetValue(node, out current))
            {
                return current == 1;
            }

            state[node] = 1;
            List<DragonColorBlock> next;
            if (edges.TryGetValue(node, out next))
            {
                for (var i = 0; i < next.Count; i++)
                {
                    if (HasCycle(next[i], edges, state))
                    {
                        return true;
                    }
                }
            }

            state[node] = 2;
            return false;
        }

        private static void ForSelectedBlocks(System.Action<DragonColorBlock, BlockMechanicsConfig> action)
        {
            var config = FindConfig();
            if (config == null)
            {
                return;
            }

            var selected = Selection.gameObjects;
            for (var i = 0; i < selected.Length; i++)
            {
                var block = selected[i].GetComponent<DragonColorBlock>();
                if (block != null)
                {
                    action(block, config);
                }
            }

            MarkSceneDirty();
        }

        private static BlockMechanicsConfig FindConfig()
        {
            return BlockMechanicViews.EditorFindConfig();
        }

        private static void MarkSceneDirty()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
