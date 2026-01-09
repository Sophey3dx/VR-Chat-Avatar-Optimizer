# VRChat Avatar Optimizer

A production-ready Unity Editor Tool for VRChat avatar creators that automatically generates optimized avatar variants based on the currently active outfit state.

## Features

- **Safe Optimization**: Never modifies the original avatar - always creates duplicates
- **Dry Run Mode**: Preview changes before applying them
- **Comprehensive Analysis**: Scans AnimatorControllers, Expression Menus, PhysBones, and more
- **Performance Estimation**: Provides estimated performance tier (not guaranteed rank)
- **Advanced Options**: Aggressive cleanup modes for experienced users

## Usage

1. Open the tool via `Window > VRChat Avatar Optimizer`
2. Select your avatar GameObject (must have VRC Avatar Descriptor)
3. Click "Capture Snapshot" to analyze current state
4. Review the optimization report
5. Enable "Dry Run" to preview changes, or click "Create Optimized Avatar" to apply

## Important Notes

- This tool estimates performance tiers - it does NOT guarantee a specific VRChat rank
- Always test optimized avatars before uploading
- Use aggressive cleanup options at your own risk
- The tool creates duplicates - your original avatar remains untouched

## Installation

### Adding to VCC (VRChat Creator Companion)

1. **Open VCC** and your VRChat project
2. **Go to Packages tab** in VCC
3. **Click "Add Package"** → **"Add from disk"**
4. **Navigate to this folder** and select it
5. The package will be automatically added to your project

Alternatively, you can copy this folder to your project's `Packages/` directory.

See `INSTALLATION.md` for detailed instructions.

## Requirements

- Unity 2022.3.22f1
- VRChat SDK 3 (Avatars)

