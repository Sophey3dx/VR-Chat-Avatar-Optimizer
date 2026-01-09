using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Animations;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// Analyzes Animator Controllers for unused clips, parameters, states, and transitions
    /// CRITICAL: A clip is only unused if NO State, Transition, BlendTree, or AnyState references it
    /// </summary>
    public class AnimatorAnalyzer
    {
        public class AnimatorAnalysisResult
        {
            public List<AnimationClip> UnusedClips = new List<AnimationClip>();
            public List<AnimationClip> PotentiallyUnusedClips = new List<AnimationClip>(); // Needs warning
            public List<string> UnusedParameters = new List<string>();
            public List<AnimatorState> UnusedStates = new List<AnimatorState>();
            public List<AnimatorStateTransition> UnusedTransitions = new List<AnimatorStateTransition>();
            public HashSet<AnimationClip> AllReferencedClips = new HashSet<AnimationClip>();
            public HashSet<string> AllReferencedParameters = new HashSet<string>();
        }

        /// <summary>
        /// Analyzes the Animator Controller for unused components
        /// </summary>
        public AnimatorAnalysisResult Analyze(RuntimeAnimatorController controller, UsageScanner usageScanner)
        {
            var result = new AnimatorAnalysisResult();
            
            if (controller == null)
            {
                return result;
            }

            if (controller is AnimatorController animatorController)
            {
                // First pass: Find all referenced clips and parameters
                FindAllReferences(animatorController, result);

                // Second pass: Identify unused components
                IdentifyUnusedComponents(animatorController, usageScanner, result);
            }

            return result;
        }

        private void FindAllReferences(AnimatorController controller, AnimatorAnalysisResult result)
        {
            if (controller.layers == null) return;

            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine != null)
                {
                    FindReferencesInStateMachine(layer.stateMachine, result);
                }
            }
        }

        private void FindReferencesInStateMachine(AnimatorStateMachine stateMachine, AnimatorAnalysisResult result)
        {
            if (stateMachine == null) return;

            // Scan states
            if (stateMachine.states != null)
            {
                foreach (var state in stateMachine.states)
                {
                    if (state.state != null)
                    {
                        FindReferencesInState(state.state, result);
                    }
                }
            }

            // Scan any state transitions
            if (stateMachine.anyStateTransitions != null)
            {
                foreach (var transition in stateMachine.anyStateTransitions)
                {
                    FindReferencesInTransition(transition, result);
                }
            }

            // Scan entry transitions
            if (stateMachine.entryTransitions != null)
            {
                foreach (var transition in stateMachine.entryTransitions)
                {
                    FindReferencesInTransition(transition, result);
                }
            }

            // Scan sub-state machines
            if (stateMachine.stateMachines != null)
            {
                foreach (var subStateMachine in stateMachine.stateMachines)
                {
                    if (subStateMachine.stateMachine != null)
                    {
                        FindReferencesInStateMachine(subStateMachine.stateMachine, result);
                    }
                }
            }
        }

        private void FindReferencesInState(AnimatorState state, AnimatorAnalysisResult result)
        {
            if (state == null) return;

            // Check motion (clip or blend tree)
            if (state.motion != null)
            {
                FindReferencesInMotion(state.motion, result);
            }

            // Check behaviours - they might reference parameters indirectly
            // We can't easily detect this, so we mark states with behaviours as potentially using parameters
            if (state.behaviours != null && state.behaviours.Length > 0)
            {
                // Behaviours might use parameters - we can't safely assume they don't
                // This is why we need to be cautious
            }

            // Check transitions
            if (state.transitions != null)
            {
                foreach (var transition in state.transitions)
                {
                    FindReferencesInTransition(transition, result);
                }
            }
        }

        private void FindReferencesInMotion(Motion motion, AnimatorAnalysisResult result)
        {
            if (motion == null) return;

            if (motion is AnimationClip clip)
            {
                result.AllReferencedClips.Add(clip);
            }
            else if (motion is BlendTree blendTree)
            {
                FindReferencesInBlendTree(blendTree, result);
            }
        }

        private void FindReferencesInBlendTree(BlendTree blendTree, AnimatorAnalysisResult result)
        {
            if (blendTree == null) return;

            // Blend tree parameters
            if (!string.IsNullOrEmpty(blendTree.blendParameter))
            {
                result.AllReferencedParameters.Add(blendTree.blendParameter);
            }
            if (!string.IsNullOrEmpty(blendTree.blendParameterY))
            {
                result.AllReferencedParameters.Add(blendTree.blendParameterY);
            }

            // Children motions
            if (blendTree.children != null)
            {
                foreach (var child in blendTree.children)
                {
                    if (child.motion != null)
                    {
                        FindReferencesInMotion(child.motion, result);
                    }
                }
            }
        }

        private void FindReferencesInTransition(AnimatorTransitionBase transition, AnimatorAnalysisResult result)
        {
            if (transition == null) return;

            // Transition conditions reference parameters
            if (transition.conditions != null)
            {
                foreach (var condition in transition.conditions)
                {
                    if (!string.IsNullOrEmpty(condition.parameter))
                    {
                        result.AllReferencedParameters.Add(condition.parameter);
                    }
                }
            }
        }

        private void IdentifyUnusedComponents(
            AnimatorController controller,
            UsageScanner usageScanner,
            AnimatorAnalysisResult result)
        {
            // Find unused parameters
            if (controller.parameters != null)
            {
                foreach (var param in controller.parameters)
                {
                    if (param != null && !string.IsNullOrEmpty(param.name))
                    {
                        bool isUsed = result.AllReferencedParameters.Contains(param.name) ||
                                     (usageScanner.ParameterUsage.ContainsKey(param.name) && 
                                      usageScanner.ParameterUsage[param.name]);

                        if (!isUsed)
                        {
                            result.UnusedParameters.Add(param.name);
                        }
                    }
                }
            }

            // Find unused clips
            // CRITICAL: Only mark as unused if NOT referenced anywhere
            var allClips = new HashSet<AnimationClip>();
            
            // Get all clips from controller
            if (controller.animationClips != null)
            {
                foreach (var clip in controller.animationClips)
                {
                    if (clip != null)
                    {
                        allClips.Add(clip);
                    }
                }
            }

            foreach (var clip in allClips)
            {
                bool isReferenced = result.AllReferencedClips.Contains(clip) ||
                                   (usageScanner.ClipUsage.ContainsKey(clip) && 
                                    usageScanner.ClipUsage[clip]);

                if (!isReferenced)
                {
                    // Check if clip might be used indirectly (behaviours, write defaults off, etc.)
                    // If uncertain, mark as potentially unused (warning only)
                    bool mightBeUsed = false;
                    
                    // TODO: Check for Write Defaults Off states that might need this clip
                    // TODO: Check for behaviours that might reference this clip
                    
                    if (mightBeUsed)
                    {
                        result.PotentiallyUnusedClips.Add(clip);
                    }
                    else
                    {
                        result.UnusedClips.Add(clip);
                    }
                }
            }

            // Find unused states (states with no incoming transitions and not entry state)
            // This is complex and risky - we skip it for now and only warn
            // TODO: Implement safe state analysis
        }
    }
}

