"use client";
import { ErrorState } from "@atiqsalon/ui";
export default function ErrorPage({ reset }: { reset: () => void }) {
  return (
    <main className="portal-content">
      <ErrorState message="The workspace could not be loaded." />
      <button className="button" onClick={reset}>
        Try again
      </button>
    </main>
  );
}
