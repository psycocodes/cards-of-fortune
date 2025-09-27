"use client";
import React, { useEffect } from "react";
import { Unity, useUnityContext } from "react-unity-webgl";
import { Expand, LampDeskIcon } from "lucide-react";
import { isMobileDevice } from "@/utils/isMobile";
import { initUnityBridge } from "@/utils/unityBridge";
import { useAccount, useWalletClient } from "wagmi";


export default function GameWrapper({ gameId }: { gameId: string }) {
  console.log("GameWrapper initialized with gameId:", gameId);
  
  // Get wallet information from wagmi
  const { address, isConnected, chain } = useAccount();
  const { data: walletClient } = useWalletClient();
  
  const unityContextConfig = {
    loaderUrl: `/games/${gameId}/Build/${gameId}.loader.js`,
    dataUrl: `/games/${gameId}/Build/${gameId}.data`,
    frameworkUrl: `/games/${gameId}/Build/${gameId}.framework.js`,
    codeUrl: `/games/${gameId}/Build/${gameId}.wasm`,
  };
  
  console.log("Unity context config:", unityContextConfig);
  
  const { 
    unityProvider, 
    requestFullscreen, 
    isLoaded, 
    loadingProgression, 
    sendMessage,
    addEventListener,
    removeEventListener,
    unload
  } = useUnityContext(unityContextConfig);

  // Safe Unity message sending wrapper with better error handling
  const safeSendMessage = React.useCallback((objectName: string, methodName: string, value: string = "") => {
    try {
      if (!isLoaded) {
        console.warn(`Cannot send message to Unity - not loaded yet. Message: ${objectName}.${methodName}(${value})`);
        return false;
      }
      
      // Check if we're in a valid state
      if (typeof sendMessage !== 'function') {
        console.error('sendMessage is not a function - Unity context may be corrupted');
        return false;
      }
      
      console.log(`Sending Unity message: ${objectName}.${methodName}(${value})`);
      sendMessage(objectName, methodName, value);
      return true;
    } catch (error) {
      console.error(`Failed to send Unity message ${objectName}.${methodName}:`, error);
      
      // Check if this is a WebGL context error
      if (error instanceof Error && error.message.includes('view')) {
        console.error('WebGL context lost - attempting page reload');
        setTimeout(() => {
          window.location.reload();
        }, 2000);
      }
      
      return false;
    }
  }, [isLoaded, sendMessage]);

  // Initialize Unity bridge when Unity is loaded
  useEffect(() => {
    const initializeBridge = async () => {
      if (isLoaded) {
        console.log("Unity game loaded successfully!");
        
        // Check wallet connection status
        if (!isConnected || !address) {
          console.warn("Wallet not connected. Redirecting to main page...");
          // You might want to redirect or show a message
          return;
        }

        console.log("Wallet connected:", {
          address,
          chainId: chain?.id,
          chainName: chain?.name
        });
        
        // Make SAFE sendMessage function globally available for the bridge
        (window as any).unitySendMessage = safeSendMessage;
        
        // Load ethers.js if not already loaded
        if (typeof (window as any).ethers === 'undefined') {
          try {
            // Dynamically import ethers
            const ethers = await import('ethers');
            (window as any).ethers = ethers;
            console.log("Ethers.js loaded successfully");
          } catch (error) {
            console.error("Failed to load ethers.js:", error);
            return;
          }
        }
        
        // Initialize the bridge with wallet information
        console.log("Initializing bridge with wallet info:", {
          address,
          chainId: chain?.id,
          chainName: chain?.name,
          hasWalletClient: !!walletClient
        });
        
        initUnityBridge("BlockchainManager", {
          walletAddress: address,
          chainId: chain?.id,
          walletClient: walletClient
        } as any);
        console.log("Unity bridge initialized for game:", gameId);
        
        // Send wallet information to Unity with safety checks
        setTimeout(() => {
          try {
            console.log("Sending wallet address to Unity:", address);
            const addressSent = safeSendMessage("BlockchainManager", "SetWalletAddress", address);
            
            if (addressSent) {
              // Initialize blockchain in Unity only if address was sent successfully
              setTimeout(() => {
                safeSendMessage("BlockchainManager", "InitializeBlockchain", "");
              }, 100);
            } else {
              console.error("Failed to send wallet address - skipping blockchain initialization");
            }
          } catch (error) {
            console.error("Failed to send wallet info to Unity:", error);
          }
        }, 500);

        // Also send player info to PlayerController with retries
        setTimeout(() => {
          const sendPlayerInfo = (attempts: number = 3) => {
            if (attempts <= 0) {
              console.error("Failed to send player info after multiple attempts");
              return;
            }

            console.log(`Sending player info to Unity (attempt ${4 - attempts}):`, address);
            const playerInfoSent = safeSendMessage("PlayerController", "SetPlayerInfo", address);
            
            if (!playerInfoSent) {
              console.warn(`Player info send failed, retrying in 1 second... (${attempts - 1} attempts left)`);
              setTimeout(() => sendPlayerInfo(attempts - 1), 1000);
            } else {
              console.log("Player info sent successfully to Unity");
            }
          };

          sendPlayerInfo();
        }, 1000);
        
        // Force Unity to refresh its canvas size
        setTimeout(() => {
          window.dispatchEvent(new Event('resize'));
        }, 100);
      }
    };
    
    initializeBridge();
  }, [isLoaded, safeSendMessage, gameId, address, isConnected, chain, walletClient]);

  // Handle component cleanup to prevent WebGL context issues
  useEffect(() => {
    return () => {
      // Cleanup Unity instance when component unmounts
      console.log('GameWrapper unmounting - cleaning up Unity instance');
      try {
        if (typeof unload === 'function') {
          unload();
        }
      } catch (error) {
        console.error('Error during Unity cleanup:', error);
      }
    };
  }, [unload]);

  // Monitor WebGL context and handle context loss
  useEffect(() => {
    if (!isLoaded) return;

    const handleContextLost = (event: Event) => {
      console.error('WebGL context lost!');
      event.preventDefault();
      // Show user-friendly error
      const errorDiv = document.createElement('div');
      errorDiv.innerHTML = '<strong>Graphics Error:</strong> The game needs to reload due to graphics driver issues.';
      errorDiv.style.cssText = 'position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); background: orange; color: black; padding: 20px; border-radius: 8px; z-index: 9999;';
      document.body.appendChild(errorDiv);
      
      setTimeout(() => {
        window.location.reload();
      }, 3000);
    };

    const handleContextRestored = (event: Event) => {
      console.log('WebGL context restored');
    };

    const canvas = document.querySelector('#unity-canvas') as HTMLCanvasElement;
    if (canvas) {
      canvas.addEventListener('webglcontextlost', handleContextLost);
      canvas.addEventListener('webglcontextrestored', handleContextRestored);
      
      return () => {
        canvas.removeEventListener('webglcontextlost', handleContextLost);
        canvas.removeEventListener('webglcontextrestored', handleContextRestored);
      };
    }
  }, [isLoaded]);

  // Add error handling
  useEffect(() => {
    const handleError = (error: any) => {
      console.error("Unity error:", error);
      // Show error in UI
      const errorDiv = document.createElement('div');
      errorDiv.innerHTML = `<strong>Unity Error:</strong> ${error.message || error}`;
      errorDiv.style.cssText = 'position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); background: red; color: white; padding: 20px; border-radius: 8px; z-index: 9999;';
      document.body.appendChild(errorDiv);
      
      setTimeout(() => {
        if (document.body.contains(errorDiv)) {
          document.body.removeChild(errorDiv);
        }
      }, 5000);
    };

    const handleProgress = (progression: number) => {
      console.log("Unity loading progress:", Math.round(progression * 100) + "%");
    };

    const handleLoaded = () => {
      console.log("Unity fully loaded and ready!");
    };

    addEventListener("error", handleError);
    addEventListener("progress", handleProgress);
    addEventListener("loaded", handleLoaded);
    
    return () => {
      removeEventListener("error", handleError);
      removeEventListener("progress", handleProgress);
      removeEventListener("loaded", handleLoaded);
    };
  }, [addEventListener, removeEventListener]);

  // Check if files exist and are accessible
  useEffect(() => {
    const checkFiles = async () => {
      const files = [
        `/games/${gameId}/Build/${gameId}.loader.js`,
        `/games/${gameId}/Build/${gameId}.data`,
        `/games/${gameId}/Build/${gameId}.framework.js`,
        `/games/${gameId}/Build/${gameId}.wasm`
      ];
      
      console.log("Checking Unity build files...");
      
      for (const file of files) {
        try {
          const response = await fetch(file, { method: 'HEAD' });
          if (!response.ok) {
            console.error(`❌ File not found: ${file} (${response.status} ${response.statusText})`);
          } else {
            console.log(`✅ File found: ${file} (${response.headers.get('content-type')})`);
          }
        } catch (error) {
          console.error(`❌ Error checking file ${file}:`, error);
        }
      }
    };
    
    checkFiles();
  }, [gameId]);

  function handleFullscreen() {
    requestFullscreen(true);
  }

  if (isMobileDevice()) {
    return (
      <div className="flex flex-col items-center justify-center h-screen bg-gray-900 text-white p-4">
        <LampDeskIcon className="w-16 h-16 mb-4 text-blue-400" />
        <h1 className="text-2xl font-bold text-center">Desktop Recommended</h1>
        <p className="mt-2 text-center text-gray-300 max-w-sm font-sans">
          This interactive experience is designed for a desktop browser. Please switch to a PC for the best performance.
        </p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900 flex items-center justify-center p-4">
      {/* Game Container */}
      <div className="relative bg-black rounded-lg overflow-hidden shadow-2xl max-w-6xl w-full" style={{ aspectRatio: '16/9' }}>
        {/* Debug info */}
        <div className="absolute top-4 left-4 z-20 bg-black bg-opacity-75 text-white px-3 py-2 rounded text-xs">
          <div>Loading: {Math.round(loadingProgression * 100)}%</div>
          <div>Game: {gameId}</div>
          <div>Loaded: {isLoaded ? 'Yes' : 'No'}</div>
        </div>
        
        {/* Unity Game Canvas */}
        <Unity
          unityProvider={unityProvider}
          className="w-full h-full"
          id="unity-canvas"
          style={{
            width: '100%',
            height: '100%',
            display: 'block',
            backgroundColor: '#000'
          }}
          tabIndex={1}
          devicePixelRatio={1}
        />
        
        {/* Fullscreen button */}
        {isLoaded && (
          <button
            onClick={handleFullscreen}
            className="absolute bottom-4 right-4 bg-black bg-opacity-50 text-white p-2 rounded hover:bg-opacity-75 transition-all z-10"
            title="Enter Fullscreen"
          >
            <Expand className="w-4 h-4"/>
          </button>
        )}
        
        {/* Loading overlay */}
        {!isLoaded && (
          <div className="absolute inset-0 bg-black bg-opacity-75 flex items-center justify-center z-30">
            <div className="text-white text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mx-auto mb-4"></div>
              <div className="text-lg font-bold">Loading Game...</div>
              <div className="text-sm opacity-75">{Math.round(loadingProgression * 100)}%</div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
