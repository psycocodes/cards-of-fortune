"use client";
import React from "react";
import { Unity, useUnityContext } from "react-unity-webgl";
import { Expand, LampDeskIcon } from "lucide-react";
import { isMobileDevice } from "@/utils/isMobile";


export default function GameWrapper({ gameId }: { gameId: string }) {
  const { unityProvider, requestFullscreen,isLoaded,
    loadingProgression } = useUnityContext({
    loaderUrl: `/games/${gameId}/Build.loader.js`,
    dataUrl: `/games/${gameId}/Build.data`,
    frameworkUrl: `/games/${gameId}/Build.framework.js`,
    codeUrl: `/games/${gameId}/Build.wasm`,
  });

  function handleFullscreen() {
    requestFullscreen(true);
  }

  function LoadingComponent() {
    const loadingPercentage = Math.round(loadingProgression * 100);
    
    if (!isLoaded) {
      return (<div className="absolute inset-0 z-20 flex flex-col items-center justify-center bg-black">
          <p className="text-lg font-semibold">Loading Game...</p>
          <div className="w-1/4 mt-2 h-2 bg-gray-700 rounded-full overflow-hidden">
            <div
              className="h-full bg-blue-500 transition-all duration-150"
              style={{ width: `${loadingPercentage}%` }}
            />
          </div>
          <p className="mt-2 text-sm">{loadingPercentage}%</p>
        </div>
      );
    }
  }

  function FullScreenButton() {
    if (isLoaded) {
      return (
        <button
          onClick={handleFullscreen}
          style={{
            position: 'absolute',
            bottom: '20px',
            right: '20px',
            padding: '10px 10px',
          }}
        >
          <Expand className="w-5 h-5"/>
        </button>
      );
    }
  }

  if (isMobileDevice()) {
    return (
      <div className="flex flex-col items-center justify-center h-screen bg-gray-900 text-white p-4">
        <LampDeskIcon className="w-16 h-16 mb-4 text-blue-400" />
        <h1 className="text-2xl font-bold text-center">Desktop Recommended</h1>
        <p className="mt-2 text-center text-gray-300 max-w-sm font-sans">
          This interactive experience is designed for a desktop browser. Please switch to a PC for the best performance.
        </p>
      </div>
    );
  }
  return (
    <div className="relative w-screen h-screen">
      <LoadingComponent />
      <Unity
        unityProvider={unityProvider}
        className={`w-full h-full transition-opacity duration-500 ${isLoaded ? "opacity-100" : "opacity-0"}`}
      />
      <FullScreenButton />
    </div>
  );
}
