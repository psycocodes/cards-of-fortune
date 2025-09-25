// WebGL JavaScript Library for Unity-Web3 Bridge
// This file provides the JavaScript functions that Unity calls via DllImport

var WebGLSupport = {
    $webGLBridge: {
        // Bridge state
        isInitialized: false,
        unityGameObject: "BlockchainManager",
        
        // Initialize bridge when available
        initBridge: function() {
            if (typeof window !== 'undefined' && window.connectWallet) {
                this.isInitialized = true;
                console.log("WebGL bridge functions are available");
            }
        },
        
        // Safe function caller
        callBridgeFunction: function(funcName) {
            var args = Array.prototype.slice.call(arguments, 1);
            this.initBridge();
            if (this.isInitialized && window[funcName]) {
                return window[funcName].apply(window, args);
            } else {
                console.warn("Bridge function " + funcName + " not available yet");
                // Retry after a short delay
                setTimeout(function() {
                    if (window[funcName]) {
                        window[funcName].apply(window, args);
                    }
                }, 100);
            }
        }
    },

    // Connect wallet function
    connectWallet: function() {
        webGLBridge.callBridgeFunction('connectWallet');
    },

    // Get wallet address function  
    getWalletAddress: function() {
        webGLBridge.callBridgeFunction('getWalletAddress');
    },

    // Send transaction function
    unitySendTx: function(funcNamePtr, argsJsonPtr) {
        var funcName = UTF8ToString(funcNamePtr);
        var argsJson = UTF8ToString(argsJsonPtr);
        webGLBridge.callBridgeFunction('unitySendTx', funcName, argsJson);
    },

    // Call contract function
    unityCall: function(funcNamePtr, argsJsonPtr) {
        var funcName = UTF8ToString(funcNamePtr);
        var argsJson = UTF8ToString(argsJsonPtr);
        webGLBridge.callBridgeFunction('unityCall', funcName, argsJson);
    }
};

// Add the functions to the library
autoAddDeps(WebGLSupport, '$webGLBridge');
mergeInto(LibraryManager.library, WebGLSupport);