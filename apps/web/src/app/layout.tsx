import type { Metadata } from "next";
import { Cormorant_Garamond, Manrope } from "next/font/google";
import "./globals.css";
const display = Cormorant_Garamond({
  subsets: ["latin"],
  variable: "--font-display",
  weight: ["600", "700"],
});
const body = Manrope({ subsets: ["latin"], variable: "--font-body" });
export const metadata: Metadata = {
  title: { default: "AtiqSalon AI", template: "%s | AtiqSalon AI" },
  description: "Operating System for Beauty & Wellness Businesses",
  metadataBase: new URL("https://atiqsalon.ai"),
};
export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={`${display.variable} ${body.variable}`}>
      <body>
        <div className="announcement">
          Built for ambitious beauty and wellness businesses across the UAE and
          beyond.
        </div>
        <header className="site-header">
          <nav className="nav container" aria-label="Main navigation">
            <a className="wordmark" href="/">
              AtiqSalon AI
            </a>
            <div className="nav-links">
              <a href="/features">Product</a>
              <a href="/solutions/beauty-salons">Solutions</a>
              <a href="/ai">AI Employees</a>
              <a href="/pricing">Pricing</a>
              <a href="/resources">Resources</a>
              <a href="/book-demo">Book a Demo</a>
              <a href="http://localhost:3001/login">Sign In</a>
              <a className="button" href="http://localhost:3001/register">
                Start Free Trial
              </a>
            </div>
          </nav>
        </header>
        {children}
        <footer className="footer">
          <div className="container footer-grid">
            <div>
              <p className="wordmark">AtiqSalon AI</p>
              <p>Operating System for Beauty & Wellness Businesses</p>
            </div>
            <div>
              <b>Platform</b>
              <p>
                <a href="/features">Features</a>
              </p>
              <p>
                <a href="/pricing">Pricing</a>
              </p>
            </div>
            <div>
              <b>Company</b>
              <p>
                <a href="/contact">Contact</a>
              </p>
              <p>
                <a href="/book-demo">Book a Demo</a>
              </p>
            </div>
            <div>
              <b>Trust</b>
              <p>
                <a href="/security">Security</a>
              </p>
              <p>
                <a href="/privacy">Privacy</a>
              </p>
              <p>
                <a href="/terms">Terms</a>
              </p>
            </div>
          </div>
        </footer>
      </body>
    </html>
  );
}
