using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Cleans up inactive GameObjects and unused SkinnedMeshRenderers
    /// </summary>
    public class ObjectCleanup
    {
        /// <summary>
        /// Removes inactive GameObjects and unused renderers from duplicate avatar
        /// </summary>
        public void Cleanup(
            GameObject duplicate,
            AvatarSnapshot snapshot,
            bool aggressiveBonePruning,
            PhysBoneAnalyzer.PhysBoneAnalysisResult physBoneAnalysis)
        {
            if (duplicate == null || snapshot == null)
            {
                Debug.LogError("[AvatarOptimizer] ObjectCleanup: Invalid parameters");
                return;
            }

            // Remove inactive GameObjects
            RemoveInactiveGameObjects(duplicate, snapshot);

            // Remove unused SkinnedMeshRenderers
            RemoveUnusedRenderers(duplicate, snapshot);

            // Bone pruning (only if aggressive mode enabled)
            if (aggressiveBonePruning && physBoneAnalysis != null)
            {
                PruneBones(duplicate, snapshot, physBoneAnalysis);
            }
        }

        private void RemoveInactiveGameObjects(GameObject root, AvatarSnapshot snapshot)
        {
            var allObjects = root.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject)
                .ToList();

            int removedCount = 0;

            foreach (var obj in allObjects)
            {
                if (obj == null || obj == root) continue;

                string path = GetGameObjectPath(root.transform, obj.transform);

                // Remove if not in snapshot's active paths
                if (!snapshot.ActiveGameObjectPaths.Contains(path))
                {
                    if (obj.transform.parent != null)
                    {
                        Undo.DestroyObjectImmediate(obj);
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[AvatarOptimizer] Removed {removedCount} inactive GameObjects");
            }
        }

        private void RemoveUnusedRenderers(GameObject root, AvatarSnapshot snapshot)
        {
            var allRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int removedCount = 0;

            foreach (var renderer in allRenderers)
            {
                if (renderer == null) continue;

                string path = GetGameObjectPath(root.transform, renderer.transform);

                // Remove if not in snapshot's active renderer paths
                if (!snapshot.ActiveRendererPaths.Contains(path))
                {
                    Undo.DestroyObjectImmediate(renderer);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[AvatarOptimizer] Removed {removedCount} unused SkinnedMeshRenderers");
            }
        }

        private void PruneBones(
            GameObject root,
            AvatarSnapshot snapshot,
            PhysBoneAnalyzer.PhysBoneAnalysisResult physBoneAnalysis)
        {
            // Bone pruning is HIGH RISK
            // Only prune bones that are:
            // 1. Not used by any active mesh
            // 2. Not used by any PhysBone
            // 3. Not in the humanoid rig
            // 4. Not referenced by any animation

            if (physBoneAnalysis == null || physBoneAnalysis.PrunableBones == null)
            {
                return;
            }

            // Build set of protected humanoid bones
            var protectedBones = GetHumanoidBones(root);

            int prunedCount = 0;
            int skippedHumanoidCount = 0;

            foreach (var bone in physBoneAnalysis.PrunableBones)
            {
                if (bone == null) continue;

                // CRITICAL: Never prune humanoid rig bones
                if (protectedBones.Contains(bone))
                {
                    skippedHumanoidCount++;
                    continue;
                }

                string bonePath = GetGameObjectPath(root.transform, bone);

                // Double-check: bone should not be in used bones
                if (!snapshot.UsedBonePaths.Contains(bonePath))
                {
                    // Check if bone has children that are used or are humanoid bones
                    bool hasProtectedChildren = false;
                    foreach (Transform child in bone)
                    {
                        string childPath = GetGameObjectPath(root.transform, child);
                        if (snapshot.UsedBonePaths.Contains(childPath) || protectedBones.Contains(child))
                        {
                            hasProtectedChildren = true;
                            break;
                        }
                    }

                    if (!hasProtectedChildren)
                    {
                        Undo.DestroyObjectImmediate(bone.gameObject);
                        prunedCount++;
                    }
                }
            }

            if (prunedCount > 0)
            {
                Debug.LogWarning($"[AvatarOptimizer] Pruned {prunedCount} bones (aggressive mode)");
            }
            if (skippedHumanoidCount > 0)
            {
                Debug.Log($"[AvatarOptimizer] Protected {skippedHumanoidCount} humanoid rig bones from pruning");
            }
        }

        /// <summary>
        /// Gets all humanoid rig bones that should never be pruned
        /// </summary>
        private HashSet<Transform> GetHumanoidBones(GameObject root)
        {
            var humanoidBones = new HashSet<Transform>();
            
            var animator = root.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                return humanoidBones;
            }

            // Get all standard humanoid bones
            foreach (HumanBodyBones boneType in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (boneType == HumanBodyBones.LastBone) continue;
                
                try
                {
                    var boneTransform = animator.GetBoneTransform(boneType);
                    if (boneTransform != null)
                    {
                        humanoidBones.Add(boneTransform);
                        
                        // Also protect all parent bones up to root
                        var parent = boneTransform.parent;
                        while (parent != null && parent != root.transform)
                        {
                            humanoidBones.Add(parent);
                            parent = parent.parent;
                        }
                    }
                }
                catch
                {
                    // Some bone types might not be mapped - that's okay
                }
            }

            return humanoidBones;
        }

        private string GetGameObjectPath(Transform root, Transform target)
        {
            if (target == null) return "";
            if (target == root) return root.name;

            var path = new List<string>();
            var current = target;

            while (current != null && current != root)
            {
                path.Add(current.name);
                current = current.parent;
            }

            path.Reverse();
            return string.Join("/", path);
        }
    }
}

