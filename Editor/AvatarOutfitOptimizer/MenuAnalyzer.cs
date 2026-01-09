using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// Analyzes Expression Menus for broken references and empty submenus
    /// </summary>
    public class MenuAnalyzer
    {
        public class MenuAnalysisResult
        {
            public List<VRCExpressionsMenu.Control> BrokenParameterControls = new List<VRCExpressionsMenu.Control>();
            public List<VRCExpressionsMenu> EmptySubmenus = new List<VRCExpressionsMenu>();
            public List<VRCExpressionsMenu> UnreachableMenus = new List<VRCExpressionsMenu>();
            public int TotalParameterBudget = 0;
            public int UsedParameterBudget = 0;
        }

        /// <summary>
        /// Analyzes the expression menu for issues
        /// </summary>
        public MenuAnalysisResult Analyze(VRCExpressionsMenu menu, AvatarSnapshot snapshot, HashSet<string> validParameters)
        {
            var result = new MenuAnalysisResult();
            
            if (menu == null)
            {
                return result;
            }

            // Calculate parameter budget
            result.TotalParameterBudget = CalculateParameterBudget(snapshot);
            result.UsedParameterBudget = validParameters.Count;

            // Analyze menu recursively
            AnalyzeMenuRecursive(menu, snapshot, validParameters, result, new HashSet<VRCExpressionsMenu>());

            return result;
        }

        private void AnalyzeMenuRecursive(
            VRCExpressionsMenu menu,
            AvatarSnapshot snapshot,
            HashSet<string> validParameters,
            MenuAnalysisResult result,
            HashSet<VRCExpressionsMenu> visitedMenus)
        {
            if (menu == null || visitedMenus.Contains(menu))
            {
                return;
            }

            visitedMenus.Add(menu);

            if (menu.controls == null || menu.controls.Count == 0)
            {
                // Empty menu - but only mark as empty if it's a submenu (not root)
                if (visitedMenus.Count > 1)
                {
                    result.EmptySubmenus.Add(menu);
                }
                return;
            }

            bool hasValidControls = false;

            foreach (var control in menu.controls)
            {
                if (control == null) continue;

                // Check for broken parameter references
                if (!string.IsNullOrEmpty(control.parameter?.name))
                {
                    string paramName = control.parameter.name;
                    
                    if (!validParameters.Contains(paramName) &&
                        !snapshot.ExpressionParameterNames.Contains(paramName) &&
                        !snapshot.AnimatorParameterNames.Contains(paramName))
                    {
                        result.BrokenParameterControls.Add(control);
                    }
                    else
                    {
                        hasValidControls = true;
                    }
                }
                else
                {
                    // Control without parameter (submenu, toggle, etc.)
                    hasValidControls = true;
                }

                // Recursively analyze submenus
                if (control.subMenu != null)
                {
                    AnalyzeMenuRecursive(control.subMenu, snapshot, validParameters, result, visitedMenus);
                }
            }

            // Check if menu is unreachable (no valid controls and not root)
            if (!hasValidControls && visitedMenus.Count > 1)
            {
                result.UnreachableMenus.Add(menu);
            }
        }

        private int CalculateParameterBudget(AvatarSnapshot snapshot)
        {
            // VRChat parameter budget calculation
            // Expression Parameters have a budget based on parameter types
            // This is a simplified calculation - actual budget depends on parameter types
            int budget = 0;
            
            // Each parameter type has different memory cost
            // For now, we use a conservative estimate
            // TODO: More accurate budget calculation based on actual parameter types
            
            return snapshot.ParameterCount;
        }
    }
}

