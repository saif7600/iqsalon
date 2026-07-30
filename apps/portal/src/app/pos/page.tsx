import { PosWorkspace } from "@/components/pos-workspace";

export default async function PosPage({ searchParams }: { searchParams: Promise<{ appointmentId?: string }> }) {
  const { appointmentId } = await searchParams;
  return <PosWorkspace appointmentId={appointmentId} />;
}
