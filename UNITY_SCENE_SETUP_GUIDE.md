# Unity Scene Setup Guide - Beauty Contest Game

This guide walks you through all the Unity Scene changes needed to implement the enhanced Beauty Contest game with improved UX, WebGL safety, and proper player management.

## 🎯 Overview of Changes

We've enhanced:
- Player scoring system with UI display
- Better status indicators with emojis
- WebGL context safety management
- Improved game flow and timing
- Enhanced result displays

---

## 📋 Required Scene Components Setup

### 1. **GameManager Setup**

#### GameManager GameObject
1. **Create/Find GameManager GameObject**:
   - In Hierarchy: Right-click → Create Empty → Name it "GameManager"
   - Add Component: `GameManager` script
   - Add Component: `Photon View`

2. **GameManager Script Configuration**:
   ```
   ✅ UI References:
   - Title Text: [Drag TextMeshPro component]
   - Round Text: [Drag TextMeshPro component] 
   - Instruction Text: [Drag TextMeshPro component]
   - Guess Input: [Drag TMP_InputField component]
   - Submit Button: [Drag Button component]
   - Result Text: [Drag TextMeshPro component]
   
   ✅ Game Settings:
   - Round Duration: 30
   - Result Display Time: 10 (increased from 5)
   - Result Processing Time: 3 (NEW)
   
   ✅ Game Rules:
   - Round Instructions[0]: "Guess a number between 0-100. Target: 80% of average."
   - Round Instructions[1]: "Rule 2: Only even numbers allowed. Target: 80% of average."
   - Round Instructions[2]: "Rule 3: No numbers ending in 0. Target: 80% of average."
   ```

3. **Photon View Configuration**:
   - Observed Components: GameManager script
   - Synchronization: Unreliable On Change
   - Ownership Transfer: Fixed

---

### 2. **PlayerController & PlayerPrefab Setup**

#### PlayerPrefab Updates (CRITICAL CHANGES)
1. **Open PlayerPrefab** (usually in `Assets/Prefabs/`):

2. **Add New UI Components**:
   ```
   PlayerPrefab/
   ├── PlayerUI/
   │   ├── PlayerNameText (existing TextMeshPro)
   │   ├── PlayerStatusText (existing TextMeshPro) 
   │   └── PlayerScoreText (NEW - TextMeshPro) ⚠️ ADD THIS
   ```

