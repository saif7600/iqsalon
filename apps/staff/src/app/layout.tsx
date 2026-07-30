import type { Metadata, Viewport } from "next";
import { IBM_Plex_Mono, Space_Grotesk } from "next/font/google";
import { PwaRegistration } from "../components/pwa-registration";
import "./globals.css";

const display = Space_Grotesk({
  subsets: ["latin"],
  variable: "--font-display",
});
const mono = IBM_Plex_Mono({
  subsets: ["latin"],
  weight: ["400", "600"],
  variable: "--font-mono",
});

export const metadata: Metadata = {
  title: { default: "AtiqSalon Staff", template: "%s | AtiqSalon Staff" },
  description: "Focused daily operations for AtiqSalon staff.",
  applicationName: "AtiqSalon Staff",
  manifest: "/manifest.webmanifest",
  appleWebApp: {
    capable: true,
    title: "AtiqSalon Staff",
    statusBarStyle: "black-translucent",
  },
};

export const viewport: Viewport = {
  themeColor: "#102520",
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${display.variable} ${mono.variable}`}>
      <body>
        <PwaRegistration />
        {children}
      </body>
    </html>
  );
}
