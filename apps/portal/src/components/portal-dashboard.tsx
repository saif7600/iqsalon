"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { apiRequest } from "@atiqsalon/sdk";
import { ErrorState, LoadingState } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Organization = {
  id: string;
  tradingName: string;
  countryCode: string;
  defaultCurrency: string;
};
type Branch = { id: string; name: string; city?: string };
type Appointment = {
  id: string;
  customerDisplayName: string;
  appointmentNumber: string;
  status: string;
  startAtUtc: string;
  endAtUtc: string;
};
type Sale = {
  id: string;
  saleNumber: string;
  status: string;
  currencyCode: string;
  businessDate: string;
  grandTotal: number;
  balanceDue: number;
};
type NamedRecord = { id: string; displayName?: string; name?: string };
type DashboardData = {
  organizations: Organization[];
  branches: Branch[];
  appointments: Appointment[];
  sales: Sale[];
  customers: NamedRecord[];
  staff: NamedRecord[];
  services: NamedRecord[];
};

const money = (value: number, currency = "AED") =>
  new Intl.NumberFormat("en-AE", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(value);

const time = (value: string) =>
  new Intl.DateTimeFormat("en-AE", {
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));

export function PortalDashboard({
  view = "dashboard",
}: {
  view?: "dashboard" | "organization" | "branches";
}) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const from = new Date();
    from.setHours(0, 0, 0, 0);
    const to = new Date(from);
    to.setDate(to.getDate() + 1);
    const query = new URLSearchParams({
      from: from.toISOString(),
      to: to.toISOString(),
    });
    Promise.all([
      apiRequest<Organization[]>("/organizations"),
      apiRequest<Branch[]>("/branches"),
      apiRequest<Appointment[]>(`/appointments?${query}`),
      apiRequest<Sale[]>("/sales"),
      apiRequest<NamedRecord[]>("/customers"),
      apiRequest<NamedRecord[]>("/staff"),
      apiRequest<NamedRecord[]>("/services"),
    ])
      .then(
        ([
          organizations,
          branches,
          appointments,
          sales,
          customers,
          staff,
          services,
        ]) =>
          setData({
            organizations,
            branches,
            appointments,
            sales,
            customers,
            staff,
            services,
          }),
      )
      .catch(() => setError("The live operating summary could not be loaded."))
      .finally(() => setLoading(false));
  }, []);

  const summary = useMemo(() => {
    const sales = data?.sales ?? [];
    const posted = sales.filter((sale) => sale.status === "Posted");
    const revenue = posted.reduce((total, sale) => total + sale.grandTotal, 0);
    const outstanding = sales.reduce(
      (total, sale) => total + sale.balanceDue,
      0,
    );
    const confirmed = (data?.appointments ?? []).filter(
      (appointment) => appointment.status === "Confirmed",
    ).length;
    return { revenue, outstanding, confirmed, posted: posted.length };
  }, [data]);

  if (view !== "dashboard") {
    const title = view === "organization" ? "Organization" : "Branches";
    return (
      <PortalShell title={title}>
        <section className="workspace-title">
          <div>
            <small>Settings</small>
            <h1>{title}</h1>
          </div>
        </section>
        {error && <ErrorState message={error} />}
        {loading ? (
          <LoadingState />
        ) : (
          <div className="settings-records">
            {view === "organization"
              ? data?.organizations.map((organization) => (
                  <article key={organization.id}>
                    <span>Organization</span>
                    <h2>{organization.tradingName}</h2>
                    <p>
                      {organization.countryCode} ·{" "}
                      {organization.defaultCurrency}
                    </p>
                  </article>
                ))
              : data?.branches.map((branch) => (
                  <article key={branch.id}>
                    <span>Branch</span>
                    <h2>{branch.name}</h2>
                    <p>{branch.city || "City not specified"}</p>
                  </article>
                ))}
          </div>
        )}
      </PortalShell>
    );
  }

  return (
    <PortalShell title="Overview">
      <section className="owner-dashboard">
        <header className="owner-welcome">
          <div>
            <small>Business overview</small>
            <h1>
              {data?.organizations[0]?.tradingName || "Your operating day"}
            </h1>
            <p>
              Live appointments, customers, sales, team and service activity.
            </p>
          </div>
          <div className="dashboard-actions">
            <span className="live-indicator">Live data</span>
            <Link className="button secondary" href="/reports/commercial">
              View reports
            </Link>
            <Link className="button" href="/appointments/new">
              New appointment
            </Link>
          </div>
        </header>

        {error && <ErrorState message={error} />}
        {loading ? (
          <LoadingState />
        ) : data ? (
          <>
            <section className="metric-strip" aria-label="Business metrics">
              <article className="metric-tile metric-violet">
                <div>
                  <span>Posted revenue</span>
                  <b>{summary.posted} sales</b>
                </div>
                <strong>
                  {money(
                    summary.revenue,
                    data.organizations[0]?.defaultCurrency,
                  )}
                </strong>
              </article>
              <article className="metric-tile metric-blue">
                <div>
                  <span>Appointments today</span>
                  <b>{summary.confirmed} confirmed</b>
                </div>
                <strong>{data.appointments.length}</strong>
              </article>
              <article className="metric-tile metric-teal">
                <div>
                  <span>Customers</span>
                  <b>Tenant records</b>
                </div>
                <strong>{data.customers.length}</strong>
              </article>
              <article className="metric-tile metric-amber">
                <div>
                  <span>Outstanding</span>
                  <b>Across open sales</b>
                </div>
                <strong>
                  {money(
                    summary.outstanding,
                    data.organizations[0]?.defaultCurrency,
                  )}
                </strong>
              </article>
              <article className="metric-tile metric-rose">
                <div>
                  <span>Active catalogue</span>
                  <b>{data.staff.length} staff records</b>
                </div>
                <strong>{data.services.length}</strong>
              </article>
            </section>

            <div className="dashboard-columns">
              <section className="dashboard-panel schedule-panel">
                <div className="panel-heading">
                  <div>
                    <small>Today</small>
                    <h2>Schedule</h2>
                  </div>
                  <Link href="/calendar">Open calendar</Link>
                </div>
                {data.appointments.length ? (
                  <div className="schedule-list">
                    {data.appointments
                      .slice()
                      .sort(
                        (left, right) =>
                          new Date(left.startAtUtc).getTime() -
                          new Date(right.startAtUtc).getTime(),
                      )
                      .slice(0, 8)
                      .map((appointment) => (
                        <Link
                          className="schedule-row"
                          href={`/appointments/${appointment.id}`}
                          key={appointment.id}
                        >
                          <time>{time(appointment.startAtUtc)}</time>
                          <span>
                            <strong>{appointment.customerDisplayName}</strong>
                            <small>{appointment.appointmentNumber}</small>
                          </span>
                          <b data-status={appointment.status}>
                            {appointment.status}
                          </b>
                        </Link>
                      ))}
                  </div>
                ) : (
                  <div className="operational-empty">
                    <span>Schedule clear</span>
                    <h3>No appointments today</h3>
                    <p>
                      New bookings will appear here immediately after
                      confirmation.
                    </p>
                    <Link href="/appointments/new">Create appointment</Link>
                  </div>
                )}
              </section>

              <aside className="dashboard-panel attention-panel">
                <div className="panel-heading">
                  <div>
                    <small>Operating attention</small>
                    <h2>Review now</h2>
                  </div>
                  <Link href="/iqai">Ask IQAI</Link>
                </div>
                <div className="attention-list">
                  {!data.branches.length && (
                    <Link href="/settings/branches">
                      <i className="attention-warning">!</i>
                      <span>
                        <strong>No permitted branches</strong>
                        <small>
                          Add or assign a branch to unlock branch operations.
                        </small>
                      </span>
                    </Link>
                  )}
                  {summary.outstanding > 0 && (
                    <Link href="/pos/sales">
                      <i className="attention-money">AED</i>
                      <span>
                        <strong>
                          {money(
                            summary.outstanding,
                            data.organizations[0]?.defaultCurrency,
                          )}{" "}
                          outstanding
                        </strong>
                        <small>Review open sale balances.</small>
                      </span>
                    </Link>
                  )}
                  {!data.appointments.length && (
                    <Link href="/calendar">
                      <i className="attention-calendar">0</i>
                      <span>
                        <strong>No appointments scheduled today</strong>
                        <small>Review availability or incoming bookings.</small>
                      </span>
                    </Link>
                  )}
                  <Link href="/inventory">
                    <i className="attention-stock">S</i>
                    <span>
                      <strong>Inventory controls</strong>
                      <small>
                        Review balances and reorder status by branch.
                      </small>
                    </span>
                  </Link>
                </div>
              </aside>
            </div>

            <div className="dashboard-lower">
              <section className="dashboard-panel activity-panel">
                <div className="panel-heading">
                  <div>
                    <small>Commercial activity</small>
                    <h2>Recent sales</h2>
                  </div>
                  <Link href="/pos/sales">View all</Link>
                </div>
                {data.sales.length ? (
                  data.sales.slice(0, 6).map((sale) => (
                    <Link
                      className="activity-row"
                      href={`/pos/sales/${sale.id}`}
                      key={sale.id}
                    >
                      <span>
                        <strong>{sale.saleNumber}</strong>
                        <small>
                          {sale.businessDate} · {sale.status}
                        </small>
                      </span>
                      <b>{money(sale.grandTotal, sale.currencyCode)}</b>
                    </Link>
                  ))
                ) : (
                  <p className="panel-empty">
                    No sale activity has been posted.
                  </p>
                )}
              </section>

              <section className="dashboard-panel application-panel">
                <div className="panel-heading">
                  <div>
                    <small>Workspace</small>
                    <h2>Business applications</h2>
                  </div>
                </div>
                <div className="dashboard-apps">
                  {[
                    ["Calendar", "/calendar", "Schedule"],
                    ["Point of sale", "/pos", "Checkout"],
                    ["Customers", "/customers", "Relationships"],
                    ["Inventory", "/inventory", "Stock"],
                    ["Workforce", "/workforce", "Team"],
                    ["IQAI", "/iqai", "Copilot"],
                  ].map(([label, href, note]) => (
                    <Link href={href} key={href}>
                      <span>{label.slice(0, 1)}</span>
                      <strong>{label}</strong>
                      <small>{note}</small>
                    </Link>
                  ))}
                </div>
              </section>
            </div>
          </>
        ) : null}
      </section>
    </PortalShell>
  );
}
