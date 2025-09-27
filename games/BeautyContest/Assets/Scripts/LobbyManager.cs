using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPun, IConnectionCallbacks, IMatchmakingCallbacks, ILobbyCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI connectionStatusText;
    public TextMeshProUGUI roomInfoText;
    public Button startGameButton;
    
    [Header("Player Spawning")]
    public Transform playerPanelContainer;
    public GameObject playerPrefab;
    
    [Header("Game Settings")]
    public int maxPlayersPerRoom = 5;
    public int minPlayersToStart = 2;
    
    private bool isConnectingToPhoton = false;
    private Dictionary<int, GameObject> spawnedPlayerObjects = new Dictionary<int, GameObject>();
    
    void Start()
    {
        // Initialize UI
        UpdateConnectionStatus("Initializing...");
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
            
        // Add this component to Photon callbacks
        PhotonNetwork.AddCallbackTarget(this);
        
        ConnectToPhoton();
    }
    
    // Manual method for testing - call this if automatic lobby join fails
    [ContextMenu("Force Join Lobby")]
    public void ForceJoinLobby()
    {
        Debug.Log("🔧 MANUAL: Forcing lobby join...");
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log($"Current State: {PhotonNetwork.NetworkClientState}");
            Debug.Log($"InLobby: {PhotonNetwork.InLobby}");
            
            if (PhotonNetwork.InLobby)
            {
                Debug.Log("🎯 Already in lobby - manually triggering OnJoinedLobby logic...");
                OnJoinedLobby(); // Manually call the callback
            }
            else
            {
                PhotonNetwork.JoinLobby();
            }
        }
        else
        {
            Debug.LogWarning("Not connected to Photon - cannot join lobby");
        }
    }
    
    void OnDestroy()
    {
        // Remove from Photon callbacks when destroyed
        PhotonNetwork.RemoveCallbackTarget(this);
        
        // Cancel periodic sync
        CancelInvoke(nameof(PeriodicRoomSync));
    }
    
    void PeriodicRoomSync()
    {
        if (PhotonNetwork.InRoom)
        {
            // Check if player count mismatch
            int actualPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int shownPlayers = spawnedPlayerObjects.Count;
            
            Debug.Log($"🔄 Periodic sync - Actual players: {actualPlayers}, Shown prefabs: {shownPlayers}");
            
            // Always force UI updates during periodic sync
            UpdateRoomInfo();
            UpdateStartButton();
            
            if (actualPlayers != shownPlayers)
            {
                Debug.LogWarning($"🚨 Player count mismatch! Actual: {actualPlayers}, Shown: {shownPlayers}. Refreshing...");
                RefreshAllPlayerRepresentations();
            }
            
            // Check if UI text is showing correct values
            if (roomInfoText != null)
            {
                string currentText = roomInfoText.text;
                if (!currentText.Contains($"Players: {actualPlayers}"))
                {
                    Debug.LogWarning($"🚨 UI text mismatch! Text shows: '{currentText}', should show {actualPlayers} players");
                    // Force another UI update
                    StartCoroutine(ForceUIUpdateWithDelay());
                }
            }
        }
    }
    
    void ConnectToPhoton()
    {
        if (isConnectingToPhoton) return;
        
        isConnectingToPhoton = true;
        UpdateConnectionStatus("Connecting to Photon...");
        
        // Cancel any existing timeouts
        CancelInvoke(nameof(CheckConnectionTimeout));
        
        // Set up Photon settings
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.SendRate = 20;
        PhotonNetwork.SerializationRate = 15;
        PhotonNetwork.EnableCloseConnection = true; // Help with connection stability
        
        // Try to disconnect first if already connected
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already connected, disconnecting first...");
            PhotonNetwork.Disconnect();
            return; // OnDisconnected will call ConnectToPhoton again
        }
        
        // Add connection timeout
        Debug.Log($"Connecting to Photon Cloud...");
        Debug.Log($"PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"PhotonNetwork.NetworkClientState: {PhotonNetwork.NetworkClientState}");
        
        // Connect to Photon
        bool connectionResult = PhotonNetwork.ConnectUsingSettings();
        Debug.Log($"ConnectUsingSettings result: {connectionResult}");
        
        if (!connectionResult)
        {
            Debug.LogError("Failed to start connection to Photon!");
            UpdateConnectionStatus("Connection failed to start");
            isConnectingToPhoton = false;
        }
        else
        {
            // Set timeout for connection (increased to 15 seconds)
            Invoke(nameof(CheckConnectionTimeout), 15f);
        }
    }
    
    void CheckConnectionTimeout()
    {
        if (isConnectingToPhoton && !PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning($"Connection to Photon timed out. Current state: {PhotonNetwork.NetworkClientState}");
            UpdateConnectionStatus("Connection timeout - retrying...");
            
            // Force disconnect and retry
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            else
            {
                isConnectingToPhoton = false;
                Invoke(nameof(ConnectToPhoton), 3f);
            }
        }
        else if (isConnectingToPhoton && PhotonNetwork.IsConnectedAndReady)
        {
            // Connection successful, cancel connecting state
            isConnectingToPhoton = false;
            Debug.Log("Connection successful, timeout cancelled");
        }
    }
    
    #region Photon Callbacks
    
    public void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        Debug.Log($"Photon Server: {PhotonNetwork.CloudRegion}");
        Debug.Log($"App Version: {PhotonNetwork.AppVersion}");
        Debug.Log($"Player Count Online: {PhotonNetwork.CountOfPlayersOnMaster}");
        Debug.Log($"Rooms Online: {PhotonNetwork.CountOfRooms}");
        
        isConnectingToPhoton = false; // Mark connection as successful
        CancelInvoke(nameof(CheckConnectionTimeout)); // Cancel timeout
        
        UpdateConnectionStatus("Connected! Joining lobby...");
        
        // Add a small delay before joining lobby to ensure connection is stable
        Invoke(nameof(JoinLobbyDelayed), 0.5f);
    }
    
    void JoinLobbyDelayed()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Attempting to join lobby...");
            Debug.Log($"Current Network State: {PhotonNetwork.NetworkClientState}");
            Debug.Log($"IsConnectedAndReady: {PhotonNetwork.IsConnectedAndReady}");
            Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
            
            PhotonNetwork.JoinLobby();
            
            // Add timeout for lobby join (15 seconds)
            Invoke(nameof(CheckLobbyJoinTimeout), 15f);
        }
        else
        {
            Debug.LogWarning("Cannot join lobby - not connected and ready");
            Debug.LogWarning($"NetworkClientState: {PhotonNetwork.NetworkClientState}");
            UpdateConnectionStatus("Connection unstable - retrying...");
            ConnectToPhoton();
        }
    }
    
    void CheckLobbyJoinTimeout()
    {
        if (!PhotonNetwork.InLobby)
        {
            Debug.LogError("Failed to join lobby within timeout period!");
            Debug.LogError($"Current state: {PhotonNetwork.NetworkClientState}");
            UpdateConnectionStatus("Lobby join timeout - retrying connection...");
            
            // Disconnect and reconnect
            PhotonNetwork.Disconnect();
        }
        else
        {
            Debug.Log("Lobby join successful - timeout cancelled");
        }
    }
    
    public void OnJoinedLobby()
    {
        Debug.Log("✅ SUCCESS: Joined Photon Lobby!");
        Debug.Log($"Lobby Type: {PhotonNetwork.CurrentLobby}");
        Debug.Log($"InLobby: {PhotonNetwork.InLobby}");
        Debug.Log($"CountOfRooms: {PhotonNetwork.CountOfRooms}");
        
        // Cancel lobby join timeout
        CancelInvoke(nameof(CheckLobbyJoinTimeout));
        
        UpdateConnectionStatus("In lobby. Finding room...");
        
        // Try to join existing room or create new one
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };
        
        Debug.Log($"Attempting to join/create room: BeautyContest_Room");
        PhotonNetwork.JoinOrCreateRoom("BeautyContest_Room", roomOptions, TypedLobby.Default);
    }
    
    public void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
        
        // Set up player nickname with wallet address
        SetupPlayerNickname();
        
        UpdateConnectionStatus($"In room with {PhotonNetwork.CurrentRoom.PlayerCount} players");
        UpdateRoomInfo();
        
        // Spawn player representations for ALL players in the room (including existing ones)
        SpawnAllPlayerPrefabs();
        
        // Update start button
        UpdateStartButton();
        
        // Start periodic sync to catch missed callbacks
        InvokeRepeating(nameof(PeriodicRoomSync), 2f, 3f); // Check every 3 seconds after 2 second delay
    }
    
    void SetupPlayerNickname()
    {
        // Generate a more unique identifier for WebGL builds
        // Use combination of timestamp, random number, and device identifier
        string timestamp = System.DateTime.Now.Ticks.ToString();
        string randomId = UnityEngine.Random.Range(10000, 99999).ToString();
        string systemId = SystemInfo.deviceUniqueIdentifier.Substring(0, System.Math.Min(8, SystemInfo.deviceUniqueIdentifier.Length));
        string walletAddress = $"Player_{systemId}_{randomId}_{timestamp.Substring(timestamp.Length - 6)}";
        
        // Try to get wallet from BlockchainManager
        BlockchainManager blockchainManager = FindObjectOfType<BlockchainManager>();
        if (blockchainManager != null)
        {
            string wallet = blockchainManager.GetWalletAddress();
            if (wallet != "Not Connected")
                walletAddress = wallet;
        }
        
        // Set player name
        PhotonNetwork.LocalPlayer.NickName = walletAddress;
        Debug.Log($"Set local player nickname to: {walletAddress}");
    }
    
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"🎉 [ALL CLIENTS] Player {newPlayer.NickName} joined the room (Actor: {newPlayer.ActorNumber})");
        Debug.Log($"🎯 Current room player count: {PhotonNetwork.CurrentRoom.PlayerCount}");
        Debug.Log($"📊 Players in room: {string.Join(", ", PhotonNetwork.CurrentRoom.Players.Keys)}");
        
        // Force refresh all player representations
        RefreshAllPlayerRepresentations();
        
        // Force UI updates with delay to ensure proper refresh
        StartCoroutine(ForceUIUpdateWithDelay());
        
        Debug.Log($"✅ OnPlayerEnteredRoom completed for {newPlayer.NickName}");
    }
    
    System.Collections.IEnumerator ForceUIUpdateWithDelay()
    {
        // Wait a frame to ensure all networking updates are processed
        yield return new WaitForEndOfFrame();
        
        Debug.Log($"🔄 Force UI update - Player count: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        UpdateRoomInfo();
        UpdateStartButton();
        
        // Force another update after a small delay
        yield return new WaitForSeconds(0.1f);
        
        UpdateRoomInfo();
        UpdateStartButton();
        
        Debug.Log($"✅ Force UI update completed");
    }
    
    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.NickName} left the room");
        
        // Remove their visual representation
        if (spawnedPlayerObjects.ContainsKey(otherPlayer.ActorNumber))
        {
            if (spawnedPlayerObjects[otherPlayer.ActorNumber] != null)
                Destroy(spawnedPlayerObjects[otherPlayer.ActorNumber]);
            spawnedPlayerObjects.Remove(otherPlayer.ActorNumber);
        }
        
        UpdateRoomInfo();
        UpdateStartButton();
    }
    
    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Disconnected from Photon: {cause}");
        UpdateConnectionStatus($"Disconnected: {cause}");
        isConnectingToPhoton = false;
        
        // Cancel any pending timeouts
        CancelInvoke(nameof(CheckConnectionTimeout));
        CancelInvoke(nameof(JoinLobbyDelayed));
        
        // Handle different disconnect causes with appropriate retry strategies
        switch (cause)
        {
            case DisconnectCause.ServerTimeout:
            case DisconnectCause.DisconnectByServerLogic:
                Debug.LogWarning("Server timeout detected - retrying with longer delay...");
                Invoke(nameof(ConnectToPhoton), 5f); // Longer delay for server issues
                break;
                
            case DisconnectCause.DisconnectByClientLogic:
                Debug.Log("Client disconnect - retrying normally...");
                Invoke(nameof(ConnectToPhoton), 2f);
                break;
                
            case DisconnectCause.ApplicationQuit:
                Debug.Log("Application quitting - not retrying");
                return; // Don't retry on application quit
                
            default:
                Debug.Log("Other disconnect cause - retrying...");
                Invoke(nameof(ConnectToPhoton), 3f);
                break;
        }
    }
    
    // Required interface methods (empty implementations)
    public void OnConnected() { Debug.Log("OnConnected called"); }
    public void OnRegionListReceived(RegionHandler regionHandler) { Debug.Log("OnRegionListReceived called"); }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { Debug.Log("OnCustomAuthenticationResponse called"); }
    public void OnCustomAuthenticationFailed(string debugMessage) { Debug.LogError($"Custom auth failed: {debugMessage}"); }
    public void OnFriendListUpdate(List<FriendInfo> friendList) { Debug.Log("OnFriendListUpdate called"); }
    
    // Room creation/join callbacks
    public void OnCreatedRoom() { Debug.Log("✅ OnCreatedRoom: Successfully created room"); }
    public void OnCreateRoomFailed(short returnCode, string message) 
    { 
        Debug.LogError($"❌ Create room failed: Code {returnCode}, Message: {message}"); 
        UpdateConnectionStatus($"Failed to create room: {message}");
    }
    public void OnJoinRoomFailed(short returnCode, string message) 
    { 
        Debug.LogError($"❌ Join room failed: Code {returnCode}, Message: {message}"); 
        UpdateConnectionStatus($"Failed to join room: {message}");
    }
    
    // Lobby callbacks  
    public void OnLeftLobby() 
    { 
        Debug.Log("OnLeftLobby called"); 
        UpdateConnectionStatus("Left lobby");
    }
    public void OnRoomListUpdate(List<RoomInfo> roomList) 
    { 
        Debug.Log($"OnRoomListUpdate: {roomList.Count} rooms available"); 
        foreach(var room in roomList)
        {
            Debug.Log($"Room: {room.Name}, Players: {room.PlayerCount}/{room.MaxPlayers}");
        }
    }
    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) 
    { 
        Debug.Log("OnLobbyStatisticsUpdate called"); 
        foreach(var lobby in lobbyStatistics)
        {
            Debug.Log($"Lobby: {lobby.Name}, Rooms: {lobby.RoomCount}, Players: {lobby.PlayerCount}");
        }
    }
    public void OnJoinRandomFailed(short returnCode, string message) { Debug.LogError($"Join random failed: Code {returnCode}, Message: {message}"); }
    public void OnLeftRoom() { Debug.Log("OnLeftRoom: Left the room"); }
    
    #endregion
    
    void RefreshAllPlayerRepresentations()
    {
        Debug.Log($"🔄 Refreshing all player representations. Current count: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        // Clear existing and respawn all
        SpawnAllPlayerPrefabs();
        
        Debug.Log($"✅ Player representation refresh completed. Showing {spawnedPlayerObjects.Count} players");
    }
    
    void SpawnAllPlayerPrefabs()
    {
        Debug.Log("🎮 Spawning player prefabs for all players in room...");
        
        // Clear existing player objects
        foreach (var playerObj in spawnedPlayerObjects.Values)
        {
            if (playerObj != null)
                Destroy(playerObj);
        }
        spawnedPlayerObjects.Clear();
        
        // Spawn prefabs for all players currently in the room
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            SpawnPlayerPrefabForPlayer(player);
        }
    }
    
    void SpawnPlayerPrefabForPlayer(Player player)
    {
        if (playerPrefab == null || playerPanelContainer == null)
        {
            Debug.LogError("Player prefab or container not assigned!");
            return;
        }
        
        // Don't spawn if already exists
        if (spawnedPlayerObjects.ContainsKey(player.ActorNumber))
        {
            Debug.Log($"Player {player.ActorNumber} prefab already exists");
            return;
        }
        
        Debug.Log($"Spawning prefab for Player {player.ActorNumber}: {player.NickName}");
        
        // Instantiate the player prefab
        GameObject playerObj = Instantiate(playerPrefab, playerPanelContainer);
        
        // Store reference
        spawnedPlayerObjects[player.ActorNumber] = playerObj;
        
        // Get the PlayerController and initialize it with the player data
        PlayerController playerController = playerObj.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Set up the player controller with the correct player info
            playerController.InitializeWithPlayer(player);
        }
        else
        {
            Debug.LogWarning($"PlayerController not found on spawned prefab for {player.NickName}");
        }
        
        Debug.Log($"✅ Spawned player representation for {player.NickName} (Actor: {player.ActorNumber})");
    }
    
    void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = $"Status: {status}";
            
        Debug.Log($"Connection Status: {status}");
    }
    
    void UpdateRoomInfo()
    {
        if (roomInfoText != null && PhotonNetwork.InRoom)
        {
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            string roomName = PhotonNetwork.CurrentRoom.Name;
            
            string roomText = $"Room: {roomName}\nPlayers: {currentPlayers}/{maxPlayers}";
            string previousText = roomInfoText.text;
            
            roomInfoText.text = roomText;
            
            // Force UI refresh
            roomInfoText.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            
            Debug.Log($"📊 UpdateRoomInfo - Previous: '{previousText}' → New: '{roomText}'");
            Debug.Log($"🔍 Actual displayed text: '{roomInfoText.text}'");
            Debug.Log($"🎯 Real player count: {currentPlayers}, Spawned prefabs: {spawnedPlayerObjects.Count}");
        }
        else
        {
            Debug.LogWarning($"❌ UpdateRoomInfo failed - roomInfoText: {roomInfoText != null}, InRoom: {PhotonNetwork.InRoom}");
        }
    }
    
    void UpdateStartButton()
    {
        if (startGameButton != null)
        {
            bool canStart = PhotonNetwork.IsMasterClient && 
                           PhotonNetwork.CurrentRoom.PlayerCount >= minPlayersToStart;
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            
            TextMeshProUGUI buttonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
            string previousButtonText = buttonText != null ? buttonText.text : "NULL";
            
            startGameButton.interactable = canStart;
            
            string newButtonText;
            if (canStart)
                newButtonText = "Start Game";
            else if (!PhotonNetwork.IsMasterClient)
                newButtonText = "Waiting for Host...";
            else
                newButtonText = $"Need {minPlayersToStart} Players";
                
            if (buttonText != null)
            {
                buttonText.text = newButtonText;
                
                // Force UI refresh
                buttonText.SetAllDirty();
                Canvas.ForceUpdateCanvases();
            }
            
            Debug.Log($"🎮 UpdateStartButton - IsMasterClient: {PhotonNetwork.IsMasterClient}");
            Debug.Log($"🎮 Players: {currentPlayers}/{minPlayersToStart}, CanStart: {canStart}");
            Debug.Log($"🎮 Button text - Previous: '{previousButtonText}' → New: '{newButtonText}'");
            Debug.Log($"🎮 Actual button text: '{(buttonText != null ? buttonText.text : "NULL")}'" );
        }
        else
        {
            Debug.LogWarning($"❌ UpdateStartButton failed - startGameButton is null");
        }
    }
    
    void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Notify GameManager to start the game
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
        
        // Hide lobby UI elements
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(false);
    }
    
    // Public method to get current player count
    public int GetPlayerCount()
    {
        return PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
    }
    
    // Public method to check if we can start
    public bool CanStartGame()
    {
        return PhotonNetwork.IsMasterClient && GetPlayerCount() >= minPlayersToStart;
    }
}