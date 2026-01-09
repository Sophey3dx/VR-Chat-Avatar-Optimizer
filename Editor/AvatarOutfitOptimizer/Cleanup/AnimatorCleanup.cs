using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer.Cleanup
{
    /// <summary>
    /// Cleans up Animator Controller: removes unused clips, parameters, states, transitions
    /// Vorsichtig: Only removes safely unused components
    /// </summary>
    public class AnimatorCleanup
    {
        /// <summary>
        /// Cleans up Animator Controller on duplicate avatar
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

            var fxLayer = AvatarUtils.GetFXLayer(descriptor);
            if (fxLayer == null)
            {
                Debug.LogWarning("[AvatarOptimizer] No FX Layer found - skipping Animator cleanup");
                return;
            }

            if (fxLayer is AnimatorController controller)
            {
                // Remove unused parameters
                RemoveUnusedParameters(controller, analysis);

                // Remove unused clips
                RemoveUnusedClips(controller, analysis, aggressiveMode);

                // Remove unused states and transitions (risky - only in aggressive mode)
                if (aggressiveMode)
                {
                    RemoveUnusedStates(controller, analysis);
                }

                Debug.Log("[AvatarOptimizer] Animator cleanup completed");
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

