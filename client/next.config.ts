import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async headers() {
    return [
      {
        // Apply Cross-Origin headers to game pages - Unity needs specific settings
        source: '/game/:path*',
        headers: [
          {
            key: 'Cross-Origin-Opener-Policy',
            value: 'same-origin-allow-popups', // Better for Unity + wallet compatibility
          },
          {
            key: 'Cross-Origin-Embedder-Policy',
            value: 'credentialless', // Better for Unity WebGL
          },
        ],
      },
      {
        // Apply Cross-Origin headers to all Unity files
        source: '/games/:path*',
        headers: [
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
          {
            key: 'Cache-Control',
            value: 'no-cache, no-store, must-revalidate',
          },
        ],
      },
      {
        // Specific headers for Unity WebGL files
        source: '/games/:path*/Build/:path*',
        headers: [
          {
            key: 'Cross-Origin-Resource-Policy',
            value: 'cross-origin',
          },
          {
            key: 'Access-Control-Allow-Origin',
            value: '*',
          },
        ],
      },
      {
        // Handle Brotli compressed files (.br files)
        source: '/games/:path*/Build/:file*.br',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'br',
          },
          {
            key: 'Content-Type',
            value: 'application/octet-stream',
          },
        ],
      },
      {
        // Handle Unity WASM files specifically
        source: '/games/:path*/Build/:file*.wasm.br',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'br',
          },
          {
            key: 'Content-Type',
            value: 'application/wasm',
          },
        ],
      },
      {
        // Handle Unity data files
        source: '/games/:path*/Build/:file*.data.br',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'br',
          },
          {
            key: 'Content-Type',
            value: 'application/octet-stream',
          },
        ],
      },
      {
        // Handle Unity framework files
        source: '/games/:path*/Build/:file*.framework.js.br',
        headers: [
          {
            key: 'Content-Encoding',
            value: 'br',
          },
          {
            key: 'Content-Type',
            value: 'application/javascript',
          },
        ],
      },
      {
        // Handle Unity loader files (.js files)
        source: '/games/:path*/Build/:file*.js',
        headers: [
          {
            key: 'Content-Type',
            value: 'application/javascript',
          },
        ],
      },
    ];
  },
};

export default nextConfig;
