# VRChat Avatar Optimizer

A production-ready Unity Editor Tool for VRChat avatar creators that automatically generates optimized avatar variants based on the currently active outfit state.

## Features

- **Safe Optimization**: Never modifies the original avatar - always creates duplicates
- **Dry Run Mode**: Preview changes before applying them
- **Comprehensive Analysis**: Scans AnimatorControllers, Expression Menus, PhysBones, and more
- **Performance Estimation**: Provides estimated performance tier with VRChat SDK limits
- **Blender Bridge**: Export selection to Blender 5.0 for advanced optimization
- **Advanced Options**: Aggressive cleanup modes for experienced users
- **Individual Cleanup Areas**: Choose which areas to optimize

## Installation

### Method 1: VCC Repository (One-Click)

1. Open VCC (VRChat Creator Companion)
2. Go to **Settings** → **Repositories**
3. Click **"Add Repository"**
4. Enter: `https://raw.githubusercontent.com/Sophey3dx/VR-Chat-Avatar-Optimizer/main/index.json`
5. The package will appear in your Packages list

### Method 2: Unity Package Manager

1. Open your VRChat project in Unity (via VCC)
2. Go to: `Window > Package Manager`
3. Click **"+"** → **"Add package from git URL..."**
4. Enter: `https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git`
5. Click **"Add"**

## Usage

### Basic Workflow
1. Open the tool: `Tools > VRChat Avatar Optimizer`
2. Select your avatar GameObject (must have VRC Avatar Descriptor)
3. **Enable only the objects you want to KEEP** in the hierarchy
4. Click "Capture Snapshot" to analyze current state
5. Review the analysis in the "Analysis" tab
6. Select which cleanup areas to apply in the "Optimize" tab
7. Enable "Dry Run" to preview, or click "Create Optimized Avatar" to apply

### Blender Bridge (Advanced)
1. After capturing a snapshot, click **"Export to Blender"**
2. Open your avatar in Blender 5.0
3. Install the Blender plugin from `BlenderPlugin/vrchat_avatar_optimizer.py`
4. The plugin will automatically detect which objects to keep/remove
5. Use Blender tools to decimate meshes, remove unused bones, resize textures
6. Export back to Unity for final optimization

## Important Notes

- This tool estimates performance tiers - it does NOT guarantee a specific VRChat rank
- Always test optimized avatars before uploading
- Use aggressive cleanup options at your own risk
- The tool creates duplicates - your original avatar remains untouched

## Requirements

- Unity 2022.3
- VRChat SDK 3 (Avatars) >= 3.4.0
- (Optional) Blender 5.0 for advanced optimization
