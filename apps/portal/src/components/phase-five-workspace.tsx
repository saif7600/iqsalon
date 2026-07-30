"use client";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import {
  Button,
  Card,
  ErrorState,
  LoadingState,
  PageTitle,
} from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";
type Branch = { id: string; organizationId: string; name: string };
type Staff = { id: string; displayName: string };
type Shift = {
  id: string;
  staffMemberId: string;
  startsAtUtc: string;
  status: string;
};
type Attendance = {
  id: string;
  businessDate: string;
  workedMinutes: number;
  status: string;
};
type Leave = { id: string; startsOn: string; endsOn: string; status: string };
type Target = {
  id: string;
  metric: string;
  targetValue: number;
  periodStart: string;
  periodEnd: string;
};
export function WorkforceWorkspace({
  mode = "workforce",
}: {
  mode?: "workforce" | "performance";
}) {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [attendance, setAttendance] = useState<Attendance[]>([]);
  const [leave, setLeave] = useState<Leave[]>([]);
  const [targets, setTargets] = useState<Target[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const load = useCallback(async () => {
    setError("");
    try {
      const [branchRows, staffRows] = await Promise.all([
        apiRequest<Branch[]>("/branches"),
        apiRequest<Staff[]>("/staff"),
      ]);
      setBranches(branchRows);
      setStaff(staffRows);
      if (mode === "workforce") {
        const rows = await Promise.all([
          apiRequest<Shift[]>("/workforce/shifts"),
          apiRequest<Attendance[]>("/workforce/attendance"),
          apiRequest<Leave[]>("/workforce/leave-requests"),
        ]);
        setShifts(rows[0]);
        setAttendance(rows[1]);
        setLeave(rows[2]);
      } else setTargets(await apiRequest<Target[]>("/performance/targets"));
    } catch {
      setError(
        `${mode === "workforce" ? "Workforce" : "Performance"} data could not be loaded.`,
      );
    } finally {
      setLoading(false);
    }
  }, [mode]);
  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const branch = branches.find((x) => x.id === data.get("branchId"));
    if (!branch) return;
    try {
      if (mode === "workforce")
        await apiRequest("/workforce/shifts", {
          method: "POST",
          body: JSON.stringify({
            organizationId: branch.organizationId,
            branchId: branch.id,
            staffMemberId: data.get("staffMemberId"),
            shiftTemplateId: null,
            startsAtUtc: data.get("startsAtUtc"),
            endsAtUtc: data.get("endsAtUtc"),
            unpaidBreakMinutes: Number(data.get("breakMinutes")),
            notes: data.get("notes"),
          }),
        });
      else
        await apiRequest("/performance/targets", {
          method: "POST",
          body: JSON.stringify({
            organizationId: branch.organizationId,
            branchId: branch.id,
            staffMemberId: data.get("staffMemberId") || null,
            metric: data.get("metric"),
            targetValue: Number(data.get("targetValue")),
            periodStart: data.get("periodStart"),
            periodEnd: data.get("periodEnd"),
          }),
        });
      form.reset();
      await load();
    } catch {
      setError(
        `${mode === "workforce" ? "Shift" : "Target"} could not be created. Check permissions and required values.`,
      );
    }
  }
  const title =
    mode === "workforce" ? "Roster & attendance" : "Targets & performance";
  return (
    <PortalShell title={title}>
      <PageTitle eyebrow="Phase 5 operating core" title={title}>
        Live tenant-scoped records with API-enforced permissions.
      </PageTitle>
      {error && <ErrorState message={error} />}
      {loading ? (
        <LoadingState />
      ) : (
        <>
          <Card>
            <h2>
              {mode === "workforce" ? "Publish a shift" : "Create a target"}
            </h2>
            <form className="admin-form" onSubmit={submit}>
              <select name="branchId" required>
                <option value="">Branch</option>
                {branches.map((x) => (
                  <option key={x.id} value={x.id}>
                    {x.name}
                  </option>
                ))}
              </select>
              <select name="staffMemberId" required={mode === "workforce"}>
                <option value="">
                  {mode === "workforce" ? "Staff member" : "Whole branch"}
                </option>
                {staff.map((x) => (
                  <option key={x.id} value={x.id}>
                    {x.displayName}
                  </option>
                ))}
              </select>
              {mode === "workforce" ? (
                <>
                  <input name="startsAtUtc" type="datetime-local" required />
                  <input name="endsAtUtc" type="datetime-local" required />
                  <input
                    name="breakMinutes"
                    aria-label="Unpaid break minutes"
                    type="number"
                    min="0"
                    defaultValue="0"
                    required
                  />
                  <input name="notes" placeholder="Shift note" />
                </>
              ) : (
                <>
                  <select name="metric">
                    <option>ServiceRevenue</option>
                    <option>ProductRevenue</option>
                    <option>BookingsCompleted</option>
                    <option>RebookingRate</option>
                    <option>RetailAttachRate</option>
                  </select>
                  <input
                    name="targetValue"
                    aria-label="Target value"
                    type="number"
                    min="0"
                    step="0.01"
                    required
                  />
                  <input
                    name="periodStart"
                    aria-label="Period start"
                    type="date"
                    required
                  />
                  <input
                    name="periodEnd"
                    aria-label="Period end"
                    type="date"
                    required
                  />
                </>
              )}
              <Button type="submit">
                {mode === "workforce" ? "Publish shift" : "Create target"}
              </Button>
            </form>
          </Card>
          {mode === "workforce" ? (
            <div className="phase-grid">
              <Card>
                <h2>Published shifts</h2>
                {shifts.length ? (
                  shifts.map((x) => (
                    <p key={x.id}>
                      <strong>
                        {
                          staff.find((s) => s.id === x.staffMemberId)
                            ?.displayName
                        }
                      </strong>
                      <br />
                      {new Date(x.startsAtUtc).toLocaleString()} · {x.status}
                    </p>
                  ))
                ) : (
                  <p>No shifts published.</p>
                )}
              </Card>
              <Card>
                <h2>Attendance</h2>
                {attendance.length ? (
                  attendance.map((x) => (
                    <p key={x.id}>
                      {x.businessDate} · {(x.workedMinutes / 60).toFixed(1)}{" "}
                      hours · {x.status}
                    </p>
                  ))
                ) : (
                  <p>No attendance records.</p>
                )}
              </Card>
              <Card>
                <h2>Leave requests</h2>
                {leave.length ? (
                  leave.map((x) => (
                    <p key={x.id}>
                      {x.startsOn} to {x.endsOn} · {x.status}
                    </p>
                  ))
                ) : (
                  <p>No leave requests.</p>
                )}
              </Card>
            </div>
          ) : (
            <Card>
              <h2>Active targets</h2>
              {targets.length ? (
                targets.map((x) => (
                  <p key={x.id}>
                    <strong>{x.metric}</strong> · {x.targetValue} ·{" "}
                    {x.periodStart} to {x.periodEnd}
                  </p>
                ))
              ) : (
                <p>No targets configured.</p>
              )}
            </Card>
          )}
        </>
      )}
    </PortalShell>
  );
}
