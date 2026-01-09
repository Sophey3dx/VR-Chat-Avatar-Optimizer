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
    /// Serializable snapshot of avatar state for comparison and optimization
    /// </summary>
    [Serializable]
    public class AvatarSnapshot
    {
        [SerializeField] private List<string> activeGameObjectPaths = new List<string>();
        [SerializeField] private List<string> activeRendererPaths = new List<string>();
        [SerializeField] private List<string> usedBonePaths = new List<string>();
        [SerializeField] private List<string> activePhysBonePaths = new List<string>();
        [SerializeField] private List<string> expressionParameterNames = new List<string>();
        [SerializeField] private List<string> animatorParameterNames = new List<string>();
        [SerializeField] private string avatarFingerprint = "";
        [SerializeField] private int meshCount;
        [SerializeField] private int materialCount;
        [SerializeField] private int boneCount;
        [SerializeField] private int physBoneCount;
        [SerializeField] private int parameterCount;

        // Default constructor for serialization
        public AvatarSnapshot() { }

        public List<string> ActiveGameObjectPaths => activeGameObjectPaths ?? new List<string>();
        public List<string> ActiveRendererPaths => activeRendererPaths ?? new List<string>();
        public List<string> UsedBonePaths => usedBonePaths ?? new List<string>();
        public List<string> ActivePhysBonePaths => activePhysBonePaths ?? new List<string>();
        public List<string> ExpressionParameterNames => expressionParameterNames ?? new List<string>();
        public List<string> AnimatorParameterNames => animatorParameterNames ?? new List<string>();
        public string AvatarFingerprint => avatarFingerprint ?? "";
        public int MeshCount => meshCount;
        public int MaterialCount => materialCount;
        public int BoneCount => boneCount;
        public int PhysBoneCount => physBoneCount;
        public int ParameterCount => parameterCount;

        /// <summary>
        /// Creates a snapshot from the current avatar state
        /// </summary>
        public static AvatarSnapshot Capture(GameObject avatarRoot)
        {
            if (avatarRoot == null)
            {
                Debug.LogError("[AvatarOptimizer] Cannot capture snapshot: avatar root is null");
                return null;
            }

            var descriptor = AvatarUtils.GetAvatarDescriptor(avatarRoot);
            if (descriptor == null)
            {
                Debug.LogError("[AvatarOptimizer] Cannot capture snapshot: no VRC Avatar Descriptor found");
                return null;
            }

            var snapshot = new AvatarSnapshot();

            try
            {
                // Capture active GameObjects
                snapshot.activeGameObjectPaths = AvatarUtils.GetActiveGameObjectPaths(avatarRoot);

                // Capture active SkinnedMeshRenderers
                var activeRenderers = AvatarUtils.GetActiveSkinnedMeshRenderers(avatarRoot);
                snapshot.activeRendererPaths = activeRenderers
                    .Select(r => GetGameObjectPath(avatarRoot.transform, r.transform))
                    .ToList();

                // Capture used bones
                var usedBones = AvatarUtils.GetUsedBones(avatarRoot);
                snapshot.usedBonePaths = usedBones
                    .Select(b => GetGameObjectPath(avatarRoot.transform, b))
                    .ToList();

                // Capture PhysBones (VRC SDK 3)
                snapshot.activePhysBonePaths = CapturePhysBones(avatarRoot, avatarRoot.transform);

                // Capture Expression Parameters
                var expressionParams = AvatarUtils.GetExpressionParameters(descriptor);
                if (expressionParams != null && expressionParams.parameters != null)
                {
                    snapshot.expressionParameterNames = expressionParams.parameters
                        .Where(p => p != null)
                        .Select(p => p.name)
                        .ToList();
                }

                // Capture Animator Parameters from FX Layer
                var fxLayer = AvatarUtils.GetFXLayer(descriptor);
                if (fxLayer != null)
                {
                    snapshot.animatorParameterNames = CaptureAnimatorParameters(fxLayer);
                }

                // Generate avatar fingerprint
                var materials = SerializationHelper.GetMaterialsFromRenderers(activeRenderers);
                snapshot.avatarFingerprint = SerializationHelper.GenerateAvatarFingerprint(
                    activeRenderers,
                    usedBones,
                    materials
                );

                // Capture counts
                snapshot.meshCount = activeRenderers.Count;
                snapshot.materialCount = materials.Count;
                snapshot.boneCount = usedBones.Count;
                snapshot.physBoneCount = snapshot.activePhysBonePaths.Count;
                snapshot.parameterCount = snapshot.expressionParameterNames.Count + snapshot.animatorParameterNames.Count;

                Debug.Log($"[AvatarOptimizer] Snapshot captured: {snapshot.meshCount} meshes, {snapshot.materialCount} materials, {snapshot.boneCount} bones");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarOptimizer] Error capturing snapshot: {e.Message}");
                return null;
            }

            return snapshot;
        }

        /// <summary>
        /// Creates a simulated snapshot with estimated values for dry run mode (simple version)
        /// </summary>
        public static AvatarSnapshot CreateSimulated(
            int meshCount,
            int materialCount,
            int boneCount,
            int physBoneCount,
            int parameterCount,
            string fingerprint)
        {
            var snapshot = new AvatarSnapshot();
            snapshot.meshCount = Math.Max(0, meshCount);
            snapshot.materialCount = Math.Max(0, materialCount);
            snapshot.boneCount = Math.Max(0, boneCount);
            snapshot.physBoneCount = Math.Max(0, physBoneCount);
            snapshot.parameterCount = Math.Max(0, parameterCount);
            snapshot.avatarFingerprint = fingerprint + "_simulated";
            return snapshot;
        }

        /// <summary>
        /// Creates a simulated snapshot with full path lists for accurate comparison
        /// </summary>
        public static AvatarSnapshot CreateSimulatedWithPaths(
            List<string> remainingGameObjectPaths,
            List<string> remainingRendererPaths,
            List<string> remainingBonePaths,
            List<string> remainingPhysBonePaths,
            List<string> remainingExpressionParams,
            List<string> remainingAnimatorParams,
            int materialCount,
            string fingerprint)
        {
            var snapshot = new AvatarSnapshot();
            
            // Copy lists to avoid reference issues
            snapshot.activeGameObjectPaths = remainingGameObjectPaths != null 
                ? new List<string>(remainingGameObjectPaths) 
                : new List<string>();
            snapshot.activeRendererPaths = remainingRendererPaths != null 
                ? new List<string>(remainingRendererPaths) 
                : new List<string>();
            snapshot.usedBonePaths = remainingBonePaths != null 
                ? new List<string>(remainingBonePaths) 
                : new List<string>();
            snapshot.activePhysBonePaths = remainingPhysBonePaths != null 
                ? new List<string>(remainingPhysBonePaths) 
                : new List<string>();
            snapshot.expressionParameterNames = remainingExpressionParams != null 
                ? new List<string>(remainingExpressionParams) 
                : new List<string>();
            snapshot.animatorParameterNames = remainingAnimatorParams != null 
                ? new List<string>(remainingAnimatorParams) 
                : new List<string>();
            
            // Calculate counts from lists
            snapshot.meshCount = snapshot.activeRendererPaths.Count;
            snapshot.materialCount = Math.Max(0, materialCount);
            snapshot.boneCount = snapshot.usedBonePaths.Count;
            snapshot.physBoneCount = snapshot.activePhysBonePaths.Count;
            snapshot.parameterCount = snapshot.expressionParameterNames.Count + snapshot.animatorParameterNames.Count;
            snapshot.avatarFingerprint = fingerprint + "_simulated";
            
            return snapshot;
        }

        /// <summary>
        /// Compares this snapshot with another and returns differences
        /// </summary>
        public SnapshotComparison Compare(AvatarSnapshot other)
        {
            if (other == null) return null;

            return new SnapshotComparison
            {
                Before = this,
                After = other,
                RemovedGameObjects = activeGameObjectPaths.Except(other.activeGameObjectPaths).ToList(),
                AddedGameObjects = other.activeGameObjectPaths.Except(activeGameObjectPaths).ToList(),
                RemovedRenderers = activeRendererPaths.Except(other.activeRendererPaths).ToList(),
                AddedRenderers = other.activeRendererPaths.Except(activeRendererPaths).ToList(),
                RemovedBones = usedBonePaths.Except(other.usedBonePaths).ToList(),
                AddedBones = other.usedBonePaths.Except(usedBonePaths).ToList(),
                RemovedPhysBones = activePhysBonePaths.Except(other.activePhysBonePaths).ToList(),
                AddedPhysBones = other.activePhysBonePaths.Except(activePhysBonePaths).ToList(),
                RemovedParameters = expressionParameterNames.Except(other.expressionParameterNames)
                    .Concat(animatorParameterNames.Except(other.animatorParameterNames))
                    .ToList(),
                AddedParameters = other.expressionParameterNames.Except(expressionParameterNames)
                    .Concat(other.animatorParameterNames.Except(animatorParameterNames))
                    .ToList()
            };
        }

        private static string GetGameObjectPath(Transform root, Transform target)
        {
            if (target == null) return "";
            if (target == root) return root.name;

            var path = new List<string>();
            var current = target;

            while (current != null && current != root)
            {
                path.Add(current.name);
                current = current.parent;
            }

            path.Reverse();
            return string.Join("/", path);
        }

        private static List<string> CapturePhysBones(GameObject avatarRoot, Transform root)
        {
            var physBonePaths = new List<string>();
            
            // VRC SDK 3 PhysBone component type
            var physBoneType = Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3.Dynamics.PhysBone");
            if (physBoneType == null)
            {
                // Fallback: try alternative namespace
                physBoneType = Type.GetType("VRC.Dynamics.VRCPhysBone, VRC.SDK3.Dynamics");
            }

            if (physBoneType != null)
            {
                var allPhysBones = avatarRoot.GetComponentsInChildren(physBoneType, true);
                foreach (var physBone in allPhysBones)
                {
                    if (physBone != null)
                    {
                        var component = physBone as Component;
                        if (component != null && component.gameObject.activeInHierarchy)
                        {
                            string path = GetGameObjectPath(root, component.transform);
                            physBonePaths.Add(path);
                        }
                    }
                }
            }

            return physBonePaths;
        }

        private static List<string> CaptureAnimatorParameters(RuntimeAnimatorController controller)
        {
            var parameters = new List<string>();
            
            if (controller is AnimatorController animatorController)
            {
                if (animatorController.parameters != null)
                {
                    foreach (var param in animatorController.parameters)
                    {
                        if (param != null && !string.IsNullOrEmpty(param.name))
                        {
                            parameters.Add(param.name);
                        }
                    }
                }
            }
            else
            {
                // Fallback for other controller types
                // Try to get parameters via reflection if needed
                try
                {
                    var parametersProperty = controller.GetType().GetProperty("parameters");
                    if (parametersProperty != null)
                    {
                        var paramsArray = parametersProperty.GetValue(controller) as System.Array;
                        if (paramsArray != null)
                        {
                            foreach (var param in paramsArray)
                            {
                                var nameProperty = param.GetType().GetProperty("name");
                                if (nameProperty != null)
                                {
                                    string name = nameProperty.GetValue(param) as string;
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        parameters.Add(name);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Reflection failed - that's okay
                }
            }

            return parameters;
        }
    }

    /// <summary>
    /// Comparison result between two snapshots
    /// </summary>
    [Serializable]
    public class SnapshotComparison
    {
        public AvatarSnapshot Before;
        public AvatarSnapshot After;
        public List<string> RemovedGameObjects = new List<string>();
        public List<string> AddedGameObjects = new List<string>();
        public List<string> RemovedRenderers = new List<string>();
        public List<string> AddedRenderers = new List<string>();
        public List<string> RemovedBones = new List<string>();
        public List<string> AddedBones = new List<string>();
        public List<string> RemovedPhysBones = new List<string>();
        public List<string> AddedPhysBones = new List<string>();
        public List<string> RemovedParameters = new List<string>();
        public List<string> AddedParameters = new List<string>();
    }
}

