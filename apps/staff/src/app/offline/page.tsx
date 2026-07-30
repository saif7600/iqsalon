import Link from "next/link";

export default function Offline() {
  return (
    <main className="offline">
      <p>OFFLINE / SAFE MODE</p>
      <h1>Connection required.</h1>
      <span>
        No operational action has been recorded. Restore connectivity to
        continue.
      </span>
      <Link href="/">Retry connection</Link>
    </main>
  );
}
