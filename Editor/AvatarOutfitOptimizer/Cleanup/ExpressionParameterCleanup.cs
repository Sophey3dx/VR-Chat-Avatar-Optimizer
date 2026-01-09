using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Cleans up VRCExpressionParameters: removes unused parameters
    /// IMPORTANT: Always duplicates the asset first to protect the original
    /// </summary>
    public class ExpressionParameterCleanup
    {
        /// <summary>
        /// Cleans up Expression Parameters on duplicate avatar
        /// Creates a duplicate of the parameters asset to protect the original
        /// </summary>
        public void Cleanup(
            GameObject duplicate,
            AnimatorAnalyzer.AnimatorAnalysisResult animatorAnalysis,
            UsageScanner usageScanner)
        {
            if (duplicate == null)
            {
                Debug.LogError("[AvatarOptimizer] ExpressionParameterCleanup: Invalid parameters");
                return;
            }

            var descriptor = AvatarUtils.GetAvatarDescriptor(duplicate);
            if (descriptor == null)
            {
                Debug.LogWarning("[AvatarOptimizer] No VRC Avatar Descriptor found - skipping Expression Parameter cleanup");
                return;
            }

            var originalParams = AvatarUtils.GetExpressionParameters(descriptor);
            if (originalParams == null)
            {
                Debug.LogWarning("[AvatarOptimizer] No Expression Parameters found - skipping cleanup");
                return;
            }

            // Determine which parameters are actually used
            var usedParameters = new HashSet<string>();
            
            // Parameters used by animator
            if (usageScanner != null && usageScanner.ParameterUsage != null)
            {
                foreach (var kvp in usageScanner.ParameterUsage)
                {
                    if (kvp.Value) // Parameter is used
                    {
                        usedParameters.Add(kvp.Key);
                    }
                }
            }

            // Parameters referenced by animator analysis
            if (animatorAnalysis != null && animatorAnalysis.AllReferencedParameters != null)
            {
                foreach (var param in animatorAnalysis.AllReferencedParameters)
                {
                    usedParameters.Add(param);
                }
            }

            // Check if any parameters would be removed
            var paramsToRemove = new List<VRCExpressionParameters.Parameter>();
            if (originalParams.parameters != null)
            {
                foreach (var param in originalParams.parameters)
                {
                    if (param != null && !string.IsNullOrEmpty(param.name))
                    {
                        if (!usedParameters.Contains(param.name))
                        {
                            paramsToRemove.Add(param);
                        }
                    }
                }
            }

            if (paramsToRemove.Count == 0)
            {
                Debug.Log("[AvatarOptimizer] No unused Expression Parameters found");
                return;
            }

            // Duplicate the parameters asset to protect the original
            var duplicatedParams = DuplicateExpressionParameters(originalParams, duplicate.name);
            if (duplicatedParams == null)
            {
                Debug.LogError("[AvatarOptimizer] Failed to duplicate Expression Parameters - skipping cleanup");
                return;
            }

            // Assign duplicated parameters to the avatar
            Undo.RecordObject(descriptor, "Set Expression Parameters");
            descriptor.expressionParameters = duplicatedParams;
            EditorUtility.SetDirty(descriptor);

            // Remove unused parameters from the duplicated asset
            var paramsToKeep = duplicatedParams.parameters
                .Where(p => p != null && !string.IsNullOrEmpty(p.name) && usedParameters.Contains(p.name))
                .ToArray();

            Undo.RecordObject(duplicatedParams, "Remove Unused Expression Parameters");
            duplicatedParams.parameters = paramsToKeep;
            EditorUtility.SetDirty(duplicatedParams);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AvatarOptimizer] Removed {paramsToRemove.Count} unused Expression Parameters");
            foreach (var param in paramsToRemove)
            {
                Debug.Log($"[AvatarOptimizer]   - Removed: {param.name}");
            }
        }

        /// <summary>
        /// Duplicates a VRCExpressionParameters asset to protect the original
        /// </summary>
        private VRCExpressionParameters DuplicateExpressionParameters(VRCExpressionParameters original, string avatarName)
        {
            if (original == null) return null;

            string originalPath = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(originalPath))
            {
                Debug.LogWarning("[AvatarOptimizer] Original parameters has no asset path - cannot duplicate");
                return null;
            }

            // Create a unique path for the duplicate
            string directory = Path.GetDirectoryName(originalPath);
            string originalName = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            string duplicatePath = Path.Combine(directory, $"{originalName}_{avatarName}_Optimized{extension}");
            
            // Ensure unique path
            duplicatePath = AssetDatabase.GenerateUniqueAssetPath(duplicatePath);

            // Copy the asset
            if (!AssetDatabase.CopyAsset(originalPath, duplicatePath))
            {
                Debug.LogError($"[AvatarOptimizer] Failed to copy Expression Parameters to {duplicatePath}");
                return null;
            }

            AssetDatabase.Refresh();

            var duplicated = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(duplicatePath);
            if (duplicated != null)
            {
                Debug.Log($"[AvatarOptimizer] Created duplicate parameters: {duplicatePath}");
            }

            return duplicated;
        }
    }
}
