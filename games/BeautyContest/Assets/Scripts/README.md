# Developer Setup Instructions

## First Time Setup

1. Copy `DeveloperSettings.cs.template` to `DeveloperSettings.cs`
2. Replace the placeholder values in `DeveloperSettings.cs` with your own:
   - `PRIVATE_KEY`: Your test private key (get test ETH from Sepolia faucet)
   - `RPC_URL`: Your Alchemy/Infura API URL
   - `CONTRACT_ADDRESS`: Should already be correct

## Important Notes

- **NEVER commit `DeveloperSettings.cs`** - it's in .gitignore for a reason
- Use only **test accounts** with minimal Sepolia ETH
- The template file (`DeveloperSettings.cs.template`) should be committed
- Each developer maintains their own `DeveloperSettings.cs` locally

## Getting Test ETH

Visit a Sepolia faucet:
- https://sepoliafaucet.com/
- https://www.alchemy.com/faucets/ethereum-sepolia
- https://faucet.quicknode.com/ethereum/sepolia