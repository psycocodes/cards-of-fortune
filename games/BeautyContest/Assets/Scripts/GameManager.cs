using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviourPun
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI instructionText;
    public TMP_InputField guessInput;
    public Button submitButton;
    public TextMeshProUGUI resultText;
    
    [Header("Game Settings")]
    public float roundDuration = 30f;
    public float resultDisplayTime = 5f;
    
    [Header("Game Rules")]
    public string[] roundInstructions = {
        "Guess a number between 0-100. Target: 80% of average.",
        "Rule 2: Only even numbers allowed. Target: 80% of average.",
        "Rule 3: No numbers ending in 0. Target: 80% of average."
    };
    
    // Game state
    private int currentRound = 0;
    private bool gameInProgress = false;
    private bool roundInProgress = false;
    private Dictionary<int, int> currentRoundGuesses = new Dictionary<int, int>();
    private List<int> eliminatedPlayers = new List<int>();
    private float roundTimer = 0f;
    
    // Results
    private float lastRoundAverage = 0f;
    private float lastRoundTarget = 0f;
    private int lastRoundWinner = -1;
    private int lastRoundEliminated = -1;
    
    void Start()
    {
        InitializeUI();
        
        // Set up submit button
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
            
        // Initially disable game UI
        SetGameUIActive(false);
    }
    
    void Update()
    {
        if (roundInProgress && PhotonNetwork.IsMasterClient)
        {
            roundTimer -= Time.deltaTime;
            
            if (roundTimer <= 0f)
            {
                // Time's up! Process round with current guesses
                ProcessRoundResults();
            }
        }
    }
    
    void InitializeUI()
    {
        if (titleText != null)
            titleText.text = "Keynesian Beauty Contest";
            
        if (resultText != null)
            resultText.text = "";
            
        UpdateRoundUI();
    }
    
    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        Debug.Log("Starting Keynesian Beauty Contest Game!");
        
        gameInProgress = true;
        currentRound = 0;
        eliminatedPlayers.Clear();
        
        // Notify all clients to start the game
        if (photonView != null)
        {
            photonView.RPC("OnGameStarted", RpcTarget.All);
        }
        else
        {
            Debug.LogError("PhotonView is null! Make sure GameManager has a PhotonView component.");
            // Fallback: call locally only
            OnGameStarted();
            return;
        }
        
        // Start first round
        StartNewRound();
    }
    
    [PunRPC]
    void OnGameStarted()
    {
        gameInProgress = true;
        SetGameUIActive(true);
        
        if (resultText != null)
            resultText.text = "Game Starting...";
            
        Debug.Log("Game started for all clients");
    }
    
    void StartNewRound()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        currentRound++;
        currentRoundGuesses.Clear();
        roundTimer = roundDuration;
        
        Debug.Log($"Starting Round {currentRound}");
        
        // Reset all players for new round
        ResetPlayersForNewRound();
        
        // Notify all clients about new round
        photonView.RPC("OnNewRoundStarted", RpcTarget.All, currentRound);
        
        roundInProgress = true;
    }
    
    [PunRPC]
    void OnNewRoundStarted(int round)
    {
        currentRound = round;
        roundInProgress = true;
        
        UpdateRoundUI();
        
        // Enable input for non-eliminated players
        bool isEliminated = eliminatedPlayers.Contains(PhotonNetwork.LocalPlayer.ActorNumber);
        if (guessInput != null)
        {
            guessInput.text = "";
            guessInput.interactable = !isEliminated;
        }
            
        if (submitButton != null)
            submitButton.interactable = !isEliminated;
            
        if (resultText != null)
            resultText.text = isEliminated ? "You are eliminated" : $"Round {round} - Make your guess!";
    }
    
    void ResetPlayersForNewRound()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (!eliminatedPlayers.Contains(player.GetActorNumber))
            {
                player.ResetForNewRound();
            }
        }
    }
    
    public void ReceivePlayerGuess(int playerActorNumber, int guess)
    {
        if (!PhotonNetwork.IsMasterClient || !roundInProgress) return;
        
        // Check if player is eliminated
        if (eliminatedPlayers.Contains(playerActorNumber)) return;
        
        // Store the guess
        currentRoundGuesses[playerActorNumber] = guess;
        
        Debug.Log($"Received guess from player {playerActorNumber}: {guess}");
        
        // Check if all active players have submitted
        int activePlayers = GetActivePlayerCount();
        if (currentRoundGuesses.Count >= activePlayers)
        {
            Debug.Log("All players have submitted their guesses!");
            ProcessRoundResults();
        }
    }
    
    void ProcessRoundResults()
    {
        if (!PhotonNetwork.IsMasterClient || !roundInProgress) return;
        
        roundInProgress = false;
        
        Debug.Log("Processing round results...");
        
        // Calculate average and target
        if (currentRoundGuesses.Count > 0)
        {
            lastRoundAverage = (float)currentRoundGuesses.Values.Average();
            lastRoundTarget = lastRoundAverage * 0.8f;
            
            // Find closest guess to target
            int closestPlayerActorNumber = -1;
            float closestDistance = float.MaxValue;
            
            foreach (var kvp in currentRoundGuesses)
            {
                float distance = Mathf.Abs(kvp.Value - lastRoundTarget);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayerActorNumber = kvp.Key;
                }
            }
            
            lastRoundWinner = closestPlayerActorNumber;
            
            // Find player to eliminate (furthest from target)
            int eliminatePlayerActorNumber = -1;
            float furthestDistance = 0f;
            
            foreach (var kvp in currentRoundGuesses)
            {
                float distance = Mathf.Abs(kvp.Value - lastRoundTarget);
                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                    eliminatePlayerActorNumber = kvp.Key;
                }
            }
            
            lastRoundEliminated = eliminatePlayerActorNumber;
            
            // Eliminate the player
            if (eliminatePlayerActorNumber != -1)
            {
                eliminatedPlayers.Add(eliminatePlayerActorNumber);
                
                // Find and eliminate the player object
                PlayerController[] players = FindObjectsOfType<PlayerController>();
                foreach (PlayerController player in players)
                {
                    if (player.GetActorNumber == eliminatePlayerActorNumber)
                    {
                        player.Eliminate();
                        break;
                    }
                }
            }
        }
        
        // Send results to all clients
        photonView.RPC("OnRoundResults", RpcTarget.All, lastRoundAverage, lastRoundTarget, lastRoundWinner, lastRoundEliminated);
        
        // Check win condition
        Invoke(nameof(CheckWinCondition), resultDisplayTime);
    }
    
    [PunRPC]
    void OnRoundResults(float average, float target, int winnerActorNumber, int eliminatedActorNumber)
    {
        lastRoundAverage = average;
        lastRoundTarget = target;
        lastRoundWinner = winnerActorNumber;
        lastRoundEliminated = eliminatedActorNumber;
        
        // Display results
        if (resultText != null)
        {
            string resultsMessage = $"Round {currentRound} Results:\n" +
                                  $"Average: {average:F2}\n" +
                                  $"Target (80%): {target:F2}\n" +
                                  $"Winner: Player {winnerActorNumber}\n" +
                                  $"Eliminated: Player {eliminatedActorNumber}";
                                  
            resultText.text = resultsMessage;
        }
        
        // Disable input during results
        if (guessInput != null)
            guessInput.interactable = false;
        if (submitButton != null)
            submitButton.interactable = false;
            
        Debug.Log($"Round results - Average: {average:F2}, Target: {target:F2}, Winner: {winnerActorNumber}, Eliminated: {eliminatedActorNumber}");
    }
    
    void CheckWinCondition()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        int activePlayers = GetActivePlayerCount();
        
        if (activePlayers <= 1)
        {
            // Game over! Find the winner
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            PlayerController winner = null;
            
            foreach (PlayerController player in players)
            {
                if (!eliminatedPlayers.Contains(player.GetActorNumber))
                {
                    winner = player;
                    break;
                }
            }
            
            if (winner != null)
            {
                photonView.RPC("OnGameEnded", RpcTarget.All, winner.GetActorNumber, winner.GetWalletAddress);
            }
        }
        else
        {
            // Continue to next round
            StartNewRound();
        }
    }
    
    [PunRPC]
    void OnGameEnded(int winnerActorNumber, string winnerWallet)
    {
        gameInProgress = false;
        roundInProgress = false;
        
        if (resultText != null)
        {
            resultText.text = $"🎉 GAME OVER! 🎉\n" +
                             $"Winner: {winnerWallet}\n" +
                             $"Congratulations!";
        }
        
        Debug.Log($"Game ended! Winner: {winnerWallet} (Actor: {winnerActorNumber})");
        
        // Here you would trigger blockchain settlement
        TriggerBlockchainSettlement(winnerWallet);
    }
    
    void TriggerBlockchainSettlement(string winnerWallet)
    {
        // This will be implemented later with your blockchain integration
        Debug.Log($"Triggering blockchain settlement for winner: {winnerWallet}");
        
        // For now, just log
        BlockchainManager blockchainManager = FindObjectOfType<BlockchainManager>();
        if (blockchainManager != null)
        {
            // blockchainManager.SettleGame(winnerWallet);
        }
    }
    
    void OnSubmitButtonClicked()
    {
        if (!roundInProgress || eliminatedPlayers.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
            return;
            
        if (guessInput != null && !string.IsNullOrEmpty(guessInput.text))
        {
            if (int.TryParse(guessInput.text, out int guess))
            {
                // Validate guess based on current round rules
                if (IsValidGuess(guess))
                {
                    // Find our player controller and submit guess
                    PlayerController[] players = FindObjectsOfType<PlayerController>();
                    foreach (PlayerController player in players)
                    {
                        if (player.photonView.IsMine)
                        {
                            player.SubmitGuess(guess);
                            break;
                        }
                    }
                }
                else
                {
                    if (resultText != null)
                        resultText.text = "Invalid guess! Check the rules.";
                }
            }
            else
            {
                if (resultText != null)
                    resultText.text = "Please enter a valid number.";
            }
        }
    }
    
    bool IsValidGuess(int guess)
    {
        // Basic validation: 0-100
        if (guess < 0 || guess > 100) return false;
        
        // Round-specific rules
        if (currentRound == 2)
        {
            // Rule 2: Only even numbers
            return guess % 2 == 0;
        }
        else if (currentRound == 3)
        {
            // Rule 3: No numbers ending in 0
            return guess % 10 != 0;
        }
        
        return true; // Round 1 has no special rules
    }
    
    int GetActivePlayerCount()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount - eliminatedPlayers.Count;
    }
    
    void UpdateRoundUI()
    {
        if (roundText != null)
            roundText.text = gameInProgress ? $"Round {currentRound}" : "Waiting to Start";
            
        if (instructionText != null)
        {
            if (gameInProgress && currentRound > 0 && currentRound <= roundInstructions.Length)
                instructionText.text = roundInstructions[currentRound - 1];
            else
                instructionText.text = "Wait for the game to start...";
        }
    }
    
    void SetGameUIActive(bool active)
    {
        if (guessInput != null)
            guessInput.gameObject.SetActive(active);
        if (submitButton != null)
            submitButton.gameObject.SetActive(active);
    }
    
    // Public getters for debugging
    public bool IsGameInProgress => gameInProgress;
    public bool IsRoundInProgress => roundInProgress;
    public int CurrentRound => currentRound;
    public float RoundTimeRemaining => roundTimer;
}