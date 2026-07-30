import { notFound } from "next/navigation";
import { Card, EmptyState, PageTitle, StatCard } from "@atiqsalon/ui";
import { PortalShell } from "@/components/portal-shell";
const allowed = [
  "dashboard",
  "settings/organization",
  "settings/branches",
  "settings/team",
  "settings/roles",
  "settings/security",
  "audit",
  "calendar",
  "appointments",
  "appointments/new",
  "services",
  "services/categories",
  "services/new",
  "staff",
  "staff/new",
  "customers",
  "customers/new",
  "resources",
  "resources/new",
];
export default async function PortalPage({
  params,
}: {
  params: Promise<{ route: string[] }>;
}) {
  const { route } = await params;
  const key = route.join("/");
  if (!allowed.includes(key)) notFound();
  const title =
    key === "dashboard"
      ? "Foundation dashboard"
      : key.split("/").at(-1)!.replace("-", " ");
  const operational = [
    "calendar",
    "appointments",
    "services",
    "staff",
    "customers",
    "resources",
  ].some((prefix) => key.startsWith(prefix));
  return (
    <PortalShell title={title}>
      <PageTitle eyebrow="Secure workspace" title={title}>
        {key === "dashboard"
          ? "Only verified foundational information is shown. No booking, sales or customer metrics are invented."
          : "This area is connected to the foundational organization and access model."}
      </PageTitle>
      {key === "dashboard" ? (
        <>
          <div className="portal-grid">
            <StatCard
              label="Organization"
              value="Not configured"
              note="Complete onboarding to establish your tenant."
            />
            <StatCard
              label="Branches"
              value="0"
              note="No branch data has been created."
            />
            <StatCard
              label="Platform status"
              value="Foundation"
              note="API connection required for live status."
            />
          </div>
          <h2>Setup progress</h2>
          <Card>
            <ol>
              <li>Verify your email</li>
              <li>Create your organization</li>
              <li>Add your first branch</li>
              <li>Invite your team</li>
            </ol>
          </Card>
        </>
      ) : operational ? (
        <section>
          <div className="actions">
            <button className="button">
              Create {title === "calendar" ? "appointment" : title}
            </button>
            <button className="button secondary">Filters</button>
          </div>
          <div
            className="operational-table"
            role="region"
            aria-label={`${title} workspace`}
          >
            <div className="table-toolbar">
              <input
                className="input"
                aria-label={`Search ${title}`}
                placeholder={`Search ${title}`}
              />
              <span className="badge">API-backed</span>
            </div>
            <EmptyState
              title={`No ${title} loaded`}
              description="Start the API and run the explicit development seed before working with real tenant-scoped records."
            />
          </div>
        </section>
      ) : (
        <EmptyState
          title={`No ${title} data yet`}
          description="Complete onboarding and connect the API to manage this area. No mock records are displayed."
        />
      )}
    </PortalShell>
  );
}
