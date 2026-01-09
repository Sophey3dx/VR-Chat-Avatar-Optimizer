using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Coordinates all cleanup operations on duplicated avatar
    /// NEVER modifies original avatar - always works on duplicates
    /// </summary>
    public class CleanupCoordinator
    {
        private ObjectCleanup objectCleanup;
        private AnimatorCleanup animatorCleanup;
        private MenuCleanup menuCleanup;
        private PhysBoneCleanup physBoneCleanup;

        public CleanupCoordinator()
        {
            objectCleanup = new ObjectCleanup();
            animatorCleanup = new AnimatorCleanup();
            menuCleanup = new MenuCleanup();
            physBoneCleanup = new PhysBoneCleanup();
        }

        /// <summary>
        /// Creates a duplicate of the avatar and performs cleanup operations
        /// </summary>
        public GameObject CreateOptimizedAvatar(
            GameObject originalAvatar,
            AvatarSnapshot snapshot,
            UsageScanner usageScanner,
            MenuAnalyzer.MenuAnalysisResult menuAnalysis,
            AnimatorAnalyzer.AnimatorAnalysisResult animatorAnalysis,
            PhysBoneAnalyzer.PhysBoneAnalysisResult physBoneAnalysis,
            bool aggressiveAnimatorCleanup,
            bool aggressiveBonePruning,
            bool dryRun,
            bool cleanupObjects = true,
            bool cleanupAnimator = true,
            bool cleanupMenu = true,
            bool cleanupPhysBones = true)
        {
            if (originalAvatar == null)
            {
                Debug.LogError("[AvatarOptimizer] Cannot create optimized avatar: original avatar is null");
                return null;
            }

            // Safety check: Verify we're not modifying the original
            if (!dryRun)
            {
                // Create duplicate
                GameObject duplicate = InstantiateAvatar(originalAvatar);
                if (duplicate == null)
                {
                    Debug.LogError("[AvatarOptimizer] Failed to create avatar duplicate");
                    return null;
                }

                // Perform cleanup operations
                PerformCleanup(
                    duplicate,
                    snapshot,
                    usageScanner,
                    menuAnalysis,
                    animatorAnalysis,
                    physBoneAnalysis,
                    aggressiveAnimatorCleanup,
                    aggressiveBonePruning,
                    cleanupObjects,
                    cleanupAnimator,
                    cleanupMenu,
                    cleanupPhysBones);

                return duplicate;
            }
            else
            {
                // Dry run: Just return null (no duplicate created)
                Debug.Log("[AvatarOptimizer] Dry run mode - no changes applied");
                return null;
            }
        }

        private GameObject InstantiateAvatar(GameObject original)
        {
            // Create duplicate with unique name
            string duplicateName = $"{original.name}_Optimized";
            GameObject duplicate = Object.Instantiate(original);
            duplicate.name = duplicateName;

            // Register undo
            Undo.RegisterCreatedObjectUndo(duplicate, "Create Optimized Avatar");

            Debug.Log($"[AvatarOptimizer] Created duplicate avatar: {duplicateName}");
            return duplicate;
        }

        private void PerformCleanup(
            GameObject duplicate,
            AvatarSnapshot snapshot,
            UsageScanner usageScanner,
            MenuAnalyzer.MenuAnalysisResult menuAnalysis,
            AnimatorAnalyzer.AnimatorAnalysisResult animatorAnalysis,
            PhysBoneAnalyzer.PhysBoneAnalysisResult physBoneAnalysis,
            bool aggressiveAnimatorCleanup,
            bool aggressiveBonePruning,
            bool cleanupObjects,
            bool cleanupAnimator,
            bool cleanupMenu,
            bool cleanupPhysBones)
        {
            // Order matters: Clean up objects first, then components
            // Only perform cleanup for selected areas
            
            if (cleanupObjects)
            {
                // 1. Object cleanup (removes inactive GameObjects, unused renderers)
                objectCleanup.Cleanup(duplicate, snapshot, aggressiveBonePruning, physBoneAnalysis);
            }

            if (cleanupPhysBones)
            {
                // 2. PhysBone cleanup (removes PhysBones on deleted objects)
                physBoneCleanup.Cleanup(duplicate, physBoneAnalysis);
            }

            if (cleanupAnimator)
            {
                // 3. Animator cleanup (removes unused clips, parameters, states)
                animatorCleanup.Cleanup(duplicate, animatorAnalysis, usageScanner, aggressiveAnimatorCleanup);
            }

            if (cleanupMenu)
            {
                // 4. Menu cleanup (removes broken parameters, empty submenus)
                menuCleanup.Cleanup(duplicate, menuAnalysis);
            }

            Debug.Log("[AvatarOptimizer] Cleanup operations completed");
        }
    }
}

