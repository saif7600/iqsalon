import { BookingClient } from "./booking-client";

export default async function PublicBookingPage({
  params,
}: {
  params: Promise<{ organizationSlug: string; branchCode: string }>;
}) {
  const { organizationSlug, branchCode } = await params;
  return (
    <BookingClient
      organizationSlug={organizationSlug}
      branchCode={branchCode}
    />
  );
}
