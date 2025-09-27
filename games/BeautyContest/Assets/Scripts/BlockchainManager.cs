using UnityEngine;
using UnityEngine.UI;
using System.Numerics;
using System.Threading.Tasks;
using TMPro;

public class BlockchainManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI walletAddressText;
    public TextMeshProUGUI statusText;
    public Button testButton;

    [Header("Blockchain Settings")]
    [Tooltip("Contract address loaded from DeveloperSettings.cs")]
    public string contractAddress = DeveloperSettings.CONTRACT_ADDRESS;

#if UNITY_EDITOR || UNITY_STANDALONE
    // -----------------------------
    // Runs only in Editor/Desktop
    // -----------------------------
    [Header("Development Configuration (Editor Only)")]
    [Tooltip("These values are loaded from DeveloperSettings.cs")]
    private string rpcURL = DeveloperSettings.RPC_URL;
    
    [Tooltip("Private key loaded from DeveloperSettings.cs")]
    private string privateKey = DeveloperSettings.PRIVATE_KEY;

    private string abi = @"[
      {
        ""inputs"":[],
        ""name"":""getNumber"",
        ""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],
        ""stateMutability"":""view"",
        ""type"":""function""
      },
      {
        ""inputs"":[{""internalType"":""uint256"",""name"":""num"",""type"":""uint256""}],
        ""name"":""setNumber"",
        ""outputs"":[],
        ""stateMutability"":""nonpayable"",
        ""type"":""function""
      },
      {
        ""inputs"":[],
        ""name"":""storedNumber"",
        ""outputs"":[{""internalType"":""uint256"",""name"":"""",""type"":""uint256""}],
        ""stateMutability"":""view"",
        ""type"":""function""
      }
    ]";

    private Nethereum.Web3.Web3 web3;
    private Nethereum.Contracts.Contract contract;

    void Start()
    {
        InitializeBlockchain();
        
        if (testButton != null)
        {
            testButton.onClick.AddListener(() => StartCoroutine(TestBlockchainCoroutine()));
        }
    }

    private void InitializeBlockchain()
    {
        try
        {
            UpdateStatus("Initializing blockchain connection...");
            
            // Set up web3 with account (chainId 11155111 = Sepolia)
            var account = new Nethereum.Web3.Accounts.Account(privateKey, 11155111);
            web3 = new Nethereum.Web3.Web3(account, rpcURL);

            // Load contract
            contract = web3.Eth.GetContract(abi, contractAddress);

            // Display wallet address
            string walletAddress = web3.TransactionManager.Account.Address;
            UpdateWalletAddress(walletAddress);
            
            UpdateStatus("Connected to Sepolia testnet");
            Debug.Log("Unity Editor connected to blockchain. Wallet: " + walletAddress);
        }
        catch (System.Exception ex)
        {
            UpdateStatus("Connection failed: " + ex.Message);
            Debug.LogError("Blockchain connection error: " + ex.Message);
        }
    }

    private System.Collections.IEnumerator TestBlockchainCoroutine()
    {
        var task = TestBlockchain();
        yield return new WaitUntil(() => task.IsCompleted);
        
        if (task.IsFaulted)
        {
            Debug.LogError("Test failed: " + task.Exception?.GetBaseException().Message);
            UpdateStatus("Test failed");
        }
    }

    private async Task TestBlockchain()
    {
        try
        {
            UpdateStatus("Testing blockchain interaction...");
            
            // Test: write a number
            await SetNumber(42);
            
            // Test: read back the number
            var value = await GetNumber();
            UpdateStatus($"Contract value: {value}");
            Debug.Log("Unity Editor read from contract: " + value);
        }
        catch (System.Exception ex)
        {
            UpdateStatus("Test failed: " + ex.Message);
            Debug.LogError("Test error: " + ex.Message);
        }
    }

    private async Task SetNumber(int num)
    {
        var setFunction = contract.GetFunction("setNumber");

        // Estimate gas with proper parameters
        var gas = await setFunction.EstimateGasAsync(
            from: web3.TransactionManager.Account.Address,
            gas: null,
            value: null,
            functionInput: num
        );

        // Send transaction
        string txnHash = await setFunction.SendTransactionAsync(
            from: web3.TransactionManager.Account.Address,
            gas: gas,
            value: null,
            functionInput: num
        );

        Debug.Log("Unity Editor Transaction Hash: " + txnHash);
    }

    private async Task<BigInteger> GetNumber()
    {
        var getFunction = contract.GetFunction("getNumber");
        return await getFunction.CallAsync<BigInteger>();
    }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    // -----------------------------
    // Runs only in WebGL builds
    // -----------------------------
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void connectWallet();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void unitySendTx(string funcName, string argsJson);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void unityCall(string funcName, string argsJson);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void getWalletAddress();

    void Start()
    {
        UpdateStatus("WebGL build - waiting for wallet info...");
        
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestBlockchainWebGL);
        }
        
        // Don't request wallet connection - wait for it to be passed from the web page
        // The GameWrapper will call SetWalletAddress and InitializeBlockchain
    }

    // Called from GameWrapper to set the wallet address from the main page
    public void SetWalletAddress(string address)
    {
        Debug.Log("Received wallet address from web page: " + address);
        UpdateWalletAddress(address);
        UpdateStatus("Wallet connected: " + address.Substring(0, 6) + "...");
    }

    // Called from GameWrapper to initialize blockchain connection
    public void InitializeBlockchain()
    {
        Debug.Log("Initializing blockchain for WebGL...");
        UpdateStatus("Blockchain initialized - ready for interactions");
        
        // Request wallet address to confirm connection
        getWalletAddress();
    }

    public void TestBlockchainWebGL()
    {
        UpdateStatus("Testing blockchain via WebGL...");
        CallSetNumber(42);
    }

    public void CallSetNumber(int value)
    {
        unitySendTx("setNumber", $"[{value}]");
    }

    public void CallGetNumber()
    {
        unityCall("getNumber", "[]");
    }

    public void RequestWalletAddress()
    {
        getWalletAddress();
    }

    // Can be called from web page to test communication
    public void TestConnection(string message)
    {
        Debug.Log("Test connection received: " + message);
        UpdateStatus("Connection test: " + message);
    }

    // Callbacks from JavaScript
    public void OnWalletConnected(string walletAddress)
    {
        UpdateWalletAddress(walletAddress);
        UpdateStatus("Wallet connected");
        Debug.Log("WebGL wallet connected: " + walletAddress);
    }

    public void OnWalletDisconnected()
    {
        UpdateWalletAddress("Not connected");
        UpdateStatus("Wallet disconnected");
        Debug.Log("WebGL wallet disconnected");
    }

    public void OnTxComplete(string txHash)
    {
        UpdateStatus("Transaction completed");
        Debug.Log("WebGL Tx completed: " + txHash);
        // After transaction, read the value
        CallGetNumber();
    }

    public void OnCallComplete(string value)
    {
        UpdateStatus($"Contract value: {value}");
        Debug.Log("WebGL chain value: " + value);
    }

    public void OnError(string error)
    {
        UpdateStatus("Error: " + error);
        Debug.LogError("WebGL error: " + error);
    }
#endif

    // Common methods for both platforms
    private void UpdateWalletAddress(string address)
    {
        if (walletAddressText != null)
        {
            walletAddressText.text = "Wallet: " + address;
        }
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
        Debug.Log("Status: " + status);
    }
    
    // Public method to get wallet address for other scripts
    public string GetWalletAddress()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (web3?.TransactionManager?.Account?.Address != null)
        {
            return web3.TransactionManager.Account.Address;
        }
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
        return currentWalletAddress;
#endif
        return "Not Connected";
    }
}