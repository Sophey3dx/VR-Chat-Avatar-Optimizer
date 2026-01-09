using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Cleans up Expression Menus: removes broken parameters and empty submenus
    /// ONLY safe operations - never removes manually linked or externally reused menus
    /// </summary>
    public class MenuCleanup
    {
        /// <summary>
        /// Cleans up Expression Menu on duplicate avatar
        /// </summary>
        public void Cleanup(
            GameObject duplicate,
            MenuAnalyzer.MenuAnalysisResult analysis)
        {
            if (duplicate == null || analysis == null)
            {
                Debug.LogError("[AvatarOptimizer] MenuCleanup: Invalid parameters");
                return;
            }

            var descriptor = AvatarUtils.GetAvatarDescriptor(duplicate);
            if (descriptor == null)
            {
                Debug.LogWarning("[AvatarOptimizer] No VRC Avatar Descriptor found - skipping Menu cleanup");
                return;
            }

            var expressionMenu = AvatarUtils.GetExpressionMenu(descriptor);
            if (expressionMenu == null)
            {
                Debug.LogWarning("[AvatarOptimizer] No Expression Menu found - skipping Menu cleanup");
                return;
            }

            // Remove broken parameter controls (safe)
            RemoveBrokenParameterControls(expressionMenu, analysis);

            // Remove empty submenus (safe)
            RemoveEmptySubmenus(expressionMenu, analysis);

            // Warn about unreachable menus (but don't remove - too risky)
            if (analysis.UnreachableMenus != null && analysis.UnreachableMenus.Count > 0)
            {
                Debug.LogWarning($"[AvatarOptimizer] Found {analysis.UnreachableMenus.Count} unreachable menus - not removing (manual review recommended)");
            }
        }

        private void RemoveBrokenParameterControls(
            VRCExpressionsMenu menu,
            MenuAnalyzer.MenuAnalysisResult analysis)
        {
            if (menu == null || menu.controls == null || analysis.BrokenParameterControls == null)
            {
                return;
            }

            var controlsToRemove = new HashSet<VRCExpressionsMenu.Control>(analysis.BrokenParameterControls);
            var newControls = new List<VRCExpressionsMenu.Control>();

            foreach (var control in menu.controls)
            {
                if (control != null && !controlsToRemove.Contains(control))
                {
                    newControls.Add(control);
                }
            }

            int originalCount = menu.controls.Count;
            if (newControls.Count < originalCount)
            {
                Undo.RecordObject(menu, "Remove Broken Parameter Controls");
                menu.controls.Clear();
                menu.controls.AddRange(newControls);
                EditorUtility.SetDirty(menu);
                Debug.Log($"[AvatarOptimizer] Removed {originalCount - newControls.Count} broken parameter controls");
            }

            // Recursively process submenus
            foreach (var control in menu.controls)
            {
                if (control != null && control.subMenu != null)
                {
                    RemoveBrokenParameterControls(control.subMenu, analysis);
                }
            }
        }

        private void RemoveEmptySubmenus(
            VRCExpressionsMenu menu,
            MenuAnalyzer.MenuAnalysisResult analysis)
        {
            if (menu == null || menu.controls == null || analysis.EmptySubmenus == null)
            {
                return;
            }

            var emptySubmenus = new HashSet<VRCExpressionsMenu>(analysis.EmptySubmenus);

            // Check if any controls reference empty submenus
            var controlsToRemove = new List<VRCExpressionsMenu.Control>();

            foreach (var control in menu.controls)
            {
                if (control != null && control.subMenu != null)
                {
                    if (emptySubmenus.Contains(control.subMenu))
                    {
                        // Remove control that references empty submenu
                        controlsToRemove.Add(control);
                    }
                    else
                    {
                        // Recursively process submenu
                        RemoveEmptySubmenus(control.subMenu, analysis);
                    }
                }
            }

            if (controlsToRemove.Count > 0)
            {
                Undo.RecordObject(menu, "Remove Empty Submenus");
                var keepControls = menu.controls.Where(c => !controlsToRemove.Contains(c)).ToList();
                menu.controls.Clear();
                menu.controls.AddRange(keepControls);
                EditorUtility.SetDirty(menu);
                Debug.Log($"[AvatarOptimizer] Removed {controlsToRemove.Count} empty submenus");
            }
        }
    }
}

