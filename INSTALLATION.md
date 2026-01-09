# Installation Guide - VRChat Avatar Optimizer

## Adding the Package to VCC (VRChat Creator Companion)

**Important:** The "Add Repository" button in VCC is for VPM repositories (with index.json format), NOT for Git repositories. Use one of the methods below instead.

### Method 1: Install from GitHub (Recommended)

#### Option A: Via Unity Package Manager

1. **Open your VRChat project in Unity** (through VCC)
2. **In Unity Editor**, go to: `Window > Package Manager`
3. Click the **"+"** button in the top-left of Package Manager
4. Select **"Add package from git URL..."**
5. Enter: `https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git`
6. Click **"Add"** - the package will be downloaded and imported automatically

#### Option B: Via manifest.json

1. Open your project's `Packages/manifest.json` file
2. Add this line to the `dependencies` section:
   ```json
   "com.vrchat.avatar-optimizer": "https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git"
   ```
3. Save the file - Unity will automatically download and import the package

### Method 2: Local Package (For Development)

#### Option A: Via Unity Editor (Easiest)

1. **Open your VRChat project in Unity** (through VCC)
2. **In Unity Editor**, go to: `Window > Package Manager`
3. Click the **"+"** button in the top-left of Package Manager
4. Select **"Add package from disk..."**
5. Navigate to: `C:\Users\david\Desktop\VR CHAT AVATAR OPTIMIZER`
6. Select the **`package.json`** file
7. The package will be added and imported automatically

#### Option B: Manual Copy (Alternative)

1. **Open your VCC project folder** in File Explorer
2. Navigate to the **`Packages`** folder
3. **Copy the entire `VR CHAT AVATAR OPTIMIZER` folder** into `Packages/`
4. **Rename it** to `com.vrchat.avatar-optimizer` (optional but recommended)
5. **Reload Unity** - the package will be automatically detected

#### Option C: Edit manifest.json

1. Open your project's `Packages/manifest.json` file
2. Add this line to the dependencies:
   ```json
   "com.vrchat.avatar-optimizer": "file:../VR CHAT AVATAR OPTIMIZER"
   ```
   (Adjust the path relative to your Packages folder)
3. Save and Unity will automatically import the package

### Method 2: Manual Installation (Alternative)

1. **Copy Package to VCC Packages Folder**
   - Navigate to your VCC project folder
   - Go to: `Packages/` directory
   - Copy the entire `VR CHAT AVATAR OPTIMIZER` folder into `Packages/`
   - Rename it to: `com.vrchat.avatar-optimizer` (optional, but recommended)

2. **Update manifest.json** (if needed)
   - Open `Packages/manifest.json`
   - Add this line if not automatically added:
   ```json
   "com.vrchat.avatar-optimizer": "file:../com.vrchat.avatar-optimizer"
   ```
   - Or if you copied it directly:
   ```json
   "com.vrchat.avatar-optimizer": "file:com.vrchat.avatar-optimizer"
   ```

3. **Reload Unity**
   - Unity will automatically detect and import the package

## Using the Tool

1. **Open Unity Editor** (through VCC)
2. **Open the Tool**
   - Go to: `Window > VRChat Avatar Optimizer`
3. **Follow the Guide Tab**
   - The tool has 3 tabs: Guide, Analysis, Optimize
   - Start with the Guide tab for instructions

## Troubleshooting

### Package Not Appearing
- Make sure the `package.json` file is in the root of the package folder
- Check that the folder structure matches the expected format
- Restart VCC and Unity

### Assembly Definition Errors
- Ensure VRChat SDK 3 is installed in your project
- The package should automatically reference VRC SDK assemblies

### Tool Not in Menu
- Check that the package was imported successfully
- Look for any compilation errors in the Unity Console
- Try reimporting the package

## Requirements

- Unity 2022.3.22f1
- VRChat SDK 3 (Avatars) - must be installed in your project
- VRChat Creator Companion

## Notes

- The package works as a local package during development
- For distribution, you would typically publish it to a Git repository or VPM registry
- All original avatars are never modified - the tool always creates duplicates

