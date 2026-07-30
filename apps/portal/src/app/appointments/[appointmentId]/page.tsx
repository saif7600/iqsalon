import { AppointmentWorkspace } from "@/components/appointment-workspace";
export default async function AppointmentDetailPage({
  params,
}: {
  params: Promise<{ appointmentId: string }>;
}) {
  const { appointmentId } = await params;
  return <AppointmentWorkspace mode="detail" appointmentId={appointmentId} />;
}
