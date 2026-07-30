"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import {
  Badge,
  Button,
  Card,
  ErrorState,
  LoadingState,
  PageTitle,
} from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Branch = {
  id: string;
  organizationId: string;
  name: string;
  timeZone: string;
};
type Appointment = {
  id: string;
  branchId: string;
  appointmentNumber: string;
  customerDisplayName: string;
  status: string;
  source: string;
  startAtUtc: string;
  endAtUtc: string;
};
type Service = {
  id: string;
  organizationId: string;
  name: string;
  durationMinutes: number;
  cleanupMinutes: number;
};
type Staff = { id: string; displayName: string };
type Customer = { id: string; displayName: string };
type AppointmentDetail = {
  appointment: Appointment;
  services: Array<{
    serviceId: string;
    staffMemberId: string;
    unitPrice: number;
    durationMinutes: number;
  }>;
};
type EditLine = { serviceId: string; staffMemberId: string };

const transitionActions: Record<string, Array<[string, string]>> = {
  Draft: [
    ["confirm", "Confirm"],
    ["cancel", "Cancel"],
  ],
  PendingConfirmation: [
    ["confirm", "Confirm"],
    ["cancel", "Cancel"],
  ],
  Confirmed: [
    ["check-in", "Check in"],
    ["no-show", "No-show"],
    ["cancel", "Cancel"],
  ],
  CheckedIn: [
    ["start", "Start"],
    ["cancel", "Cancel"],
  ],
  InProgress: [["complete", "Complete"]],
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-AE", {
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}

