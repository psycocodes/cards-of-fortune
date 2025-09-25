# Unity WebGL Blockchain Integration Testing Guide

This guide explains how to test blockchain functionality in both Unity Editor and WebGL builds.

## Setup Overview

1. **Unity Editor/Standalone**: Uses Nethereum directly with a private key for testing
2. **WebGL Build**: Uses JavaScript bridge to communicate with MetaMask wallet via Next.js

## Files Created

### Unity Scripts
- `BlockchainManager.cs` - Main blockchain interaction script with conditional compilation
- `Assets/Plugins/WebGLSupport.jslib` - Unity JavaScript library for WebGL builds

### JavaScript Bridge
- `utils/unityBridge.js` - Handles Web3 wallet connections and blockchain calls for WebGL
- Updated `GameWrapper.tsx` - Initializes the Unity-JavaScript bridge

## Unity Setup Instructions

### 1. Create UI Components
In your Unity scene, create the following UI elements:

```
Canvas
├── WalletAddressText (Text component)
├── StatusText (Text component)
└── TestButton (Button component)
```

### 2. Attach the Script
1. Create an empty GameObject named "BlockchainManager"
2. Attach the `BlockchainManager.cs` script to it
3. In the inspector, drag the UI components to their respective fields:
   - Wallet Address Text → `walletAddressText`
   - Status Text → `statusText`
   - Test Button → `testButton`
4. Set the Contract Address field if different from default

### 3. Testing in Unity Editor
- Play the scene in Unity Editor
- The script will automatically connect using the private key
- Click the test button to send a transaction and read the result
- Watch the console for detailed logs

### 4. Building for WebGL
1. **Important**: Ensure `WebGLSupport.jslib` is in the `Assets/Plugins/` folder
2. Switch to WebGL platform in Build Settings
3. Build the project to your Next.js public folder: `client/public/games/[game-name]/`
4. The WebGL build will use MetaMask instead of private key

**Note**: The `.jslib` file is required for Unity WebGL builds to recognize the JavaScript functions called via `DllImport`. Without this file, you'll get "undefined symbol" errors during build.

## Next.js Setup

### Prerequisites
Make sure these packages are installed (already in package.json):
```bash
npm install ethers wagmi @rainbow-me/rainbowkit react-unity-webgl
```

### Testing WebGL Build
1. **Connect wallet on the main page first**:
   - Navigate to `http://localhost:3000/`
   - Click "Connect Wallet" and connect your MetaMask
   - Ensure you're on Sepolia testnet

2. **Navigate to the game**:
   - Click "Enter the Game" or go to `http://localhost:3000/game/beauty-contest`

3. The game will:
   - Load ethers.js automatically
   - Use the wallet address from the main page (no new connection requested)
   - Display the connected wallet address in Unity
   - Allow blockchain interactions via the test button using the existing wallet connection

## Workflow

The correct flow is:
1. **Main Page (`/`)**: User connects wallet using RainbowKit
2. **Game Page (`/game/[slug]`)**: Game receives wallet info and uses it
3. **No wallet connection requests inside the game**

## Blockchain Configuration

### Contract Details
- **Network**: Sepolia Testnet (Chain ID: 11155111)
- **Contract Address**: `0x6a821521b3C6a3E6298E222082c927bFdE0ddA38`
- **Functions**:
  - `setNumber(uint256)` - Write operation
  - `getNumber()` - Read operation
  - `storedNumber()` - Read operation

### Test Private Key (Editor Only)
The private key in the script is for testing purposes only. Replace with your own test key:
```csharp
private string privateKey = "your_test_private_key_here";
```

## Troubleshooting

### WebGL Build Issues
- **HTTP error 500 / COOP errors**: Restart Next.js dev server after config changes (`npm run dev`)
- **"undefined symbol" errors**: Ensure `WebGLSupport.jslib` is in `Assets/Plugins/` folder
- **Game not loading**: Check Unity build file paths match actual generated files
- **"Ethers.js not loaded" error**: The bridge will automatically load ethers.js dynamically
- **MetaMask not detected**: Install MetaMask browser extension
- **Contract not initialized**: Wallet connection failed
- **Wrong network**: Switch to Sepolia testnet in MetaMask

### File Path Issues
If your Unity build generates files with different names (e.g., `game-name.loader.js` instead of `Build.loader.js`), the GameWrapper automatically handles this by using the `gameId` parameter to construct the correct file paths.

### Unity Editor Issues
- Ensure Nethereum packages are properly imported
- Check console for connection errors
- Verify contract address and ABI are correct

### WebGL Issues
- Check browser console for JavaScript errors
- Ensure MetaMask is installed and connected
- Verify the Unity bridge is properly initialized
- Check that the correct network (Sepolia) is selected

### Common Error Messages
- "MetaMask not detected" → Install MetaMask browser extension
- "Contract not initialized" → Wallet connection failed
- "Wrong network" → Switch to Sepolia testnet in MetaMask

## Development Workflow

1. **Editor Testing**: Use Unity Editor for rapid iteration and debugging
2. **WebGL Testing**: Build and test in browser for production-like environment
3. **Production**: Deploy with proper wallet integration and security measures

## Security Notes

⚠️ **Important**: 
- Never commit private keys to version control
- Use test networks only for development
- Remove or secure private keys before production deployment
- The provided private key is for testing purposes only

## Next Steps

- Replace test contract with your game's smart contracts
- Add more blockchain functions as needed
- Implement proper error handling and user feedback
- Add loading states and transaction confirmations