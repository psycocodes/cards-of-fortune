using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviourPun
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerStatusText;
    public TextMeshProUGUI playerScoreText; // NEW: Player score display
    public Image backgroundImage;
    
    [Header("Visual States")]
    public Color activeColor = Color.white;
    public Color eliminatedColor = Color.red;
    public Color waitingColor = Color.yellow;
    public Color submittedColor = Color.green; // NEW: Submitted state color
    
    private bool isEliminated = false;
    private bool hasSubmittedGuess = false;
    private int currentGuess = 0;
    private string walletAddress = "";
    private string playerDisplayName = "";
    private int playerScore = 100; // NEW: Starting score
    private int playerNumber = 0; // NEW: Player number (0, 1, 2, etc.)
    
    void Start()
    {
        // Get player number from ActorNumber (0-based for display)
        if (photonView.Owner != null)
        {
            playerNumber = photonView.Owner.ActorNumber - 1; // Convert to 0-based
            playerDisplayName = $"Player {playerNumber}";
        }
        else
        {
            playerNumber = 0;
            playerDisplayName = "Player ?";
        }
        
        // Initialize player data
        if (photonView.IsMine)
        {
            // This is our local player
            InitializeLocalPlayer();
        }
        else
        {
            // This is a remote player
            InitializeRemotePlayer();
        }
        
        Debug.Log($"PlayerController started for {playerDisplayName} (Actor: {photonView.Owner?.ActorNumber})");
    }
    
    void InitializeLocalPlayer()
    {
        // Get wallet address from BlockchainManager
        BlockchainManager blockchainManager = FindObjectOfType<BlockchainManager>();
        if (blockchainManager != null)
        {
            walletAddress = blockchainManager.GetWalletAddress();
            if (walletAddress == "Not Connected")
                walletAddress = "0x..." + Random.Range(1000, 9999); // Placeholder
        }
        else
        {
            walletAddress = playerDisplayName;
        }
        
        Debug.Log($"Local player initialized: {playerDisplayName} with wallet: {walletAddress}");
        UpdateUI();
        
        // Send player info to all clients immediately
        if (photonView != null)
        {
            photonView.RPC("SetPlayerInfo", RpcTarget.All, walletAddress, false, playerScore, playerDisplayName);
        }
    }
    
    void InitializeRemotePlayer()
    {
        // Remote players will get their info via RPC
        if (photonView.Owner != null)
        {
            walletAddress = photonView.Owner.NickName ?? playerDisplayName;
        }
        else
        {
            walletAddress = playerDisplayName;
        }
        
        Debug.Log($"Remote player initialized: {playerDisplayName}");
        UpdateUI();
    }
    
    // Method to initialize player with specific Player data (used by LobbyManager)
    public void InitializeWithPlayer(Photon.Realtime.Player player)
    {
        if (player != null)
        {
            walletAddress = player.NickName ?? ("Player_" + player.ActorNumber);
            Debug.Log($"Initialized PlayerController with: {walletAddress}");
            UpdateUI();
        }
        else
        {
            walletAddress = "Unknown Player";
            UpdateUI();
        }
    }
    
    [PunRPC]
    void SetPlayerInfo(string playerWallet, bool eliminated, int score, string displayName)
    {
        walletAddress = playerWallet;
        isEliminated = eliminated;
        playerScore = score;
        playerDisplayName = displayName;
        
        Debug.Log($"SetPlayerInfo RPC received: {displayName}, Score: {score}, Eliminated: {eliminated}");
        
        // Use safe UI update for WebGL
        WebGLContextManager.SafeUIUpdate(() => {
            UpdateUI();
        });
    }
    
    [PunRPC]
    void UpdatePlayerScore(int newScore)
    {
        playerScore = newScore;
        Debug.Log($"Score updated for {playerDisplayName}: {newScore}");
        
        // Use safe UI update for WebGL
        WebGLContextManager.SafeUIUpdate(() => {
            UpdateUI();
        });
    }
    
    [PunRPC]
    void SetGuessStatus(bool submitted, int guess = 0)
    {
        hasSubmittedGuess = submitted;
        currentGuess = guess;
        
        string statusMessage = submitted ? $"Submitted guess: {guess}" : "Waiting for guess...";
        Debug.Log($"SetGuessStatus RPC for {playerDisplayName}: {statusMessage}");
        
        // Use safe UI update for WebGL
        WebGLContextManager.SafeUIUpdate(() => {
            UpdateUI();
        });
    }
    
    [PunRPC]
    void EliminatePlayer()
    {
        isEliminated = true;
        
        // Use safe UI update for WebGL
        WebGLContextManager.SafeUIUpdate(() => {
            UpdateUI();
        });
        
        Debug.Log($"Player {walletAddress} has been eliminated!");
    }
    
    public void SubmitGuess(int guess)
    {
        if (!photonView.IsMine || isEliminated || hasSubmittedGuess) 
        {
            Debug.LogWarning($"Cannot submit guess: IsMine={photonView.IsMine}, Eliminated={isEliminated}, AlreadySubmitted={hasSubmittedGuess}");
            return;
        }
        
        currentGuess = guess;
        hasSubmittedGuess = true;
        
        Debug.Log($"Local player {playerDisplayName} submitting guess: {guess}");
        
        // Update UI immediately for local player
        UpdateUI();
        
        // Use WebGL-safe RPC call
        bool rpcSent = WebGLContextManager.SafeRPC(photonView, "SetGuessStatus", RpcTarget.All, true, guess);
        
        if (!rpcSent)
        {
            // Fallback for WebGL context loss - try direct call
            Debug.LogWarning("SafeRPC failed, trying direct RPC call");
            try
            {
                photonView.RPC("SetGuessStatus", RpcTarget.All, true, guess);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Direct RPC also failed: {ex.Message}");
            }
        }
        
        // Send guess to GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log($"Sending guess to GameManager: Player {photonView.Owner.ActorNumber}, Guess: {guess}");
            gameManager.ReceivePlayerGuess(photonView.Owner.ActorNumber, guess);
        }
        else
        {
            Debug.LogError("GameManager not found!");
        }
    }
    
    public void ResetForNewRound()
    {
        if (isEliminated) return; // Eliminated players stay eliminated
        
        hasSubmittedGuess = false;
        currentGuess = 0;
        
        // Use WebGL-safe RPC call
        WebGLContextManager.SafeRPC(photonView, "SetGuessStatus", RpcTarget.All, false, 0);
    }
    
    void UpdateUI()
    {
        // Skip UI updates if running in WebGL and context is potentially lost
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // Use try-catch for WebGL UI operations
            try
            {
                UpdateUIInternal();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"UI update failed (WebGL context issue?): {ex.Message}");
                // Don't crash the game, just log the warning
            }
        }
        else
        {
            UpdateUIInternal();
        }
    }
    
    void UpdateUIInternal()
    {
        // Update player name - now shows "Player 0", "Player 1", etc.
        if (playerNameText != null)
        {
            playerNameText.text = playerDisplayName;
        }
        
        // Update player score
        if (playerScoreText != null)
        {
            playerScoreText.text = $"Score: {playerScore}";
        }
        
        // Update status text with more detailed information
        if (playerStatusText != null)
        {
            if (isEliminated)
            {
                playerStatusText.text = "❌ ELIMINATED";
            }
            else if (hasSubmittedGuess)
            {
                playerStatusText.text = $"✅ Submitted: {currentGuess}";
            }
            else
            {
                playerStatusText.text = "⏳ Thinking...";
            }
        }
        
        // Update background color based on state
        if (backgroundImage != null)
        {
            if (isEliminated)
                backgroundImage.color = eliminatedColor;
            else if (hasSubmittedGuess)
                backgroundImage.color = submittedColor; // Green for submitted
            else
                backgroundImage.color = activeColor; // White for waiting
        }
    }
    
    // Public getters for GameManager
    public bool IsEliminated => isEliminated;
    public bool HasSubmittedGuess => hasSubmittedGuess;
    public int GetCurrentGuess => currentGuess;
    public string GetWalletAddress => walletAddress;
    public int GetActorNumber => photonView.Owner.ActorNumber;
    public int GetPlayerScore => playerScore;
    public string GetPlayerDisplayName => playerDisplayName;
    
    // Called by GameManager to update player score
    public void UpdateScore(int newScore)
    {
        if (photonView.IsMine)
        {
            // Use WebGL-safe RPC call
            WebGLContextManager.SafeRPC(photonView, "UpdatePlayerScore", RpcTarget.All, newScore);
        }
    }
    
    // Called by GameManager when player is eliminated
    public void Eliminate()
    {
        if (photonView.IsMine)
        {
            // Use WebGL-safe RPC call
            WebGLContextManager.SafeRPC(photonView, "EliminatePlayer", RpcTarget.All);
        }
    }
}
