import Link from "next/link";

export default function Offline() {
  return (
    <main className="offline-page">
      <p className="eyebrow">Connection unavailable</p>
      <h1>You are offline.</h1>
      <p>
        No customer or financial data is stored in this offline shell. Restore
        your connection to continue securely.
      </p>
      <Link className="retry" href="/">
        Try again
      </Link>
    </main>
  );
}
