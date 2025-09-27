using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    public float resultDisplayTime = 10f; // Increased from 5f to 10f
    public float resultProcessingTime = 3f; // NEW: Time to process results before showing
    
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
        if (photonView != null)
        {
            photonView.RPC("OnNewRoundStarted", RpcTarget.All, currentRound);
        }
        else
        {
            OnNewRoundStarted(currentRound);
        }
        
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
        {
            if (isEliminated)
                resultText.text = "❌ You are eliminated - spectating...";
            else
                resultText.text = $"🎮 Round {round} started!\nMake your guess and submit!";
        }
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
        if (eliminatedPlayers.Contains(playerActorNumber)) 
        {
            Debug.LogWarning($"❌ Ignoring guess from eliminated player {playerActorNumber}");
            return;
        }
        
        // Store the guess
        currentRoundGuesses[playerActorNumber] = guess;
        
        Debug.Log($"📥 Received guess from player {playerActorNumber}: {guess}");
        
        // Update UI to show submission count
        int activePlayers = GetActivePlayerCount();
        if (resultText != null)
            resultText.text = $"Round {currentRound}: {currentRoundGuesses.Count}/{activePlayers} players submitted";
        
        Debug.Log($"📊 Current submissions: {currentRoundGuesses.Count}/{activePlayers}");
        
        // Check if all active players have submitted
        if (currentRoundGuesses.Count >= activePlayers)
        {
            Debug.Log("✅ All players have submitted their guesses!");
            roundInProgress = false; // Stop the timer
            
            // Brief pause to show "All submitted" then process
            if (resultText != null)
                resultText.text = "All players submitted! Processing results...";
                
            // Process results after brief delay
            Invoke(nameof(ProcessRoundResults), resultProcessingTime);
        }
    }
    
    void ProcessRoundResults()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        roundInProgress = false;
        
        Debug.Log($"🎯 Processing round {currentRound} results...");
        
        // Calculate average and target
        float sum = 0f;
        foreach (var guess in currentRoundGuesses.Values)
        {
            sum += guess;
        }
        
        lastRoundAverage = sum / currentRoundGuesses.Count;
        lastRoundTarget = lastRoundAverage * 0.8f; // 80% of average
        
        Debug.Log($"📊 Round {currentRound} Results:");
        Debug.Log($"📊 Average: {lastRoundAverage:F2}, Target: {lastRoundTarget:F2}");
        
        // Find closest player (winner)
        int closestPlayerActorNumber = -1;
        float closestDistance = float.MaxValue;
        
        foreach (var kvp in currentRoundGuesses)
        {
            float distance = Mathf.Abs(kvp.Value - lastRoundTarget);
            Debug.Log($"📊 Player {kvp.Key}: Guess={kvp.Value}, Distance={distance:F2}");
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayerActorNumber = kvp.Key;
            }
        }
        
        lastRoundWinner = closestPlayerActorNumber;
        
        // Find player to eliminate (furthest from target) - ONLY if more than 2 players
        int totalActivePlayers = GetActivePlayerCount();
        int eliminatePlayerActorNumber = -1;
        
        if (totalActivePlayers > 2)
        {
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
                        Debug.Log($"❌ Player {eliminatePlayerActorNumber} eliminated!");
                        break;
                    }
                }
            }
        }
        else if (totalActivePlayers == 2)
        {
            // With only 2 players, the game should end after this round
            // Winner is the one closest to target
            Debug.Log($"🏆 Final round! Winner: Player {closestPlayerActorNumber}");
            
            // End the game immediately
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            string winnerWallet = "";
            foreach (PlayerController player in players)
            {
                if (player.GetActorNumber == closestPlayerActorNumber)
                {
                    winnerWallet = player.GetWalletAddress;
                    break;
                }
            }
            
            // Send results first, then end game
            photonView.RPC("OnRoundResults", RpcTarget.All, lastRoundAverage, lastRoundTarget, lastRoundWinner, -1);
            
            // End game after showing results
            Invoke(nameof(EndGameWithWinner), resultDisplayTime);
            return;
        }
        
        // Send results to all clients
        photonView.RPC("OnRoundResults", RpcTarget.All, lastRoundAverage, lastRoundTarget, lastRoundWinner, lastRoundEliminated);
        
        // Check win condition after brief display
        Invoke(nameof(CheckWinCondition), resultDisplayTime);
    }

    // Helper method to end the game with a specific winner
    void EndGameWithWinner()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        gameInProgress = false;
        roundInProgress = false;

        // Find winner details
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        string winnerWallet = "";
        string winnerName = GetPlayerDisplayName(lastRoundWinner);

        foreach (PlayerController player in players)
        {
            if (player.GetActorNumber == lastRoundWinner)
            {
                winnerWallet = player.GetWalletAddress;
                break;
            }
        }

        Debug.Log($"🎉 GAME OVER! Winner: {winnerName} ({winnerWallet})");

        // Notify all clients about game end
        photonView.RPC("OnGameEnded", RpcTarget.All, lastRoundWinner, winnerWallet);
    }

    // Helper method to get player display name
    string GetPlayerDisplayName(int actorNumber)
    {
        if (actorNumber == -1) return "Unknown";

        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.GetActorNumber == actorNumber)
            {
                string displayName = player.GetPlayerDisplayName();
                return !string.IsNullOrEmpty(displayName) ? displayName : $"Player {actorNumber}";
            }
        }

        return $"Player {actorNumber}";
    }
    
    [PunRPC]
    void OnRoundResults(float average, float target, int winnerActorNumber, int eliminatedActorNumber)
    {
        lastRoundAverage = average;
        lastRoundTarget = target;
        lastRoundWinner = winnerActorNumber;
        lastRoundEliminated = eliminatedActorNumber;
        
        // Display results with better formatting
        if (resultText != null)
        {
            string winnerName = GetPlayerDisplayName(winnerActorNumber);
            string resultsMessage = $"🎯 ROUND {currentRound} RESULTS 🎯\n" +
                                  $"Average: {average:F1}\n" +
                                  $"Target (80%): {target:F1}\n" +
                                  $"🏆 Winner: {winnerName}";
                                  
            if (eliminatedActorNumber != -1)
            {
                string eliminatedName = GetPlayerDisplayName(eliminatedActorNumber);
                resultsMessage += $"\n❌ Eliminated: {eliminatedName}";
            }
            else
            {
                resultsMessage += $"\n🎉 FINAL WINNER: {winnerName}!";
            }
                                  
            resultText.text = resultsMessage;
        }
        
        // Disable input during results display
        if (guessInput != null)
            guessInput.interactable = false;
        if (submitButton != null)
            submitButton.interactable = false;
            
        Debug.Log($"📊 Round results displayed - Average: {average:F2}, Target: {target:F2}, Winner: {winnerActorNumber}, Eliminated: {eliminatedActorNumber}");
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
                if (photonView != null)
                {
                    photonView.RPC("OnGameEnded", RpcTarget.All, winner.GetActorNumber, winner.GetWalletAddress);
                }
                else
                {
                    OnGameEnded(winner.GetActorNumber, winner.GetWalletAddress);
                }
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
        
        string displayName = GetPlayerName(winnerActorNumber);
        
        if (resultText != null)
        {
            resultText.text = $"🎉 GAME OVER! 🎉\n" +
                             $"Winner: {displayName}\n" +
                             $"🏆 Congratulations! 🏆\n" +
                             $"Prize pool is being transferred...";
        }
        
        Debug.Log($"Game ended! Winner: {winnerWallet} (Actor: {winnerActorNumber})");
        
        // Trigger blockchain settlement
        TriggerBlockchainSettlement(winnerWallet);
    }
    
    void TriggerBlockchainSettlement(string winnerWallet)
    {
        Debug.Log($"🎉 GAME WINNER: {winnerWallet}");
        Debug.Log("Triggering blockchain payout...");
        
        BlockchainManager blockchainManager = FindObjectOfType<BlockchainManager>();
        if (blockchainManager != null)
        {
            StartCoroutine(TriggerPayoutCoroutine(blockchainManager, winnerWallet));
        }
        else
        {
            Debug.LogError("BlockchainManager not found! Winner payout cannot be processed.");
        }
    }
    
    System.Collections.IEnumerator TriggerPayoutCoroutine(BlockchainManager blockchainManager, string winnerWallet)
    {
        var task = blockchainManager.TriggerWinnerPayout(winnerWallet);
        yield return new WaitUntil(() => task.IsCompleted);
        
        if (task.IsFaulted)
        {
            Debug.LogError("Blockchain payout failed: " + task.Exception?.GetBaseException().Message);
        }
        else
        {
            Debug.Log("Blockchain payout completed successfully!");
        }
    }
    
    void OnSubmitButtonClicked()
    {
        if (!roundInProgress || eliminatedPlayers.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            if (resultText != null)
                resultText.text = eliminatedPlayers.Contains(PhotonNetwork.LocalPlayer.ActorNumber) 
                    ? "You are eliminated!" 
                    : "Game is not active right now.";
            return;
        }
            
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
                            
                            // Show confirmation
                            if (resultText != null)
                                resultText.text = $"✅ Guess submitted: {guess}\nWaiting for other players...";
                            
                            // Disable input after submission
                            if (guessInput != null)
                                guessInput.interactable = false;
                            if (submitButton != null)
                                submitButton.interactable = false;
                                
                            break;
                        }
                    }
                }
                else
                {
                    if (resultText != null)
                        resultText.text = GetValidationMessage(guess);
                }
            }
            else
            {
                if (resultText != null)
                    resultText.text = "❌ Please enter a valid number.";
            }
        }
        else
        {
            if (resultText != null)
                resultText.text = "❌ Please enter a guess first.";
        }
    }
    
    string GetValidationMessage(int guess)
    {
        if (guess < 0 || guess > 100)
            return "❌ Guess must be between 0-100!";
            
        if (currentRound == 2 && guess % 2 != 0)
            return "❌ Round 2: Only even numbers allowed!";
            
        if (currentRound == 3 && guess % 10 == 0)
            return "❌ Round 3: Numbers ending in 0 not allowed!";
            
        return "❌ Invalid guess! Check the rules.";
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
    
    string GetPlayerName(int actorNumber)
    {
        // Find player by actor number
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.ActorNumber == actorNumber)
            {
                string nickname = player.NickName;
                if (!string.IsNullOrEmpty(nickname))
                {
                    // Shorten long wallet addresses
                    if (nickname.Length > 12)
                        return nickname.Substring(0, 6) + "..." + nickname.Substring(nickname.Length - 4);
                    else
                        return nickname;
                }
                return $"Player {actorNumber}";
            }
        }
        return $"Player {actorNumber}";
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