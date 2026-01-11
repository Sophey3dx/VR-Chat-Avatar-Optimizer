using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace OutfitWizard
{
    /// <summary>
    /// Creates single-item toggles with animator, parameters, and menu entries
    /// </summary>
    public static class ToggleCreator
    {
        public class ToggleConfig
        {
            public GameObject TargetObject;
            public string ToggleName;
            public string MenuPath = "Outfits";
            public bool DefaultOn = true;
            public bool Saved = true;
            public List<Component> PhysBonesToToggle = new List<Component>();
            public List<SingleToggleWizard.BlendshapeInfo> BlendshapesToAnimate = new List<SingleToggleWizard.BlendshapeInfo>();
        }
        
        public static void CreateToggle(VRCAvatarDescriptor avatar, ToggleConfig config)
        {
            if (avatar == null || config.TargetObject == null)
            {
                Debug.LogError("[OutfitWizard] Avatar or target object is null");
                return;
            }
            
            string parameterName = SanitizeParameterName($"{config.MenuPath}/{config.ToggleName}");
            string assetFolder = GetOrCreateAssetFolder(avatar);
            
            // 1. Create animation clips
            var (onClip, offClip) = CreateAnimationClips(avatar, config, assetFolder);
            
            // 2. Add parameter to avatar
            AddExpressionParameter(avatar, parameterName, config.Saved, config.DefaultOn);
            
            // 3. Create animator layer
            AddAnimatorLayer(avatar, parameterName, onClip, offClip, config.DefaultOn);
            
            // 4. Add menu entry
            AddMenuEntry(avatar, config.MenuPath, config.ToggleName, parameterName);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[OutfitWizard] Created toggle '{config.ToggleName}' successfully!");
        }
        
        private static string SanitizeParameterName(string name)
        {
            return name.Replace(" ", "_").Replace("-", "_");
        }
        
        private static string GetOrCreateAssetFolder(VRCAvatarDescriptor avatar)
        {
            string avatarPath = AssetDatabase.GetAssetPath(avatar.gameObject);
            string folder;
            
            if (string.IsNullOrEmpty(avatarPath))
            {
                folder = "Assets/OutfitWizard/Generated";
            }
            else
            {
                folder = Path.GetDirectoryName(avatarPath);
                folder = Path.Combine(folder, "OutfitWizard");
            }
            
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    AssetDatabase.CreateFolder("Assets", "OutfitWizard");
                    parent = "Assets/OutfitWizard";
                }
                AssetDatabase.CreateFolder(parent, "Generated");
            }
            
            return folder.Replace("\\", "/");
        }
        
        private static (AnimationClip onClip, AnimationClip offClip) CreateAnimationClips(
            VRCAvatarDescriptor avatar, ToggleConfig config, string folder)
        {
            string baseName = SanitizeParameterName(config.ToggleName);
            
            // ON Clip
            var onClip = new AnimationClip { name = $"{baseName}_ON" };
            var offClip = new AnimationClip { name = $"{baseName}_OFF" };
            
            // Get relative path to target
            string targetPath = GetRelativePath(avatar.transform, config.TargetObject.transform);
            
            // GameObject active property
            var onCurve = new AnimationCurve(new Keyframe(0, 1));
            var offCurve = new AnimationCurve(new Keyframe(0, 0));
            
            onClip.SetCurve(targetPath, typeof(GameObject), "m_IsActive", onCurve);
            offClip.SetCurve(targetPath, typeof(GameObject), "m_IsActive", offCurve);
            
            // PhysBones
            foreach (var pb in config.PhysBonesToToggle)
            {
                if (pb == null) continue;
                string pbPath = GetRelativePath(avatar.transform, pb.transform);
                
                onClip.SetCurve(pbPath, pb.GetType(), "m_Enabled", onCurve);
                offClip.SetCurve(pbPath, pb.GetType(), "m_Enabled", offCurve);
            }
            
            // Blendshapes
            foreach (var bs in config.BlendshapesToAnimate)
            {
                if (bs.Renderer == null) continue;
                string rendererPath = GetRelativePath(avatar.transform, bs.Renderer.transform);
                string propertyName = $"blendShape.{bs.BlendshapeName}";
                
                // ON = blendshape at 100 (shrink body)
                // OFF = blendshape at 0 (show body)
                var bsOnCurve = new AnimationCurve(new Keyframe(0, 100));
                var bsOffCurve = new AnimationCurve(new Keyframe(0, 0));
                
                onClip.SetCurve(rendererPath, typeof(SkinnedMeshRenderer), propertyName, bsOnCurve);
                offClip.SetCurve(rendererPath, typeof(SkinnedMeshRenderer), propertyName, bsOffCurve);
            }
            
            // Save clips
            AssetDatabase.CreateAsset(onClip, $"{folder}/{baseName}_ON.anim");
            AssetDatabase.CreateAsset(offClip, $"{folder}/{baseName}_OFF.anim");
            
            return (onClip, offClip);
        }
        
        private static string GetRelativePath(Transform root, Transform target)
        {
            var path = new List<string>();
            var current = target;
            
            while (current != null && current != root)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }
            
            return string.Join("/", path);
        }
        
        private static void AddExpressionParameter(VRCAvatarDescriptor avatar, string paramName, bool saved, bool defaultValue)
        {
            var parameters = avatar.expressionParameters;
            if (parameters == null)
            {
                Debug.LogError("[OutfitWizard] Avatar has no Expression Parameters asset");
                return;
            }
            
            // Check if parameter already exists
            var existingParam = parameters.parameters.FirstOrDefault(p => p.name == paramName);
            if (existingParam != null)
            {
                Debug.Log($"[OutfitWizard] Parameter '{paramName}' already exists, updating...");
                existingParam.valueType = VRCExpressionParameters.ValueType.Bool;
                existingParam.saved = saved;
                existingParam.defaultValue = defaultValue ? 1 : 0;
            }
            else
            {
                // Add new parameter
                var paramList = parameters.parameters.ToList();
                paramList.Add(new VRCExpressionParameters.Parameter
                {
                    name = paramName,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    saved = saved,
                    defaultValue = defaultValue ? 1 : 0
                });
                parameters.parameters = paramList.ToArray();
            }
            
            EditorUtility.SetDirty(parameters);
        }
        
        private static void AddAnimatorLayer(VRCAvatarDescriptor avatar, string paramName, 
            AnimationClip onClip, AnimationClip offClip, bool defaultOn)
        {
            // Get FX layer
            var fxLayer = GetFXLayer(avatar);
            if (fxLayer == null)
            {
                Debug.LogError("[OutfitWizard] Could not find FX layer");
                return;
            }
            
            // Add parameter to animator if not exists
            if (!fxLayer.parameters.Any(p => p.name == paramName))
            {
                fxLayer.AddParameter(paramName, AnimatorControllerParameterType.Bool);
            }
            
            // Create layer
            string layerName = paramName.Replace("/", "_");
            
            // Check if layer already exists
            var existingLayer = fxLayer.layers.FirstOrDefault(l => l.name == layerName);
            if (existingLayer.stateMachine != null)
            {
                // Remove existing layer
                var layers = fxLayer.layers.ToList();
                layers.RemoveAll(l => l.name == layerName);
                fxLayer.layers = layers.ToArray();
            }
            
            // Create state machine
            var stateMachine = new AnimatorStateMachine
            {
                name = layerName,
                hideFlags = HideFlags.HideInHierarchy
            };
            
            AssetDatabase.AddObjectToAsset(stateMachine, fxLayer);
            
            // Create states
            var onState = stateMachine.AddState("ON", new Vector3(300, 50, 0));
            onState.motion = onClip;
            onState.writeDefaultValues = false;
            
            var offState = stateMachine.AddState("OFF", new Vector3(300, 150, 0));
            offState.motion = offClip;
            offState.writeDefaultValues = false;
            
            // Set default state based on defaultOn
            stateMachine.defaultState = defaultOn ? onState : offState;
            
            // Create transitions
            var toOn = offState.AddTransition(onState);
            toOn.hasExitTime = false;
            toOn.duration = 0;
            toOn.AddCondition(AnimatorConditionMode.If, 0, paramName);
            
            var toOff = onState.AddTransition(offState);
            toOff.hasExitTime = false;
            toOff.duration = 0;
            toOff.AddCondition(AnimatorConditionMode.IfNot, 0, paramName);
            
            // Add layer
            var layer = new AnimatorControllerLayer
            {
                name = layerName,
                stateMachine = stateMachine,
                defaultWeight = 1f
            };
            
            var allLayers = fxLayer.layers.ToList();
            allLayers.Add(layer);
            fxLayer.layers = allLayers.ToArray();
            
            EditorUtility.SetDirty(fxLayer);
        }
        
        private static AnimatorController GetFXLayer(VRCAvatarDescriptor avatar)
        {
            foreach (var layer in avatar.baseAnimationLayers)
            {
                if (layer.type == VRCAvatarDescriptor.AnimLayerType.FX)
                {
                    return layer.animatorController as AnimatorController;
                }
            }
            return null;
        }
        
        private static void AddMenuEntry(VRCAvatarDescriptor avatar, string menuPath, string toggleName, string paramName)
        {
            var rootMenu = avatar.expressionsMenu;
            if (rootMenu == null)
            {
                Debug.LogError("[OutfitWizard] Avatar has no Expression Menu");
                return;
            }
            
            // Find or create submenu
            VRCExpressionsMenu targetMenu = rootMenu;
            
            if (!string.IsNullOrEmpty(menuPath))
            {
                // Look for existing submenu
                var existingControl = rootMenu.controls.FirstOrDefault(c => 
                    c.name == menuPath && c.type == VRCExpressionsMenu.Control.ControlType.SubMenu);
                
                if (existingControl != null && existingControl.subMenu != null)
                {
                    targetMenu = existingControl.subMenu;
                }
                else
                {
                    // Create new submenu
                    var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    subMenu.name = menuPath;
                    
                    string assetPath = AssetDatabase.GetAssetPath(rootMenu);
                    string folder = Path.GetDirectoryName(assetPath);
                    AssetDatabase.CreateAsset(subMenu, $"{folder}/{menuPath}_Menu.asset");
                    
                    rootMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = menuPath,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = subMenu
                    });
                    
                    EditorUtility.SetDirty(rootMenu);
                    targetMenu = subMenu;
                }
            }
            
            // Check if toggle already exists
            var existingToggle = targetMenu.controls.FirstOrDefault(c => c.name == toggleName);
            if (existingToggle != null)
            {
                existingToggle.parameter = new VRCExpressionsMenu.Control.Parameter { name = paramName };
            }
            else
            {
                targetMenu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = toggleName,
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = paramName }
                });
            }
            
            EditorUtility.SetDirty(targetMenu);
        }
    }
}
