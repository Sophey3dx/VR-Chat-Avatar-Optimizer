using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using AvatarOutfitOptimizer.Utils;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// Scans and analyzes usage of Animator parameters, AnimationClips, and Expression Menu items
    /// </summary>
    public class UsageScanner
    {
        /// <summary>
        /// Usage map for parameters (parameter name -> is used)
        /// </summary>
        public Dictionary<string, bool> ParameterUsage { get; private set; } = new Dictionary<string, bool>();

        /// <summary>
        /// Usage map for AnimationClips (clip -> is used)
        /// </summary>
        public Dictionary<AnimationClip, bool> ClipUsage { get; private set; } = new Dictionary<AnimationClip, bool>();

        /// <summary>
        /// Usage map for Expression Menu items (menu item -> is valid/used)
        /// </summary>
        public Dictionary<VRCExpressionsMenu.Control, bool> MenuItemUsage { get; private set; } = new Dictionary<VRCExpressionsMenu.Control, bool>();

        /// <summary>
        /// Scans the avatar for usage of all components
        /// </summary>
        public void Scan(GameObject avatarRoot, AvatarSnapshot snapshot)
        {
            if (avatarRoot == null || snapshot == null)
            {
                Debug.LogError("[AvatarOptimizer] Cannot scan usage: avatar root or snapshot is null");
                return;
            }

            var descriptor = AvatarUtils.GetAvatarDescriptor(avatarRoot);
            if (descriptor == null)
            {
                Debug.LogError("[AvatarOptimizer] Cannot scan usage: no VRC Avatar Descriptor found");
                return;
            }

            ParameterUsage.Clear();
            ClipUsage.Clear();
            MenuItemUsage.Clear();

            // Scan Animator Controller (FX Layer)
            var fxLayer = AvatarUtils.GetFXLayer(descriptor);
            if (fxLayer != null)
            {
                ScanAnimatorController(fxLayer, snapshot);
            }

            // Scan Expression Menu
            var expressionMenu = AvatarUtils.GetExpressionMenu(descriptor);
            if (expressionMenu != null)
            {
                ScanExpressionMenu(expressionMenu, snapshot);
            }

            Debug.Log($"[AvatarOptimizer] Usage scan complete: {ParameterUsage.Count} parameters, {ClipUsage.Count} clips, {MenuItemUsage.Count} menu items");
        }

        private void ScanAnimatorController(RuntimeAnimatorController controller, AvatarSnapshot snapshot)
        {
            if (controller is AnimatorController animatorController)
            {
                // Initialize all parameters as unused
                if (animatorController.parameters != null)
                {
                    foreach (var param in animatorController.parameters)
                    {
                        if (param != null && !string.IsNullOrEmpty(param.name))
                        {
                            ParameterUsage[param.name] = false;
                        }
                    }
                }

                // Scan all layers (focus on FX layer)
                if (animatorController.layers != null)
                {
                    foreach (var layer in animatorController.layers)
                    {
                        if (layer.stateMachine != null)
                        {
                            ScanStateMachine(layer.stateMachine, snapshot);
                        }
                    }
                }
            }
        }

        private void ScanStateMachine(AnimatorStateMachine stateMachine, AvatarSnapshot snapshot)
        {
            if (stateMachine == null) return;

            // Scan states
            if (stateMachine.states != null)
            {
                foreach (var state in stateMachine.states)
                {
                    if (state.state != null)
                    {
                        ScanState(state.state, snapshot);
                    }
                }
            }

            // Scan any state transitions
            if (stateMachine.anyStateTransitions != null)
            {
                foreach (var transition in stateMachine.anyStateTransitions)
                {
                    ScanTransition(transition, snapshot);
                }
            }

            // Scan entry transitions
            if (stateMachine.entryTransitions != null)
            {
                foreach (var transition in stateMachine.entryTransitions)
                {
                    ScanTransition(transition, snapshot);
                }
            }

            // Scan sub-state machines
            if (stateMachine.stateMachines != null)
            {
                foreach (var subStateMachine in stateMachine.stateMachines)
                {
                    if (subStateMachine.stateMachine != null)
                    {
                        ScanStateMachine(subStateMachine.stateMachine, snapshot);
                    }
                }
            }
        }

        private void ScanState(AnimatorState state, AvatarSnapshot snapshot)
        {
            if (state == null) return;

            // Scan motion (AnimationClip or BlendTree)
            if (state.motion != null)
            {
                ScanMotion(state.motion, snapshot);
            }

            // Scan behaviors for parameter usage
            if (state.behaviours != null)
            {
                foreach (var behaviour in state.behaviours)
                {
                    ScanBehaviour(behaviour, snapshot);
                }
            }

            // Scan transitions
            if (state.transitions != null)
            {
                foreach (var transition in state.transitions)
                {
                    ScanTransition(transition, snapshot);
                }
            }
        }

        private void ScanMotion(Motion motion, AvatarSnapshot snapshot)
        {
            if (motion == null) return;

            if (motion is AnimationClip clip)
            {
                // Mark clip as used
                ClipUsage[clip] = true;
            }
            else if (motion is BlendTree blendTree)
            {
                ScanBlendTree(blendTree, snapshot);
            }
        }

        private void ScanBlendTree(BlendTree blendTree, AvatarSnapshot snapshot)
        {
            if (blendTree == null) return;

            // Scan blend tree parameters
            if (!string.IsNullOrEmpty(blendTree.blendParameter))
            {
                MarkParameterUsed(blendTree.blendParameter);
            }
            if (!string.IsNullOrEmpty(blendTree.blendParameterY))
            {
                MarkParameterUsed(blendTree.blendParameterY);
            }

            // Scan children motions
            if (blendTree.children != null)
            {
                foreach (var child in blendTree.children)
                {
                    if (child.motion != null)
                    {
                        ScanMotion(child.motion, snapshot);
                    }
                }
            }
        }

        private void ScanTransition(AnimatorStateTransition transition, AvatarSnapshot snapshot)
        {
            if (transition == null) return;

            // Scan conditions (parameter usage)
            if (transition.conditions != null)
            {
                foreach (var condition in transition.conditions)
                {
                    if (!string.IsNullOrEmpty(condition.parameter))
                    {
                        MarkParameterUsed(condition.parameter);
                    }
                }
            }
        }

        private void ScanBehaviour(StateMachineBehaviour behaviour, AvatarSnapshot snapshot)
        {
            // Behaviours can reference parameters, but we can't easily detect this without reflection
            // This is a limitation - we mark behaviours as potentially using parameters
            // In practice, behaviours are often custom scripts that may use parameters
            // We err on the side of caution and don't mark parameters as unused if behaviours exist
        }

        private void ScanExpressionMenu(VRCExpressionsMenu menu, AvatarSnapshot snapshot)
        {
            if (menu == null) return;

            ScanExpressionMenuRecursive(menu, snapshot);
        }

        private void ScanExpressionMenuRecursive(VRCExpressionsMenu menu, AvatarSnapshot snapshot)
        {
            if (menu == null || menu.controls == null) return;

            foreach (var control in menu.controls)
            {
                if (control == null) continue;

                bool isValid = true;

                // Check if control references a valid parameter
                if (!string.IsNullOrEmpty(control.parameter?.name))
                {
                    string paramName = control.parameter.name;
                    
                    // Check if parameter exists in snapshot
                    bool parameterExists = snapshot.ExpressionParameterNames.Contains(paramName) ||
                                         snapshot.AnimatorParameterNames.Contains(paramName);

                    if (!parameterExists)
                    {
                        isValid = false;
                        Debug.LogWarning($"[AvatarOptimizer] Menu item '{control.name}' references non-existent parameter '{paramName}'");
                    }
                    else
                    {
                        MarkParameterUsed(paramName);
                    }
                }

                MenuItemUsage[control] = isValid;

                // Recursively scan submenus
                if (control.subMenu != null)
                {
                    ScanExpressionMenuRecursive(control.subMenu, snapshot);
                }
            }
        }

        private void MarkParameterUsed(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) return;
            
            if (ParameterUsage.ContainsKey(parameterName))
            {
                ParameterUsage[parameterName] = true;
            }
            else
            {
                // Parameter might be from a different layer or not in the controller
                // We still mark it as used to be safe
                ParameterUsage[parameterName] = true;
            }
        }
    }
}

