using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AvatarOutfitOptimizer
{
    /// <summary>
    /// Bridge between Unity and Blender for avatar optimization
    /// Exports active/inactive object information as JSON for Blender to read
    /// </summary>
    public static class BlenderBridge
    {
        private const string BRIDGE_FILENAME = "vrchat_optimizer_bridge.json";
        
        /// <summary>
        /// Data structure for bridge communication
        /// </summary>
        [Serializable]
        public class BridgeData
        {
            public string avatarName;
            public string timestamp;
            public string unityVersion;
            public List<string> keepObjects = new List<string>();
            public List<string> removeObjects = new List<string>();
            public OptimizationSettings settings = new OptimizationSettings();
            public AvatarStats beforeStats = new AvatarStats();
        }
        
        [Serializable]
        public class OptimizationSettings
        {
            public int targetTriangles = 70000;
            public int targetBones = 400;
            public int targetPhysBones = 32;
            public int targetPhysBoneTransforms = 256;
            public int maxTextureSize = 2048;
        }
        
        [Serializable]
        public class AvatarStats
        {
            public int triangles;
            public int bones;
            public int physBones;
            public int physBoneTransforms;
            public int materials;
            public int meshes;
        }
        
        /// <summary>
        /// Gets the default bridge folder path
        /// </summary>
        public static string GetDefaultBridgePath()
        {
            // Use Documents folder for cross-application access
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "VRChatAvatarOptimizer");
        }
        
        /// <summary>
        /// Exports bridge data to JSON file
        /// </summary>
        public static void ExportBridgeData(
            GameObject avatar,
            AvatarSnapshot fullSnapshot,
            AvatarSnapshot activeSnapshot,
            string customPath = null)
        {
            if (avatar == null || fullSnapshot == null || activeSnapshot == null)
            {
                Debug.LogError("[BlenderBridge] Cannot export: missing avatar or snapshots");
                return;
            }
            
            // Create bridge data
            var data = new BridgeData
            {
                avatarName = avatar.name,
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                unityVersion = Application.unityVersion
            };
            
            // Get all object paths
            var allPaths = fullSnapshot.ActiveGameObjectPaths;
            var activePaths = new HashSet<string>(activeSnapshot.ActiveGameObjectPaths);
            
            // Separate into keep/remove lists
            foreach (var path in allPaths)
            {
                // Extract object name from path
                string objectName = path.Contains("/") 
                    ? path.Substring(path.LastIndexOf('/') + 1) 
                    : path;
                
                if (activePaths.Contains(path))
                {
                    if (!data.keepObjects.Contains(objectName))
                        data.keepObjects.Add(objectName);
                }
                else
                {
                    if (!data.removeObjects.Contains(objectName))
                        data.removeObjects.Add(objectName);
                }
            }
            
            // Fill stats
            data.beforeStats = new AvatarStats
            {
                triangles = fullSnapshot.TriangleCount,
                bones = fullSnapshot.BoneCount,
                physBones = fullSnapshot.PhysBoneCount,
                physBoneTransforms = fullSnapshot.PhysBoneTransformCount,
                materials = fullSnapshot.MaterialCount,
                meshes = fullSnapshot.MeshCount
            };
            
            // Set target values based on VRChat limits
            data.settings = new OptimizationSettings
            {
                targetTriangles = VRChatLimits.MaxTriangles,
                targetBones = VRChatLimits.MaxBones,
                targetPhysBones = VRChatLimits.MaxPhysBones,
                targetPhysBoneTransforms = VRChatLimits.MaxPhysBoneTransforms,
                maxTextureSize = 2048
            };
            
            // Determine output path
            string bridgePath = customPath ?? GetDefaultBridgePath();
            
            // Create directory if needed
            if (!Directory.Exists(bridgePath))
            {
                Directory.CreateDirectory(bridgePath);
            }
            
            // Write JSON
            string filePath = Path.Combine(bridgePath, BRIDGE_FILENAME);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            
            Debug.Log($"[BlenderBridge] Exported bridge data to: {filePath}");
            Debug.Log($"[BlenderBridge] Keep: {data.keepObjects.Count} objects, Remove: {data.removeObjects.Count} objects");
            
            // Show confirmation
            EditorUtility.DisplayDialog(
                "Bridge Export Complete",
                $"Bridge data exported to:\n{filePath}\n\n" +
                $"Keep: {data.keepObjects.Count} objects\n" +
                $"Remove: {data.removeObjects.Count} objects\n\n" +
                "Blender will automatically detect this file.",
                "OK");
        }
        
        /// <summary>
        /// Opens the bridge folder in file explorer
        /// </summary>
        public static void OpenBridgeFolder()
        {
            string path = GetDefaultBridgePath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            EditorUtility.RevealInFinder(path);
        }
    }
}
