# 🔧 WebGL "Illegal view ID:0" Error Fix - Complete Setup Guide

## Problem Description
The error `Illegal view ID:0 method_SetGuessStatus GO:playerPrefab(Clone)` occurs in Unity WebGL builds when:

1. **WebGL Context Loss**: The browser loses the WebGL rendering context (common in embedded applications)
2. **React/Next.js Re-rendering**: Component unmounting/remounting causes Unity to lose its GL context
3. **Destroyed Objects**: Unity tries to call RPC methods on destroyed GameObjects
4. **Invalid View IDs**: Photon view IDs become invalid (0) when context is corrupted

## Root Cause
- Unity WebGL builds are sensitive to browser context changes
- When embedded in React/Next.js applications, page re-renders can corrupt the WebGL context
- The `SetGuessStatus` RPC method in `PlayerController.cs` tries to update UI elements with invalid rendering contexts
- Photon networking view IDs become invalid when Unity loses its rendering context

## Files Modified/Created

### ✅ Already Updated Files:
1. **`client/components/GameWrapper.tsx`** - Enhanced with WebGL safety
2. **`client/utils/unityBridge.js`** - Added safe Unity messaging  
3. **`games/BeautyContest/Assets/Scripts/PlayerController.cs`** - Updated with safe methods
4. **`games/BeautyContest/Assets/Scripts/WebGLContextManager.cs`** - NEW context manager

---

## 🎯 Unity Setup Instructions

### Step 1: Understanding Script Attachments

#### ❌ **What NOT to do:**
- Don't attach `PlayerController.cs` to random GameObjects in your scene
- Don't create separate GameObjects for UI error panels

#### ✅ **What TO do:**

### **1. PlayerController.cs** 
**ATTACH TO:** Your **PlayerPrefab** (the prefab that represents each player in the game)

**Location:** `Assets/Prefabs/PlayerPrefab.prefab`
- This script goes on the prefab that LobbyManager instantiates for each player
- It should have UI components: `PlayerNameText`, `PlayerStatusText`, `BackgroundImage`
- The prefab needs a **PhotonView** component as well

### **2. WebGLContextManager.cs**
**ATTACH TO:** A new empty GameObject in your scene called **"WebGLContextManager"**

**Setup:**
1. Create empty GameObject: `GameObject > Create Empty`
2. Name it: `"WebGLContextManager"`
3. Attach the `WebGLContextManager.cs` script
4. **Leave all Inspector fields EMPTY** - the script creates its own error UI automatically
5. Configure these settings in Inspector:
   - ✅ Enable Auto Reload: `true`
   - Context Check Interval: `2.0`
   - Error Panel: `None` (leave empty)
   - Error Message Text: `None` (leave empty) 
   - Reload Button: `None` (leave empty)

### **3. BlockchainManager.cs**
**DECISION:** Do you actually need this in your current scene?

#### If you're using blockchain features:
- **ATTACH TO:** Empty GameObject called **"BlockchainManager"**
- **REQUIRED UI:** You need to create:
  ```
  Canvas (if not exists)
  ├── WalletAddressText (TextMeshPro)
  └── StatusText (TextMeshPro)
  ```
- **Inspector Setup:**
  - Drag `WalletAddressText` → `walletAddressText` field
  - Drag `StatusText` → `statusText` field
  - Set Contract Address if different from default

#### If you're NOT using blockchain in this scene:
- **SKIP IT** - Don't add BlockchainManager to this scene
- The web3 integration happens in the web layer (React/Next.js)

---

## 🏗️ Complete Scene Setup

### Required GameObjects in Scene:
```
Beauty Contest Scene
├── Canvas
│   ├── TitleText (TextMeshPro)
│   ├── RoundText (TextMeshPro) 
│   ├── InstructionText (TextMeshPro)
│   ├── GuessInput (TMP_InputField)
│   ├── SubmitButton (Button)
│   ├── ResultText (TextMeshPro)
│   └── PlayerPanel (Empty GameObject with GridLayoutGroup)
├── LobbyManager (Empty GameObject)
├── GameManager (Empty GameObject)
├── WebGLContextManager (Empty GameObject) ← NEW!
└── [BlockchainManager] (Optional - only if using blockchain in scene)
```

