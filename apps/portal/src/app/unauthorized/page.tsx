import { ErrorState } from "@atiqsalon/ui";
export default function Unauthorized() {
  return (
    <main className="portal-content">
      <ErrorState message="You do not have permission to access this area." />
    </main>
  );
}
