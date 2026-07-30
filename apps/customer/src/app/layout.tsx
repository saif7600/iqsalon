import type { Metadata, Viewport } from "next";
import { Fraunces, Manrope } from "next/font/google";
import { PwaRegistration } from "../components/pwa-registration";
import "./globals.css";

const display = Fraunces({ subsets: ["latin"], variable: "--font-display" });
const body = Manrope({ subsets: ["latin"], variable: "--font-body" });

export const metadata: Metadata = {
  title: { default: "AtiqSalon", template: "%s | AtiqSalon" },
  description: "Your appointments and salon relationship in one place.",
  applicationName: "AtiqSalon",
  manifest: "/manifest.webmanifest",
  appleWebApp: { capable: true, title: "AtiqSalon", statusBarStyle: "default" },
};

export const viewport: Viewport = {
  themeColor: "#f2eee5",
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${display.variable} ${body.variable}`}>
      <body>
        <PwaRegistration />
        {children}
      </body>
    </html>
  );
}
