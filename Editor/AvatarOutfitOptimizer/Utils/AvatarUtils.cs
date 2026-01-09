using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace AvatarOutfitOptimizer.Utils
{
    /// <summary>
    /// Utility methods for working with VRChat avatars
    /// </summary>
    public static class AvatarUtils
    {
        /// <summary>
        /// Gets the VRC Avatar Descriptor from a GameObject or its children
        /// </summary>
        public static VRCAvatarDescriptor GetAvatarDescriptor(GameObject obj)
        {
            if (obj == null) return null;
            
            var descriptor = obj.GetComponent<VRCAvatarDescriptor>();
            if (descriptor != null) return descriptor;
            
            return obj.GetComponentInChildren<VRCAvatarDescriptor>();
        }

        /// <summary>
        /// Gets all active SkinnedMeshRenderers in the avatar hierarchy
        /// </summary>
        public static List<SkinnedMeshRenderer> GetActiveSkinnedMeshRenderers(GameObject avatarRoot)
        {
            var renderers = new List<SkinnedMeshRenderer>();
            if (avatarRoot == null) return renderers;
            
            var allRenderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in allRenderers)
            {
                if (renderer != null && renderer.gameObject.activeInHierarchy && renderer.enabled)
                {
                    renderers.Add(renderer);
                }
            }
            
            return renderers;
        }

        /// <summary>
        /// Gets all bones referenced by active SkinnedMeshRenderers
        /// </summary>
        public static HashSet<Transform> GetUsedBones(GameObject avatarRoot)
        {
            var usedBones = new HashSet<Transform>();
            var renderers = GetActiveSkinnedMeshRenderers(avatarRoot);
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh != null && renderer.bones != null)
                {
                    foreach (var bone in renderer.bones)
                    {
                        if (bone != null)
                        {
                            usedBones.Add(bone);
                        }
                    }
                }
            }
            
            return usedBones;
        }

        /// <summary>
        /// Gets all active GameObjects in the hierarchy (recursive)
        /// </summary>
        public static List<string> GetActiveGameObjectPaths(GameObject root)
        {
            var paths = new List<string>();
            if (root == null) return paths;
            
            CollectActivePaths(root, root.transform, paths);
            return paths;
        }

        private static void CollectActivePaths(GameObject root, Transform current, List<string> paths)
        {
            if (current == null) return;
            
            if (current.gameObject.activeInHierarchy)
            {
                string path = GetRelativePath(root.transform, current);
                paths.Add(path);
                
                foreach (Transform child in current)
                {
                    CollectActivePaths(root, child, paths);
                }
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == root) return target.name;
            
            var path = new System.Text.StringBuilder();
            var current = target;
            var segments = new List<string>();
            
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }
            
            segments.Reverse();
            return string.Join("/", segments);
        }

        /// <summary>
        /// Gets Expression Parameters component from avatar
        /// </summary>
        public static VRCExpressionParameters GetExpressionParameters(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null) return null;
            return descriptor.expressionParameters;
        }

        /// <summary>
        /// Gets Expression Menu from avatar
        /// </summary>
        public static VRCExpressionsMenu GetExpressionMenu(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null) return null;
            return descriptor.expressionsMenu;
        }

        /// <summary>
        /// Gets FX Layer AnimatorController from avatar
        /// </summary>
        public static RuntimeAnimatorController GetFXLayer(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null) return null;
            
            foreach (var layer in descriptor.baseAnimationLayers)
            {
                if (layer.type == VRCAvatarDescriptor.AnimLayerType.FX)
                {
                    return layer.animatorController;
                }
            }
            return null;
        }
    }
}

