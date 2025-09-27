using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviourPun
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerStatusText;
    public Image backgroundImage;
    
    [Header("Visual States")]
    public Color activeColor = Color.white;
    public Color eliminatedColor = Color.red;
    public Color waitingColor = Color.yellow;
    
    private bool isEliminated = false;
    private bool hasSubmittedGuess = false;
    private int currentGuess = 0;
    private string walletAddress = "";
    
    void Start()
    {
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
            walletAddress = "Player_" + photonView.Owner.ActorNumber;
        }
        
        // Set the name for this player across network
        photonView.RPC("SetPlayerInfo", RpcTarget.All, walletAddress, false);
    }
    
    void InitializeRemotePlayer()
    {
        // Remote players will get their info via RPC
        if (photonView.Owner != null)
        {
            walletAddress = photonView.Owner.NickName ?? ("Player_" + photonView.Owner.ActorNumber);
        }
        else
        {
            walletAddress = "Remote Player";
        }
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
    void SetPlayerInfo(string playerWallet, bool eliminated)
    {
        walletAddress = playerWallet;
        isEliminated = eliminated;
        UpdateUI();
    }
    
    [PunRPC]
    void SetGuessStatus(bool submitted, int guess = 0)
    {
        hasSubmittedGuess = submitted;
        currentGuess = guess;
        UpdateUI();
    }
    
    [PunRPC]
    void EliminatePlayer()
    {
        isEliminated = true;
        UpdateUI();
        Debug.Log($"Player {walletAddress} has been eliminated!");
    }
    
    public void SubmitGuess(int guess)
    {
        if (!photonView.IsMine || isEliminated || hasSubmittedGuess) return;
        
        currentGuess = guess;
        hasSubmittedGuess = true;
        
        // Notify all clients about this guess submission
        photonView.RPC("SetGuessStatus", RpcTarget.All, true, guess);
        
        // Send guess to GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ReceivePlayerGuess(photonView.Owner.ActorNumber, guess);
        }
        
        Debug.Log($"Submitted guess: {guess}");
    }
    
    public void ResetForNewRound()
    {
        if (isEliminated) return; // Eliminated players stay eliminated
        
        hasSubmittedGuess = false;
        currentGuess = 0;
        
        // Update status across network
        photonView.RPC("SetGuessStatus", RpcTarget.All, false, 0);
    }
    
    void UpdateUI()
    {
        // Update player name (shortened wallet)
        if (playerNameText != null)
        {
            if (walletAddress.Length > 10)
                playerNameText.text = walletAddress.Substring(0, 6) + "..." + walletAddress.Substring(walletAddress.Length - 4);
            else
                playerNameText.text = walletAddress;
        }
        
        // Update status text
        if (playerStatusText != null)
        {
            if (isEliminated)
            {
                playerStatusText.text = "ELIMINATED";
            }
            else if (hasSubmittedGuess)
            {
                playerStatusText.text = $"Guess: {currentGuess}";
            }
            else
            {
                playerStatusText.text = "Waiting...";
            }
        }
        
        // Update background color
        if (backgroundImage != null)
        {
            if (isEliminated)
                backgroundImage.color = eliminatedColor;
            else if (hasSubmittedGuess)
                backgroundImage.color = waitingColor;
            else
                backgroundImage.color = activeColor;
        }
    }
    
    // Public getters for GameManager
    public bool IsEliminated => isEliminated;
    public bool HasSubmittedGuess => hasSubmittedGuess;
    public int GetCurrentGuess => currentGuess;
    public string GetWalletAddress => walletAddress;
    public int GetActorNumber => photonView.Owner.ActorNumber;
    
    // Called by GameManager when player is eliminated
    public void Eliminate()
    {
        if (photonView.IsMine)
        {
            photonView.RPC("EliminatePlayer", RpcTarget.All);
        }
    }
}
