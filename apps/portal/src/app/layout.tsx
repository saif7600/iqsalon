import type { Metadata } from "next";
import { Manrope } from "next/font/google";
import { PortalRoot } from "@/components/portal-root";
import "./globals.css";
const font = Manrope({ subsets: ["latin"], variable: "--font-body" });
export const metadata: Metadata = {
  title: { default: "AtiqSalon AI Portal", template: "%s | AtiqSalon AI" },
  description: "Secure workspace for beauty and wellness businesses",
};
export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={font.variable}>
      <body>
        <PortalRoot>{children}</PortalRoot>
      </body>
    </html>
  );
}
