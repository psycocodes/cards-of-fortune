import { getDefaultConfig } from '@rainbow-me/rainbowkit';
import {
  arbitrum,
  base,
  mainnet,
  optimism,
  polygon,
  sepolia,
} from 'wagmi/chains';

export const config = getDefaultConfig({
  appName: 'Cards of Fortune',
  projectId: process.env.NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID || '5350b694fda9da4232170790c07511ed',
  chains: [
    mainnet,
    sepolia, // Always include Sepolia for development
    polygon,
    optimism,
    arbitrum,
    base,
  ],
  ssr: true,
});
