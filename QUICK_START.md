# Quick Start - VRChat Avatar Optimizer

## ⚠️ Important

**Do NOT use VCC's "Add Repository" button!** It's for VPM repositories only, not Git repositories.

## Installation from GitHub (3 Steps)

### Step 1: Open Unity Package Manager
- Open your VRChat project in Unity (via VCC)
- Go to: `Window > Package Manager`

### Step 2: Add from Git URL
- Click the **"+"** button (top-left)
- Select **"Add package from git URL..."**
- Enter: `https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git`
- Click **"Add"**

### Step 3: Use the Tool
- Go to: `Window > VRChat Avatar Optimizer`
- Follow the Guide tab for instructions

## Alternative: Edit manifest.json

1. Open `Packages/manifest.json` in your project
2. Add to `dependencies`:
   ```json
   "com.vrchat.avatar-optimizer": "https://github.com/Sophey3dx/VR-Chat-Avatar-Optimizer.git"
   ```
3. Save - Unity auto-imports

## Local Installation (For Development)

1. Clone/download this repository
2. In Unity Package Manager: **"+"** → **"Add package from disk..."**
3. Select the `package.json` file
