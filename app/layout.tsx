import type { Metadata } from "next";
import localFont from "next/font/local";
import "./globals.css";

const calebMono = localFont({
  src: "../public/fonts/CSCalebMono-Regular.ttf",
  variable: "--font-caleb-mono",
});

export const metadata: Metadata = {
  title: "Cards of Fortune",
  description: "The Web3 game that decides your fate",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={`${calebMono.variable} antialiased`}>{children}</body>
    </html>
  );
}
