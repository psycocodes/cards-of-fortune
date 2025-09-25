using System;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

public class BlockchainTest : MonoBehaviour
{
    // Replace these with your actual details
    [Header("Configuration")]
    [Tooltip("RPC URL loaded from DeveloperSettings.cs")]
    private string rpcURL = DeveloperSettings.RPC_URL; 
    private string privateKey = DeveloperSettings.PRIVATE_KEY;
    private string contractAddress = "0x6a821521b3C6a3E6298E222082c927bFdE0ddA38"; 

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

    private Web3 web3;
    private Nethereum.Contracts.Contract contract;

    async void Start()
    {
        try
        {
            // Set up web3 with account (chainId 11155111 = Sepolia)
            var account = new Account(privateKey, 11155111);
            web3 = new Web3(account, rpcURL);

            // Load contract
            contract = web3.Eth.GetContract(abi, contractAddress);

            // Test: write a number
            await SetNumber(42);

            // Test: read back the number
            var value = await GetNumber();
            Debug.Log("Stored number on chain: " + value);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error: " + ex.Message);
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

        Debug.Log("Transaction Hash: " + txnHash);
    }

    private async Task<BigInteger> GetNumber()
    {
        var getFunction = contract.GetFunction("getNumber");
        return await getFunction.CallAsync<BigInteger>();
    }
}
