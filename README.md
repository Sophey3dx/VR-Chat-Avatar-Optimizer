# VRChat Avatar Optimizer

A production-ready Unity Editor Tool for VRChat avatar creators that automatically generates optimized avatar variants based on the currently active outfit state.

## Features

- **Safe Optimization**: Never modifies the original avatar - always creates duplicates
- **Dry Run Mode**: Preview changes before applying them
- **Comprehensive Analysis**: Scans AnimatorControllers, Expression Menus, PhysBones, and more
- **Performance Estimation**: Provides estimated performance tier (not guaranteed rank)
- **Advanced Options**: Aggressive cleanup modes for experienced users
- **Individual Cleanup Areas**: Choose which areas to optimize

## Installation

### From GitHub (Recommended)

1. Open your VRChat project in Unity (via VCC)
2. Go to: `Window > Package Manager`
3. Click **"+"** → **"Add package from git URL..."**
4. Enter: `https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git`
5. Click **"Add"**

### Alternative: manifest.json

Add to your `Packages/manifest.json`:
```json
"com.vrchat.avatar-optimizer": "https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git"
```

## Usage

1. Open the tool: `Window > VRChat Avatar Optimizer`
2. Select your avatar GameObject (must have VRC Avatar Descriptor)
3. Click "Capture Snapshot" to analyze current state
4. Review the analysis in the "Analysis" tab
5. Select which cleanup areas to apply in the "Optimize" tab
6. Enable "Dry Run" to preview, or click "Create Optimized Avatar" to apply

## Important Notes

- This tool estimates performance tiers - it does NOT guarantee a specific VRChat rank
- Always test optimized avatars before uploading
- Use aggressive cleanup options at your own risk
- The tool creates duplicates - your original avatar remains untouched

## Requirements

- Unity 2022.3.22f1
- VRChat SDK 3 (Avatars)

## License

See repository for license information.