3. **Create PlayerScoreText**:
   - Right-click PlayerUI → UI → Text - TextMeshPro
   - Name: "PlayerScoreText"
   - Text: "Score: 0"
   - Font Size: 14
   - Color: White (#FFFFFF)
   - Position: Below PlayerStatusText

4. **PlayerController Script Configuration**:
   ```
   ✅ UI References:
   - Player Name Text: [Existing TextMeshPro]
   - Player Status Text: [Existing TextMeshPro]
   - Player Score Text: [NEW - Drag PlayerScoreText] ⚠️ REQUIRED
   
   ✅ Player Info:
   - Wallet Address: [Will be set via code]
   - Player Number: [Will be auto-assigned]
   - Player Score: [Will be tracked automatically]
   ```

5. **Photon View Configuration**:
   - Observed Components: PlayerController script
   - Synchronization: Unreliable On Change
   - Ownership Transfer: Take Over

---

### 3. **WebGL Context Manager Setup**

#### Create WebGLContextManager GameObject
1. **Create GameObject**:
   - Hierarchy: Right-click → Create Empty → Name "WebGLContextManager"
   - Add Component: `WebGLContextManager` script

2. **No Configuration Needed**:
   - This script works automatically
   - Provides safety wrappers for WebGL operations
   - Handles context loss recovery

---

### 4. **UI Layout Improvements**

#### Main Game UI Canvas Setup
```
GameCanvas/
├── Header/
│   ├── TitleText ("Keynesian Beauty Contest")
│   └── RoundText ("Round 1")
├── Instructions/
│   └── InstructionText (Dynamic rule display)
├── Input/
│   ├── GuessInputField (TMP_InputField)
│   └── SubmitButton (Button)
├── Results/
│   └── ResultText (Large TextMeshPro for results)
└── Players/
    ├── Player1Panel/
    ├── Player2Panel/
    └── Player3Panel/
```

#### Enhanced ResultText Styling
```
✅ ResultText Configuration:
- Font Size: 18-24 (larger for better visibility)
- Text Alignment: Center
- Color: White (#FFFFFF)
- Background: Semi-transparent dark panel
- Auto Size: Best Fit enabled
- Rich Text: Enabled (for emoji support)
```

---

### 5. **Player Panel Layout**

#### Individual Player Panel Structure
```
PlayerPanel/
├── Background (Image - Dark panel)
├── PlayerNameText ("Player 0", "Player 1", etc.)
├── PlayerStatusText ("🎮 Waiting", "✅ Submitted", "❌ Eliminated")
└── PlayerScoreText ("Score: 150") ⚠️ NEW COMPONENT
```

#### Player Status Text Configuration
```
✅ Enhanced Status Display:
- Supports emoji indicators
- Rich Text: Enabled
- Font Size: 14-16
- Colors:
  - Waiting: Yellow (#FFFF00)
  - Submitted: Green (#00FF00) 
  - Eliminated: Red (#FF0000)
```

---

## 🛠️ Step-by-Step Setup Instructions

### Step 1: Update PlayerPrefab
1. Open `Assets/Prefabs/PlayerPrefab` or your player prefab
2. Select the PlayerUI container
3. Add new TextMeshPro component named "PlayerScoreText"
4. Position it below existing status text
5. Configure text properties (size, color, etc.)
6. Drag this new component to PlayerController's "Player Score Text" field
7. **Apply changes to prefab**

### Step 2: Configure GameManager
1. Select GameManager GameObject in scene
2. Ensure all UI references are properly assigned
3. Update timing values as specified above
4. Verify Photon View is attached and configured

### Step 3: Add WebGL Safety
1. Create new empty GameObject named "WebGLContextManager"
2. Add WebGLContextManager script component
3. No further configuration needed

### Step 4: Test UI Flow
1. Run scene in Play Mode
2. Verify all text components show up properly
3. Check that player names show as "Player 0", "Player 1", etc.
4. Confirm emoji indicators work in status text

---

## ⚠️ Critical Requirements Checklist

### Before Testing:
- [ ] PlayerPrefab has PlayerScoreText component added
- [ ] PlayerController script references PlayerScoreText
- [ ] GameManager timing values updated (resultDisplayTime: 10, resultProcessingTime: 3)
- [ ] WebGLContextManager GameObject created
- [ ] All UI text supports Rich Text for emojis
- [ ] Photon View components properly configured

### Script Dependencies:
- [ ] GameManager.cs (updated with new timing and logic)
- [ ] PlayerController.cs (updated with scoring and display names)  
- [ ] WebGLContextManager.cs (new safety wrapper)

### UI Requirements:
- [ ] ResultText component can display multi-line formatted text
- [ ] Player panels have consistent styling
- [ ] Status indicators support emoji characters
- [ ] Score display is visible and properly positioned

---

## 🎮 Expected Behavior After Setup

### During Game:
1. **Player Naming**: Shows "Player 0", "Player 1" instead of wallet addresses
2. **Status Updates**: Real-time emoji indicators (🎮 ✅ ❌)
3. **Score Tracking**: Each player shows current score
4. **Enhanced Results**: Formatted with emojis and player names
5. **Better Timing**: Smooth transitions, 10-second result display

### WebGL Safety:
1. **Error Recovery**: Automatic handling of WebGL context loss
2. **Safe Operations**: All Unity calls wrapped in safety checks  
3. **User Feedback**: Clear error messages if issues occur

### Game Flow:
1. **Immediate Processing**: When all players submit, results show immediately
2. **2-Player Ending**: Game properly ends when 2 players remain
3. **Winner Display**: Clear winner announcement with celebration

---

## 🐛 Troubleshooting

### Common Issues:

1. **"PlayerScoreText not assigned"**:
   - Ensure you added PlayerScoreText to PlayerPrefab
   - Verify it's dragged to PlayerController component
   - Apply prefab changes

2. **Emojis not displaying**:
   - Enable "Rich Text" on TextMeshPro components
   - Use TextMeshPro instead of regular Text components

3. **Players not showing proper names**:
   - Check PlayerController.GetPlayerDisplayName() is implemented
   - Verify player numbering logic in PlayerController.Start()

4. **Game not ending with 2 players**:
   - Confirm GameManager.ProcessRoundResults() logic updated
   - Check EndGameWithWinner() method exists

5. **WebGL errors persist**:
   - Verify WebGLContextManager is in scene
   - Check React GameWrapper has enhanced error handling
   - Ensure Unity messages use safe wrappers

---

## 📁 File Locations

```
Unity Project Structure:
Assets/
├── Scripts/
│   ├── GameManager.cs ✅ Updated
│   ├── PlayerController.cs ✅ Updated  
│   └── WebGLContextManager.cs ✅ New
├── Prefabs/
│   └── PlayerPrefab ⚠️ Needs PlayerScoreText
└── Scenes/
    └── BeautyContestScene ⚠️ Needs setup
```

---

## ✅ Final Verification

After completing all steps:

1. **Build Test**: Create WebGL build and test in browser
2. **Multiplayer Test**: Test with 2+ players in different browser tabs
3. **Error Handling**: Verify WebGL context issues are handled gracefully
4. **UX Flow**: Confirm smooth game progression and clear feedback
5. **Results Display**: Check formatted results with emojis and names

The game should now provide a much smoother, clearer experience with proper error handling and enhanced UX as requested!