import { ConnectButton } from "@rainbow-me/rainbowkit";
import Link from "next/link";

export default function Home() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-900 to-black flex flex-col items-center justify-center gap-12">
      <h1 className="text-4xl md:text-6xl lg:text-8xl font-bold text-white tracking-wider font-[CSCalebMono]">
        Cards of Fortune
      </h1>
      <Link
        href="/game/keynesian-beauty-contest"
        className="px-8 py-3 bg-white/10 hover:bg-white/20 border border-white/20 rounded-full text-white font-[CSCalebMono] transition-all duration-300 hover:scale-105"
      >
        Enter the Game
      </Link>
      <ConnectButton />
    </div>
  );
}