### Required Prefabs:
```
Assets/Prefabs/
└── PlayerPrefab.prefab
    ├── Image (Background)
    ├── PlayerNameText (TextMeshPro child)
    ├── PlayerStatusText (TextMeshPro child)
    ├── PlayerController.cs (script) ← UPDATED!
    └── PhotonView (component)
```

---

## 🔧 Step-by-Step Implementation

### Step 1: Update PlayerController on Prefab
1. Open `Assets/Prefabs/PlayerPrefab.prefab`
2. The `PlayerController.cs` script should already be attached
3. If not attached, drag the script onto the prefab root
4. Configure Inspector fields:
   - Player Name Text → `PlayerNameText` (child object)
   - Player Status Text → `PlayerStatusText` (child object)  
   - Background Image → `Image` component on root
   - Colors: Active=White, Eliminated=Red, Waiting=Yellow

### Step 2: Add WebGL Context Manager
1. In your scene: `GameObject > Create Empty`
2. Name it: `"WebGLContextManager"`
3. Attach `WebGLContextManager.cs` script
4. **Important:** Leave all UI references empty - script creates its own UI
5. Set: Enable Auto Reload = ✅, Check Interval = 2.0

### Step 3: BlockchainManager Decision
**Option A - Keep it (if using blockchain):**
1. Keep existing BlockchainManager GameObject
2. Make sure it has UI references properly set
3. It will work alongside the WebGL fix

**Option B - Remove it (if not needed):**
1. Delete BlockchainManager GameObject from scene
2. The web3 integration happens in React/Next.js instead
3. Cleaner setup for pure game logic

### Step 4: Test the Setup
1. **Build Settings:**
   - Platform: WebGL  
   - Compression: Brotli
   - Exception Support: Explicitly Thrown Exceptions Only

2. **Build and Test:**
   ```bash
   # In client directory
   npm run dev
   # Navigate to http://localhost:3000/game/beauty-contest
   ```

3. **Look for Success Messages:**
   ```
   ✅ "WebGL context OK - RPC sent successfully"
   ✅ "Unity message sent safely: BlockchainManager.SetWalletAddress"  
   ✅ "Safe UI update completed"
   ```

4. **Error Recovery Test:**
   - Switch browser tabs rapidly
   - Check for context loss recovery messages
   - Verify automatic page reload on critical errors

---

## 🚨 Common Setup Mistakes

### ❌ **WRONG:**
- Attaching PlayerController to scene GameObjects
- Creating manual error UI components for WebGLContextManager
- Adding BlockchainManager when you don't need blockchain
- Leaving PlayerController script unattached to prefab

### ✅ **CORRECT:**
- PlayerController attached to PlayerPrefab only
- WebGLContextManager creates its own error UI automatically
- Only include scripts you actually need for your game
- All UI references properly connected in Inspector

---

## 🎯 Expected Behavior After Fix

### ✅ **Normal Operation:**
- Game loads without "Illegal view ID" errors
- Players can submit guesses successfully  
- UI updates work smoothly
- No WebGL context crashes

### ✅ **Error Recovery:**
- Browser tab switching doesn't crash game
- Context loss shows user-friendly recovery message
- Automatic page reload on critical graphics errors
- Graceful fallback when networking fails

### ✅ **Network Resilience:**
- Photon RPC errors don't crash the game
- Local UI updates continue working
- Safe error logging instead of exceptions

---

## � Final Checklist

- [ ] `PlayerController.cs` attached to PlayerPrefab (not scene objects)
- [ ] `WebGLContextManager.cs` attached to empty GameObject in scene
- [ ] All Inspector UI references properly connected
- [ ] BlockchainManager only included if actually needed
- [ ] WebGL build settings configured correctly
- [ ] React/Next.js WebGL fixes applied
- [ ] Tested context loss recovery
- [ ] Confirmed no "Illegal view ID" errors

**This setup should completely eliminate the WebGL context errors and provide a robust gaming experience in your Next.js application.**