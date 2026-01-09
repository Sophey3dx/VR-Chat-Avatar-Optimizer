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
        /// Gets SkinnedMeshRenderers in the avatar hierarchy
        /// </summary>
        /// <param name="avatarRoot">Root GameObject of the avatar</param>
        /// <param name="includeInactive">If true, includes all renderers. If false, only active and enabled renderers.</param>
        public static List<SkinnedMeshRenderer> GetSkinnedMeshRenderers(GameObject avatarRoot, bool includeInactive = false)
        {
            var renderers = new List<SkinnedMeshRenderer>();
            if (avatarRoot == null) return renderers;
            
            var allRenderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in allRenderers)
            {
                if (renderer == null) continue;
                
                if (includeInactive)
                {
                    // Include all renderers
                    renderers.Add(renderer);
                }
                else
                {
                    // Only include active and enabled renderers
                    if (renderer.gameObject.activeInHierarchy && renderer.enabled)
                    {
                        renderers.Add(renderer);
                    }
                }
            }
            
            return renderers;
        }

        /// <summary>
        /// Gets all active SkinnedMeshRenderers in the avatar hierarchy (convenience method)
        /// </summary>
        public static List<SkinnedMeshRenderer> GetActiveSkinnedMeshRenderers(GameObject avatarRoot)
        {
            return GetSkinnedMeshRenderers(avatarRoot, includeInactive: false);
        }

        /// <summary>
        /// Gets bones referenced by SkinnedMeshRenderers
        /// </summary>
        /// <param name="avatarRoot">Root GameObject of the avatar</param>
        /// <param name="includeInactive">If true, includes bones from all renderers. If false, only from active renderers.</param>
        public static HashSet<Transform> GetUsedBones(GameObject avatarRoot, bool includeInactive = false)
        {
            var usedBones = new HashSet<Transform>();
            var renderers = GetSkinnedMeshRenderers(avatarRoot, includeInactive);
            
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
        /// Gets GameObjects paths in the hierarchy
        /// </summary>
        /// <param name="root">Root GameObject</param>
        /// <param name="includeInactive">If true, includes all GameObjects. If false, only active ones.</param>
        public static List<string> GetGameObjectPaths(GameObject root, bool includeInactive = false)
        {
            var paths = new List<string>();
            if (root == null) return paths;
            
            if (includeInactive)
            {
                CollectAllPaths(root, root.transform, paths);
            }
            else
            {
                CollectActivePaths(root, root.transform, paths);
            }
            return paths;
        }

        /// <summary>
        /// Gets all active GameObjects in the hierarchy (recursive) - convenience method
        /// </summary>
        public static List<string> GetActiveGameObjectPaths(GameObject root)
        {
            return GetGameObjectPaths(root, includeInactive: false);
        }

        /// <summary>
        /// Gets ALL GameObjects in the hierarchy (including inactive) - convenience method
        /// </summary>
        public static List<string> GetAllGameObjectPaths(GameObject root)
        {
            return GetGameObjectPaths(root, includeInactive: true);
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

        private static void CollectAllPaths(GameObject root, Transform current, List<string> paths)
        {
            if (current == null) return;
            
            string path = GetRelativePath(root.transform, current);
            paths.Add(path);
            
            foreach (Transform child in current)
            {
                CollectAllPaths(root, child, paths);
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

