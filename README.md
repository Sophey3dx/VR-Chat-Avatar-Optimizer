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

### Method 1: Install from GitHub (Recommended)

**Important:** The "Add Repository" button in VCC is for VPM repositories (with index.json), not Git repositories. Use one of these methods instead:

#### Option A: Via Unity Package Manager (Easiest)

1. **Open your VRChat project in Unity** (through VCC)
2. **In Unity Editor**, go to: `Window > Package Manager`
3. Click the **"+"** button in the top-left
4. Select **"Add package from git URL..."**
5. Enter this URL:
   ```
   https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git
   ```
6. Click **"Add"** - the package will be installed automatically

#### Option B: Via manifest.json

1. Open your project's `Packages/manifest.json` file
2. Add this line to the dependencies:
   ```json
   "com.vrchat.avatar-optimizer": "https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git"
   ```
3. Save the file - Unity will automatically download and import the package

### Method 2: Local Installation

1. **Clone or download** this repository
2. **Open Unity Package Manager** (`Window > Package Manager`)
3. Click **"+"** → **"Add package from disk..."**
4. Navigate to the downloaded folder and select **`package.json`**

See `INSTALLATION.md` for detailed instructions.

## Requirements

- Unity 2022.3.22f1
- VRChat SDK 3 (Avatars)

