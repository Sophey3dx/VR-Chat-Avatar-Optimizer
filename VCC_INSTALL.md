# VCC Installation - Quick Guide

## ⚠️ Important: VCC "Add Repository" Button

The **"Add Repository"** button in VCC is **ONLY** for VPM repositories (with `index.json` format), **NOT** for Git repositories.

For Git repositories like this one, use Unity's Package Manager instead.

## ✅ Correct Installation Method

### Step 1: Open Unity
- Open your VRChat project in Unity (via VCC)

### Step 2: Open Package Manager
- Go to: `Window > Package Manager`

### Step 3: Add from Git URL
- Click the **"+"** button (top-left)
- Select **"Add package from git URL..."**
- Enter: `https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git`
- Click **"Add"**

### Alternative: Edit manifest.json

1. Open `Packages/manifest.json` in your project
2. Add this line to `dependencies`:
   ```json
   "com.vrchat.avatar-optimizer": "https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git"
   ```
3. Save - Unity will auto-import

## ✅ Verification

After installation:
- Go to: `Window > VRChat Avatar Optimizer`
- The tool should open

## ❌ What NOT to Do

- ❌ Don't use VCC's "Add Repository" button
- ❌ Don't use the GitHub URL directly in VCC's repository field
- ❌ Don't try to add `package.json` URL in VCC

## ✅ What Works

- ✅ Unity Package Manager → Add from Git URL
- ✅ Edit `manifest.json` directly
- ✅ Clone locally and add from disk

