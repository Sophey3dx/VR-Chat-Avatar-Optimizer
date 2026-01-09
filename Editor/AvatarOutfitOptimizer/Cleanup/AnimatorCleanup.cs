using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Cleans up Animator Controller: removes unused clips, parameters, states, transitions
    /// IMPORTANT: Always duplicates the controller first to protect the original asset
    /// </summary>
    public class AnimatorCleanup
    {
        /// <summary>
        /// Cleans up Animator Controller on duplicate avatar
        /// Creates a duplicate of the controller to protect the original
        /// </summary>
        public void Cleanup(
            GameObject duplicate,
            AnimatorAnalyzer.AnimatorAnalysisResult analysis,
            UsageScanner usageScanner,
            bool aggressiveMode)
        {
            if (duplicate == null || analysis == null)
            {
                Debug.LogError("[AvatarOptimizer] AnimatorCleanup: Invalid parameters");
                return;
            }

            var descriptor = AvatarUtils.GetAvatarDescriptor(duplicate);
            if (descriptor == null)
            {
                Debug.LogWarning("[AvatarOptimizer] No VRC Avatar Descriptor found - skipping Animator cleanup");
                return;
            }

            var originalFxLayer = AvatarUtils.GetFXLayer(descriptor);
            if (originalFxLayer == null)
            {
                Debug.LogWarning("[AvatarOptimizer] No FX Layer found - skipping Animator cleanup");
                return;
            }

            if (originalFxLayer is AnimatorController originalController)
            {
                // IMPORTANT: Duplicate the controller to protect the original asset
                AnimatorController duplicatedController = DuplicateAnimatorController(originalController, duplicate.name);
                
                if (duplicatedController == null)
                {
                    Debug.LogError("[AvatarOptimizer] Failed to duplicate AnimatorController - skipping cleanup");
                    return;
                }

                // Assign duplicated controller to the avatar
                SetFXLayer(descriptor, duplicatedController);

                // Now perform cleanup on the DUPLICATED controller
                RemoveUnusedParameters(duplicatedController, analysis);
                RemoveUnusedClips(duplicatedController, analysis, aggressiveMode);

                // Remove unused states and transitions (risky - only in aggressive mode)
                if (aggressiveMode)
                {
                    RemoveUnusedStates(duplicatedController, analysis);
                }

                Debug.Log("[AvatarOptimizer] Animator cleanup completed (using duplicated controller)");
            }
        }

        /// <summary>
        /// Duplicates an AnimatorController asset to protect the original
        /// </summary>
        private AnimatorController DuplicateAnimatorController(AnimatorController original, string avatarName)
        {
            if (original == null) return null;

            string originalPath = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(originalPath))
            {
                Debug.LogWarning("[AvatarOptimizer] Original controller has no asset path - cannot duplicate");
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
                Debug.LogError($"[AvatarOptimizer] Failed to copy AnimatorController to {duplicatePath}");
                return null;
            }

            AssetDatabase.Refresh();

            var duplicated = AssetDatabase.LoadAssetAtPath<AnimatorController>(duplicatePath);
            if (duplicated != null)
            {
                Debug.Log($"[AvatarOptimizer] Created duplicate controller: {duplicatePath}");
            }

            return duplicated;
        }

        /// <summary>
        /// Sets the FX layer controller on the avatar descriptor
        /// </summary>
        private void SetFXLayer(VRCAvatarDescriptor descriptor, AnimatorController controller)
        {
            if (descriptor == null || controller == null) return;

            var layers = descriptor.baseAnimationLayers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].type == VRCAvatarDescriptor.AnimLayerType.FX)
                {
                    Undo.RecordObject(descriptor, "Set FX Layer Controller");
                    layers[i].animatorController = controller;
                    descriptor.baseAnimationLayers = layers;
                    EditorUtility.SetDirty(descriptor);
                    break;
                }
            }
        }

        private void RemoveUnusedParameters(
            AnimatorController controller,
            AnimatorAnalyzer.AnimatorAnalysisResult analysis)
        {
            if (controller.parameters == null || analysis.UnusedParameters == null)
            {
                return;
            }

            int removedCount = 0;

            // Create list of parameters to keep
            var parametersToKeep = new List<AnimatorControllerParameter>();
            
            foreach (var param in controller.parameters)
            {
                if (param != null && !analysis.UnusedParameters.Contains(param.name))
                {
                    parametersToKeep.Add(param);
                }
                else
                {
                    removedCount++;
                }
            }

            // Rebuild parameters list
            if (removedCount > 0)
            {
                Undo.RecordObject(controller, "Remove Unused Animator Parameters");
                controller.parameters = parametersToKeep.ToArray();
                EditorUtility.SetDirty(controller);
                Debug.Log($"[AvatarOptimizer] Removed {removedCount} unused Animator parameters");
            }
        }

        private void RemoveUnusedClips(
            AnimatorController controller,
            AnimatorAnalyzer.AnimatorAnalysisResult analysis,
            bool aggressiveMode)
        {
            var clipsToRemove = aggressiveMode
                ? analysis.UnusedClips.Concat(analysis.PotentiallyUnusedClips).ToList()
                : analysis.UnusedClips;

            if (clipsToRemove.Count == 0)
            {
                return;
            }

            // Remove clips from controller
            // Note: This is complex because clips can be in multiple places
            // We need to remove them from all states and blend trees
            
            Undo.RecordObject(controller, "Remove Unused Animation Clips");

            int removedCount = 0;

            // Remove clips from all layers
            if (controller.layers != null)
            {
                foreach (var layer in controller.layers)
                {
                    if (layer.stateMachine != null)
                    {
                        removedCount += RemoveClipsFromStateMachine(layer.stateMachine, clipsToRemove);
                    }
                }
            }

            if (removedCount > 0)
            {
                EditorUtility.SetDirty(controller);
                Debug.Log($"[AvatarOptimizer] Removed {removedCount} unused Animation clips");
            }
        }

        private int RemoveClipsFromStateMachine(
            AnimatorStateMachine stateMachine,
            List<AnimationClip> clipsToRemove)
        {
            int removedCount = 0;

            if (stateMachine.states != null)
            {
                foreach (var state in stateMachine.states)
                {
                    if (state.state != null)
                    {
                        if (state.state.motion != null)
                        {
                            if (clipsToRemove.Contains(state.state.motion as AnimationClip))
                            {
                                Undo.RecordObject(state.state, "Remove Unused Clip");
                                state.state.motion = null;
                                removedCount++;
                            }
                            else if (state.state.motion is BlendTree blendTree)
                            {
                                removedCount += RemoveClipsFromBlendTree(blendTree, clipsToRemove);
                            }
                        }
                    }
                }
            }

            // Recursively process sub-state machines
            if (stateMachine.stateMachines != null)
            {
                foreach (var subStateMachine in stateMachine.stateMachines)
                {
                    if (subStateMachine.stateMachine != null)
                    {
                        removedCount += RemoveClipsFromStateMachine(subStateMachine.stateMachine, clipsToRemove);
                    }
                }
            }

            return removedCount;
        }

        private int RemoveClipsFromBlendTree(BlendTree blendTree, List<AnimationClip> clipsToRemove)
        {
            int removedCount = 0;

            if (blendTree.children != null)
            {
                for (int i = blendTree.children.Length - 1; i >= 0; i--)
                {
                    var child = blendTree.children[i];
                    if (child.motion != null)
                    {
                        if (clipsToRemove.Contains(child.motion as AnimationClip))
                        {
                            Undo.RecordObject(blendTree, "Remove Unused Clip from BlendTree");
                            var newChildren = new List<ChildMotion>(blendTree.children);
                            newChildren.RemoveAt(i);
                            blendTree.children = newChildren.ToArray();
                            removedCount++;
                        }
                        else if (child.motion is BlendTree subBlendTree)
                        {
                            removedCount += RemoveClipsFromBlendTree(subBlendTree, clipsToRemove);
                        }
                    }
                }
            }

            return removedCount;
        }

        private void RemoveUnusedStates(
            AnimatorController controller,
            AnimatorAnalyzer.AnimatorAnalysisResult analysis)
        {
            // Removing states is very risky
            // We only do this in aggressive mode and with careful validation
            // For now, we skip this - it's too complex and error-prone
            
            Debug.LogWarning("[AvatarOptimizer] State removal not implemented - too risky");
        }
    }
}

