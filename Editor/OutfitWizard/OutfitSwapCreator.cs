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
    /// Creates outfit swap systems with Int-based parameters
    /// Only one outfit can be active at a time
    /// </summary>
    public static class OutfitSwapCreator
    {
        public class OutfitSwapConfig
        {
            public string MenuName = "Outfits";
            public OutfitSetWizard.MenuStyle MenuStyle;
            public int DefaultOutfitIndex = 0;
            public bool Saved = true;
            public List<OutfitSetWizard.OutfitDefinition> Outfits;
            public List<GameObject> SharedItems;
            public Dictionary<int, List<OutfitSetWizard.PhysBoneEntry>> OutfitPhysBones;
            public Dictionary<int, List<OutfitSetWizard.BlendshapeEntry>> OutfitBlendshapes;
        }
        
        public static void CreateOutfitSwap(VRCAvatarDescriptor avatar, OutfitSwapConfig config)
        {
            if (avatar == null || config.Outfits == null || config.Outfits.Count < 2)
            {
                Debug.LogError("[OutfitWizard] Invalid configuration for outfit swap");
                return;
            }
            
            string parameterName = SanitizeParameterName($"Outfit/{config.MenuName}");
            string assetFolder = GetOrCreateAssetFolder(avatar);
            
            // 1. Create animation clips for each outfit
            var clips = CreateOutfitAnimationClips(avatar, config, assetFolder);
            
            // 2. Add Int parameter
            AddExpressionParameter(avatar, parameterName, config.Saved, config.DefaultOutfitIndex, config.Outfits.Count);
            
            // 3. Create animator layer with states for each outfit
            AddAnimatorLayer(avatar, parameterName, clips, config);
            
            // 4. Create menu
            CreateMenu(avatar, config, parameterName);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[OutfitWizard] Created outfit swap '{config.MenuName}' with {config.Outfits.Count} outfits!");
        }
        
        private static string SanitizeParameterName(string name)
        {
            return name.Replace(" ", "_").Replace("-", "_");
        }
        
        private static string GetOrCreateAssetFolder(VRCAvatarDescriptor avatar)
        {
            string folder = "Assets/OutfitWizard/Generated";
            
            if (!AssetDatabase.IsValidFolder("Assets/OutfitWizard"))
            {
                AssetDatabase.CreateFolder("Assets", "OutfitWizard");
            }
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/OutfitWizard", "Generated");
            }
            
            return folder;
        }
        
        private static List<AnimationClip> CreateOutfitAnimationClips(
            VRCAvatarDescriptor avatar, OutfitSwapConfig config, string folder)
        {
            var clips = new List<AnimationClip>();
            
            // Collect ALL outfit items (to know what to turn off)
            var allOutfitItems = new HashSet<GameObject>();
            foreach (var outfit in config.Outfits)
            {
                foreach (var item in outfit.Items)
                {
                    if (item != null) allOutfitItems.Add(item);
                }
            }
            
            // Collect ALL physbones
            var allPhysBones = new HashSet<Component>();
            foreach (var kvp in config.OutfitPhysBones)
            {
                foreach (var entry in kvp.Value)
                {
                    if (entry.Include && entry.PhysBone != null)
                        allPhysBones.Add(entry.PhysBone);
                }
            }
            
            for (int i = 0; i < config.Outfits.Count; i++)
            {
                var outfit = config.Outfits[i];
                var clip = new AnimationClip { name = $"Outfit_{SanitizeParameterName(outfit.Name)}" };
                
                var onCurve = new AnimationCurve(new Keyframe(0, 1));
                var offCurve = new AnimationCurve(new Keyframe(0, 0));
                var blendOn = new AnimationCurve(new Keyframe(0, 100));
                var blendOff = new AnimationCurve(new Keyframe(0, 0));
                
                // Turn ON this outfit's items
                foreach (var item in outfit.Items)
                {
                    if (item == null) continue;
                    string path = GetRelativePath(avatar.transform, item.transform);
                    clip.SetCurve(path, typeof(GameObject), "m_IsActive", onCurve);
                }
                
                // Turn OFF other outfit items
                foreach (var item in allOutfitItems)
                {
                    if (!outfit.Items.Contains(item))
                    {
                        string path = GetRelativePath(avatar.transform, item.transform);
                        clip.SetCurve(path, typeof(GameObject), "m_IsActive", offCurve);
                    }
                }
                
                // PhysBones for this outfit ON
                if (config.OutfitPhysBones.TryGetValue(i, out var pbEntries))
                {
                    foreach (var entry in pbEntries)
                    {
                        if (!entry.Include || entry.PhysBone == null) continue;
                        string path = GetRelativePath(avatar.transform, entry.PhysBone.transform);
                        clip.SetCurve(path, entry.PhysBone.GetType(), "m_Enabled", onCurve);
                    }
                }
                
                // PhysBones for other outfits OFF
                foreach (var pb in allPhysBones)
                {
                    bool isThisOutfit = pbEntries?.Any(e => e.PhysBone == pb && e.Include) ?? false;
                    if (!isThisOutfit)
                    {
                        string path = GetRelativePath(avatar.transform, pb.transform);
                        clip.SetCurve(path, pb.GetType(), "m_Enabled", offCurve);
                    }
                }
                
                // Blendshapes for this outfit ON
                if (config.OutfitBlendshapes.TryGetValue(i, out var bsEntries))
                {
                    foreach (var entry in bsEntries)
                    {
                        if (!entry.Include || entry.Renderer == null) continue;
                        string path = GetRelativePath(avatar.transform, entry.Renderer.transform);
                        string propName = $"blendShape.{entry.BlendshapeName}";
                        clip.SetCurve(path, typeof(SkinnedMeshRenderer), propName, blendOn);
                    }
                }
                
                // Blendshapes for other outfits OFF
                for (int j = 0; j < config.Outfits.Count; j++)
                {
                    if (j == i) continue;
                    if (config.OutfitBlendshapes.TryGetValue(j, out var otherBsEntries))
                    {
                        foreach (var entry in otherBsEntries)
                        {
                            if (!entry.Include || entry.Renderer == null) continue;
                            string path = GetRelativePath(avatar.transform, entry.Renderer.transform);
                            string propName = $"blendShape.{entry.BlendshapeName}";
                            clip.SetCurve(path, typeof(SkinnedMeshRenderer), propName, blendOff);
                        }
                    }
                }
                
                // Save clip
                AssetDatabase.CreateAsset(clip, $"{folder}/Outfit_{SanitizeParameterName(outfit.Name)}.anim");
                clips.Add(clip);
            }
            
            return clips;
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
        
        private static void AddExpressionParameter(VRCAvatarDescriptor avatar, string paramName, 
            bool saved, int defaultValue, int outfitCount)
        {
            var parameters = avatar.expressionParameters;
            if (parameters == null)
            {
                Debug.LogError("[OutfitWizard] Avatar has no Expression Parameters asset");
                return;
            }
            
            var existingParam = parameters.parameters.FirstOrDefault(p => p.name == paramName);
            if (existingParam != null)
            {
                existingParam.valueType = VRCExpressionParameters.ValueType.Int;
                existingParam.saved = saved;
                existingParam.defaultValue = defaultValue;
            }
            else
            {
                var paramList = parameters.parameters.ToList();
                paramList.Add(new VRCExpressionParameters.Parameter
                {
                    name = paramName,
                    valueType = VRCExpressionParameters.ValueType.Int,
                    saved = saved,
                    defaultValue = defaultValue
                });
                parameters.parameters = paramList.ToArray();
            }
            
            EditorUtility.SetDirty(parameters);
        }
        
        private static void AddAnimatorLayer(VRCAvatarDescriptor avatar, string paramName, 
            List<AnimationClip> clips, OutfitSwapConfig config)
        {
            var fxLayer = GetFXLayer(avatar);
            if (fxLayer == null)
            {
                Debug.LogError("[OutfitWizard] Could not find FX layer");
                return;
            }
            
            // Add parameter
            if (!fxLayer.parameters.Any(p => p.name == paramName))
            {
                fxLayer.AddParameter(paramName, AnimatorControllerParameterType.Int);
            }
            
            string layerName = SanitizeParameterName(config.MenuName) + "_Swap";
            
            // Remove existing layer if exists
            var layers = fxLayer.layers.ToList();
            layers.RemoveAll(l => l.name == layerName);
            fxLayer.layers = layers.ToArray();
            
            // Create state machine
            var stateMachine = new AnimatorStateMachine
            {
                name = layerName,
                hideFlags = HideFlags.HideInHierarchy
            };
            
            AssetDatabase.AddObjectToAsset(stateMachine, fxLayer);
            
            // Create states
            var states = new List<AnimatorState>();
            for (int i = 0; i < config.Outfits.Count; i++)
            {
                var outfit = config.Outfits[i];
                var state = stateMachine.AddState(outfit.Name, new Vector3(300, 50 + i * 80, 0));
                state.motion = clips[i];
                state.writeDefaultValues = false;
                states.Add(state);
                
                if (i == config.DefaultOutfitIndex)
                {
                    stateMachine.defaultState = state;
                }
            }
            
            // Create transitions between all states
            for (int i = 0; i < states.Count; i++)
            {
                for (int j = 0; j < states.Count; j++)
                {
                    if (i == j) continue;
                    
                    var transition = states[i].AddTransition(states[j]);
                    transition.hasExitTime = false;
                    transition.duration = 0;
                    transition.AddCondition(AnimatorConditionMode.Equals, j, paramName);
                }
            }
            
            // Add layer
            var layer = new AnimatorControllerLayer
            {
                name = layerName,
                stateMachine = stateMachine,
                defaultWeight = 1f
            };
            
            layers = fxLayer.layers.ToList();
            layers.Add(layer);
            fxLayer.layers = layers.ToArray();
            
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
        
        private static void CreateMenu(VRCAvatarDescriptor avatar, OutfitSwapConfig config, string paramName)
        {
            var rootMenu = avatar.expressionsMenu;
            if (rootMenu == null)
            {
                Debug.LogError("[OutfitWizard] Avatar has no Expression Menu");
                return;
            }
            
            string assetPath = AssetDatabase.GetAssetPath(rootMenu);
            string folder = Path.GetDirectoryName(assetPath);
            
            // Create submenu for outfits
            var outfitMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            outfitMenu.name = config.MenuName;
            
            if (config.MenuStyle == OutfitSetWizard.MenuStyle.RadialPuppet)
            {
                // Create radial puppet control
                outfitMenu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "Select Outfit",
                    type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                    subParameters = new VRCExpressionsMenu.Control.Parameter[]
                    {
                        new VRCExpressionsMenu.Control.Parameter { name = paramName }
                    }
                });
                
                // Add labels for each outfit
                for (int i = 0; i < config.Outfits.Count; i++)
                {
                    outfitMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = config.Outfits[i].Name,
                        type = VRCExpressionsMenu.Control.ControlType.Button,
                        parameter = new VRCExpressionsMenu.Control.Parameter { name = paramName },
                        value = i
                    });
                }
            }
            else // SubmenuToggles
            {
                // Create button for each outfit
                for (int i = 0; i < config.Outfits.Count; i++)
                {
                    outfitMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = config.Outfits[i].Name,
                        type = VRCExpressionsMenu.Control.ControlType.Button,
                        parameter = new VRCExpressionsMenu.Control.Parameter { name = paramName },
                        value = i
                    });
                }
            }
            
            AssetDatabase.CreateAsset(outfitMenu, $"{folder}/{config.MenuName}_Menu.asset");
            
            // Add to root menu
            var existingEntry = rootMenu.controls.FirstOrDefault(c => c.name == config.MenuName);
            if (existingEntry != null)
            {
                existingEntry.subMenu = outfitMenu;
            }
            else
            {
                rootMenu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = config.MenuName,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = outfitMenu
                });
            }
            
            EditorUtility.SetDirty(rootMenu);
            EditorUtility.SetDirty(outfitMenu);
        }
    }
}