export function AppointmentWorkspace({
  mode,
  appointmentId,
}: {
  mode: "calendar" | "list" | "new" | "detail";
  appointmentId?: string;
}) {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [branchId, setBranchId] = useState("");
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [services, setServices] = useState<Service[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [detail, setDetail] = useState<AppointmentDetail | null>(null);
  const [editLines, setEditLines] = useState<EditLine[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [calendarView, setCalendarView] = useState<"day" | "week">("week");
  const [calendarDate, setCalendarDate] = useState(
    new Date().toLocaleDateString("en-CA"),
  );

  const loadAppointments = useCallback(
    async (selectedBranch?: string) => {
      const from = new Date();
      from.setDate(from.getDate() - 1);
      const to = new Date();
      to.setDate(to.getDate() + (mode === "calendar" ? 7 : 90));
      const query = new URLSearchParams({
        from: from.toISOString(),
        to: to.toISOString(),
      });
      if (selectedBranch) query.set("branchId", selectedBranch);
      setAppointments(
        await apiRequest<Appointment[]>(`/appointments?${query}`),
      );
    },
    [mode],
  );

  useEffect(() => {
    async function load() {
      try {
        if (mode === "detail" && appointmentId) {
          const [appointmentDetail, availableServices, availableStaff] =
            await Promise.all([
              apiRequest<AppointmentDetail>(`/appointments/${appointmentId}`),
              apiRequest<Service[]>("/services"),
              apiRequest<Staff[]>("/staff"),
            ]);
          setDetail(appointmentDetail);
          setServices(availableServices);
          setStaff(availableStaff);
          setEditLines(
            appointmentDetail.services.map((line) => ({
              serviceId: line.serviceId,
              staffMemberId: line.staffMemberId,
            })),
          );
        } else {
          const availableBranches = await apiRequest<Branch[]>("/branches");
          setBranches(availableBranches);
          const selectedBranch = availableBranches[0]?.id ?? "";
          setBranchId(selectedBranch);
          if (mode === "new") {
            const [availableServices, availableStaff, availableCustomers] =
              await Promise.all([
                apiRequest<Service[]>("/services"),
                apiRequest<Staff[]>("/staff"),
                apiRequest<Customer[]>("/customers"),
              ]);
            setServices(availableServices);
            setStaff(availableStaff);
            setCustomers(availableCustomers);
          } else {
            await loadAppointments(selectedBranch);
          }
        }
      } catch {
        setError(
          "The operational data could not be loaded. Sign in again or check the API.",
        );
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, [appointmentId, loadAppointments, mode]);

  async function changeBranch(value: string) {
    setBranchId(value);
    setLoading(true);
    try {
      await loadAppointments(value);
    } catch {
      setError("Appointments could not be refreshed.");
    } finally {
      setLoading(false);
    }
  }

  async function createAppointment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    const data = new FormData(event.currentTarget);
    const service = services.find((item) => item.id === data.get("serviceId"));
    const branch = branches.find((item) => item.id === data.get("branchId"));
    if (!service || !branch) return;
    const start = new Date(String(data.get("startAt")));
    const end = new Date(
      start.getTime() +
        (service.durationMinutes + service.cleanupMinutes) * 60_000,
    );
    try {
      const result = await apiRequest<{ id: string }>("/appointments", {
        method: "POST",
        body: JSON.stringify({
          organizationId: branch.organizationId,
          branchId: branch.id,
          customerId: data.get("customerId"),
          startAtUtc: start.toISOString(),
          endAtUtc: end.toISOString(),
          services: [
            { serviceId: service.id, staffMemberId: data.get("staffMemberId") },
          ],
          source: "Reception",
          status: "Confirmed",
          customerNotes: data.get("customerNotes"),
        }),
      });
      window.location.assign(`/appointments/${result.id}`);
    } catch {
      setError(
        "The appointment could not be created. The slot may conflict or fall outside working hours.",
      );
    }
  }

  async function transition(action: string) {
    if (!detail) return;
    try {
      await apiRequest<void>(
        `/appointments/${detail.appointment.id}/${action}`,
        {
          method: "POST",
          body: JSON.stringify({ reason: null }),
        },
      );
      setDetail(
        await apiRequest<AppointmentDetail>(
          `/appointments/${detail.appointment.id}`,
        ),
      );
    } catch {
      setError("The status change was rejected.");
    }
  }

  async function reschedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail) return;
    const data = new FormData(event.currentTarget);
    const start = new Date(String(data.get("startAt")));
    const duration =
      new Date(detail.appointment.endAtUtc).getTime() -
      new Date(detail.appointment.startAtUtc).getTime();
    try {
      await apiRequest(`/appointments/${detail.appointment.id}/reschedule`, {
        method: "POST",
        body: JSON.stringify({
          startAtUtc: start.toISOString(),
          endAtUtc: new Date(start.getTime() + duration).toISOString(),
        }),
      });
      setDetail(
        await apiRequest<AppointmentDetail>(
          `/appointments/${detail.appointment.id}`,
        ),
      );
    } catch {
      setError(
        "The reschedule was rejected because the new interval is unavailable.",
      );
    }
  }

  async function editAppointment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (
      !detail ||
      editLines.some((line) => !line.serviceId || !line.staffMemberId)
    )
      return;
    const data = new FormData(event.currentTarget);
    const start = new Date(String(data.get("startAt")));
    const durationMinutes = editLines.reduce((total, line) => {
      const service = services.find((item) => item.id === line.serviceId);
      return (
        total + (service ? service.durationMinutes + service.cleanupMinutes : 0)
      );
    }, 0);
    try {
      await apiRequest(`/appointments/${detail.appointment.id}`, {
        method: "PUT",
        body: JSON.stringify({
          startAtUtc: start.toISOString(),
          endAtUtc: new Date(
            start.getTime() + durationMinutes * 60_000,
          ).toISOString(),
          services: editLines,
          customerNotes: data.get("customerNotes"),
          internalNotes: data.get("internalNotes"),
        }),
      });
      const refreshed = await apiRequest<AppointmentDetail>(
        `/appointments/${detail.appointment.id}`,
      );
      setDetail(refreshed);
      setEditLines(
        refreshed.services.map((line) => ({
          serviceId: line.serviceId,
          staffMemberId: line.staffMemberId,
        })),
      );
    } catch {
      setError(
        "The appointment edit was rejected because staff, resources, or the interval are unavailable.",
      );
    }
  }

  const grouped = useMemo(() => {
    const result = new Map<string, Appointment[]>();
    for (const appointment of appointments) {
      const key = new Date(appointment.startAtUtc).toLocaleDateString("en-CA");
      result.set(key, [...(result.get(key) ?? []), appointment]);
    }
    return [...result.entries()];
  }, [appointments]);
  const visibleGroups = useMemo(() => {
    if (calendarView === "week") return grouped;
    return grouped.filter(([date]) => date === calendarDate);
  }, [calendarDate, calendarView, grouped]);
  const setupMissing =
    mode === "new" &&
    (!branches.length || !customers.length || !services.length || !staff.length);

  const title =
    mode === "calendar"
      ? "Reception calendar"
      : mode === "new"
        ? "New appointment"
        : mode === "detail"
          ? "Appointment detail"
          : "Appointments";
  return (
    <PortalShell title={title}>
      <PageTitle eyebrow="Bookings" title={title}>
        Manage the schedule, staff assignments, and customer appointments.
      </PageTitle>
      {error && <ErrorState message={error} />}
      {loading ? (
        <LoadingState />
      ) : mode === "new" ? (
        <div className="booking-workspace">
          <div className="booking-steps" aria-label="Booking steps">
            <strong>1. Customer</strong>
            <span>2. Service & staff</span>
            <span>3. Date & time</span>
            <span>4. Confirm</span>
          </div>
          {setupMissing && (
            <Card>
              <div className="booking-prerequisites">
                <div>
                  <small>SETUP REQUIRED</small>
                  <h2>Complete the booking catalogue</h2>
                  <p>
                    A booking needs at least one branch, customer, active
                    service, and staff member. Empty selectors are disabled
                    until these records exist.
                  </p>
                </div>
                <div className="booking-setup-links">
                  {!branches.length && <a href="/settings/branches">Add branch</a>}
                  {!customers.length && <a href="/customers/new">Add customer</a>}
                  {!services.length && <a href="/services/new">Add service</a>}
                  {!staff.length && <a href="/staff/new">Add staff member</a>}
                </div>
              </div>
            </Card>
          )}
          <Card>
            <form className="appointment-form" onSubmit={createAppointment}>
            <label>
              Branch
              <select name="branchId" required>
                {branches.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Customer
              <select name="customerId" required>
                <option value="">Select customer</option>
                {customers.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.displayName}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Service
              <select name="serviceId" required>
                <option value="">Select service</option>
                {services.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Staff member
              <select name="staffMemberId" required>
                <option value="">Select staff</option>
                {staff.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.displayName}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Start time
              <input name="startAt" type="datetime-local" required />
            </label>
            <label className="full">
              Customer notes
              <textarea name="customerNotes" rows={3} />
            </label>
              <Button type="submit" disabled={setupMissing}>
                Review appointment
              </Button>
            </form>
          </Card>
        </div>
      ) : mode === "detail" && detail ? (
        <div className="appointment-detail-grid">
          <Card>
            <div className="record-heading">
              <div>
                <small>{detail.appointment.appointmentNumber}</small>
                <h2>{detail.appointment.customerDisplayName}</h2>
              </div>
              <Badge>{detail.appointment.status}</Badge>
            </div>
            <dl className="record-facts">
              <div>
                <dt>Starts</dt>
                <dd>{formatDate(detail.appointment.startAtUtc)}</dd>
              </div>
              <div>
                <dt>Source</dt>
                <dd>{detail.appointment.source}</dd>
              </div>
              <div>
                <dt>Services</dt>
                <dd>{detail.services.length}</dd>
              </div>
            </dl>
            <div className="actions">
              <a className="button" href={`/pos?appointmentId=${detail.appointment.id}`}>
                Checkout
              </a>
              {(transitionActions[detail.appointment.status] ?? []).map(
                ([action, label]) => (
                  <Button
                    key={action}
                    className="secondary"
                    onClick={() => transition(action)}
                  >
                    {label}
                  </Button>
                ),
              )}
            </div>
          </Card>
          <Card>
            <h2>Reschedule</h2>
            <form className="reschedule-form" onSubmit={reschedule}>
              <input name="startAt" type="datetime-local" required />
              <Button type="submit">Check and move</Button>
            </form>
          </Card>
          <Card>
            <h2>Edit services and staff</h2>
            <form className="appointment-form" onSubmit={editAppointment}>
              <label>
                Start time
                <input name="startAt" type="datetime-local" required />
              </label>
              {editLines.map((line, index) => (
                <div
                  className="full record-row"
                  key={`${index}-${line.serviceId}`}
                >
                  <select
                    aria-label={`Service ${index + 1}`}
                    value={line.serviceId}
                    onChange={(event) =>
                      setEditLines((current) =>
                        current.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, serviceId: event.target.value }
                            : item,
                        ),
                      )
                    }
                  >
                    <option value="">Select service</option>
                    {services.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.name}
                      </option>
                    ))}
                  </select>
                  <select
                    aria-label={`Staff ${index + 1}`}
                    value={line.staffMemberId}
                    onChange={(event) =>
                      setEditLines((current) =>
                        current.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, staffMemberId: event.target.value }
                            : item,
                        ),
                      )
                    }
                  >
                    <option value="">Select staff</option>
                    {staff.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.displayName}
                      </option>
                    ))}
                  </select>
                  <Button
                    type="button"
                    className="secondary"
                    onClick={() =>
                      setEditLines((current) =>
                        current.filter((_, itemIndex) => itemIndex !== index),
                      )
                    }
                  >
                    Remove
                  </Button>
                </div>
              ))}
              <Button
                type="button"
                className="secondary"
                onClick={() =>
                  setEditLines((current) => [
                    ...current,
                    { serviceId: "", staffMemberId: "" },
                  ])
                }
              >
                Add service
              </Button>
              <label className="full">
                Customer notes
                <textarea name="customerNotes" rows={2} />
              </label>
              <label className="full">
                Internal notes
                <textarea name="internalNotes" rows={2} />
              </label>
              <Button type="submit" disabled={editLines.length === 0}>
                Save appointment
              </Button>
            </form>
          </Card>
        </div>
      ) : (
        <>
          <div className="calendar-commandbar">
            <label>
              Branch
              <select
                value={branchId}
                onChange={(event) => void changeBranch(event.target.value)}
              >
                {branches.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Schedule date
              <input
                type="date"
                value={calendarDate}
                onChange={(event) => setCalendarDate(event.target.value)}
              />
            </label>
            <div className="calendar-view-switch" aria-label="Calendar view">
              <button
                type="button"
                className={calendarView === "day" ? "active" : ""}
                onClick={() => setCalendarView("day")}
              >
                Day
              </button>
              <button
                type="button"
                className={calendarView === "week" ? "active" : ""}
                onClick={() => setCalendarView("week")}
              >
                Week
              </button>
            </div>
            <a className="button" href="/appointments/new">
              + New booking
            </a>
          </div>
          <div className="calendar-summary">
            <div><strong>{appointments.length}</strong><span>Bookings</span></div>
            <div><strong>{appointments.filter((item) => item.status === "Confirmed").length}</strong><span>Confirmed</span></div>
            <div><strong>{appointments.filter((item) => item.status === "CheckedIn").length}</strong><span>Checked in</span></div>
            <div><strong>{appointments.filter((item) => item.status === "InProgress").length}</strong><span>In service</span></div>
          </div>
          <div className={`agenda calendar-agenda calendar-${calendarView}`}>
            {visibleGroups.length === 0 ? (
              <Card>
                <div className="calendar-empty">
                  <small>OPEN SCHEDULE</small>
                  <h2>No bookings for this {calendarView}</h2>
                  <p>
                    The schedule is clear. Create a booking or choose another
                    date to review availability.
                  </p>
                  <a className="button" href="/appointments/new">
                    Create booking
                  </a>
                </div>
              </Card>
            ) : (
              visibleGroups.map(([date, items]) => (
                <section className="agenda-day" key={date}>
                  <header>
                    <strong>
                      {new Intl.DateTimeFormat("en-AE", {
                        weekday: "long",
                        day: "numeric",
                        month: "long",
                      }).format(new Date(`${date}T12:00:00`))}
                    </strong>
                    <span>{items.length}</span>
                  </header>
                  {items.map((appointment) => (
                    <a
                      className="appointment-row"
                      href={`/appointments/${appointment.id}`}
                      key={appointment.id}
                    >
                      <time>
                        {new Intl.DateTimeFormat("en-AE", {
                          hour: "numeric",
                          minute: "2-digit",
                        }).format(new Date(appointment.startAtUtc))}
                      </time>
                      <div>
                        <strong>{appointment.customerDisplayName}</strong>
                        <small>
                          {appointment.appointmentNumber} · {appointment.source}
                        </small>
                      </div>
                      <Badge>{appointment.status}</Badge>
                    </a>
                  ))}
                </section>
              ))
            )}
          </div>
        </>
      )}
    </PortalShell>
  );
}
