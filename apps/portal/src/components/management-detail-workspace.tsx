"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";

type JsonRecord = Record<string, unknown>;
type ScheduleRow = {
  branchId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const apiBaseUrl =
    process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";
  const response = await fetch(`${apiBaseUrl}/api/v1${path}`, {
    credentials: "include",
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(body || `Request failed with status ${response.status}.`);
  }

  return (await response.json()) as T;
}

function value(record: JsonRecord, key: string): string {
  return String(record[key] ?? "");
}

export function StaffOperatingProfile({ recordId }: { recordId: string }) {
  const [profile, setProfile] = useState<JsonRecord>();
  const [branches, setBranches] = useState<JsonRecord[]>([]);
  const [services, setServices] = useState<JsonRecord[]>([]);
  const [branchIds, setBranchIds] = useState<string[]>([]);
  const [serviceIds, setServiceIds] = useState<string[]>([]);
  const [workingHours, setWorkingHours] = useState<ScheduleRow[]>([]);
  const [breaks, setBreaks] = useState<ScheduleRow[]>([]);
  const [message, setMessage] = useState("");

  const load = useCallback(async () => {
    const [nextProfile, nextBranches, nextServices] = await Promise.all([
      request<JsonRecord>(`/staff/${recordId}/operating-profile`),
      request<JsonRecord[]>("/branches"),
      request<JsonRecord[]>("/services"),
    ]);
    setProfile(nextProfile);
    setBranches(nextBranches);
    setServices(nextServices);
    setBranchIds(
      ((nextProfile.assignments as JsonRecord[]) ?? []).map((item) =>
        value(item, "branchId"),
      ),
    );
    setServiceIds(
      ((nextProfile.capabilities as JsonRecord[]) ?? []).map((item) =>
        value(item, "serviceId"),
      ),
    );
    setWorkingHours(
      ((nextProfile.workingHours as JsonRecord[]) ?? []).map((item) => ({
        branchId: value(item, "branchId"),
        dayOfWeek: Number(item.dayOfWeek),
        startTime: value(item, "startTime").slice(0, 5),
        endTime: value(item, "endTime").slice(0, 5),
      })),
    );
    setBreaks(
      ((nextProfile.breaks as JsonRecord[]) ?? []).map((item) => ({
        branchId: value(item, "branchId"),
        dayOfWeek: Number(item.dayOfWeek),
        startTime: value(item, "startTime").slice(0, 5),
        endTime: value(item, "endTime").slice(0, 5),
      })),
    );
  }, [recordId]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      void load().catch((error: Error) => setMessage(error.message));
    }, 0);
    return () => window.clearTimeout(timeout);
  }, [load]);

  async function save(event: FormEvent) {
    event.preventDefault();
    setMessage("Saving...");
    try {
      const effectiveFrom = new Date().toISOString().slice(0, 10);
      await request(`/staff/${recordId}/configuration`, {
        method: "PUT",
        body: JSON.stringify({
          assignments: branchIds.map((branchId, index) => ({
            branchId,
            startDate: effectiveFrom,
            endDate: null,
            isPrimary: index === 0,
          })),
          capabilities: serviceIds.map((serviceId) => ({
            serviceId,
            branchId: null,
            onlineBookingEnabled: true,
            skillLevel: "Standard",
          })),
          workingHours: workingHours.map((row) => ({
            ...row,
            startTime: `${row.startTime}:00`,
            endTime: `${row.endTime}:00`,
            effectiveFrom,
            effectiveTo: null,
          })),
          breaks: breaks.map((row) => ({
            ...row,
            startTime: `${row.startTime}:00`,
            endTime: `${row.endTime}:00`,
            effectiveFrom,
            effectiveTo: null,
          })),
        }),
      });
      setMessage("Operating profile saved.");
      await load();
    } catch (error) {
      setMessage((error as Error).message);
    }
  }

  async function addOverride(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setMessage("Saving override...");
    try {
      await request(`/staff/${recordId}/availability-overrides`, {
        method: "POST",
        body: JSON.stringify({
          branchId: data.get("branchId"),
          startsAtUtc: data.get("startsAt"),
          endsAtUtc: data.get("endsAt"),
          overrideType:
            data.get("isAvailable") === "true" ? "Available" : "Unavailable",
          reason: data.get("reason"),
        }),
      });
      event.currentTarget.reset();
      setMessage("Availability override added.");
      await load();
    } catch (error) {
      setMessage((error as Error).message);
    }
  }

  if (!profile) return <p>{message || "Loading staff operating profile..."}</p>;
  const staff = (profile.staff as JsonRecord) ?? profile;

  return (
    <section className="workspace-stack">
      <header className="page-heading">
        <div>
          <p className="eyebrow">Staff operations</p>
          <h1>{value(staff, "displayName") || "Staff profile"}</h1>
        </div>
        <p>{message}</p>
      </header>
      <form className="form-card workspace-stack" onSubmit={save}>
        <h2>Branch access and service capability</h2>
        <fieldset>
          <legend>Assigned branches</legend>
          {branches.map((branch) => (
            <label key={value(branch, "id")}>
              <input
                type="checkbox"
                checked={branchIds.includes(value(branch, "id"))}
                onChange={(event) =>
                  setBranchIds((current) =>
                    event.target.checked
                      ? [...current, value(branch, "id")]
                      : current.filter((id) => id !== value(branch, "id")),
                  )
                }
              />{" "}
              {value(branch, "name")}
            </label>
          ))}
        </fieldset>
        <fieldset>
          <legend>Services this staff member can perform</legend>
          {services.map((service) => (
            <label key={value(service, "id")}>
              <input
                type="checkbox"
                checked={serviceIds.includes(value(service, "id"))}
                onChange={(event) =>
                  setServiceIds((current) =>
                    event.target.checked
                      ? [...current, value(service, "id")]
                      : current.filter((id) => id !== value(service, "id")),
                  )
                }
              />{" "}
              {value(service, "name")}
            </label>
          ))}
        </fieldset>
        <ScheduleEditor
          title="Working hours"
          rows={workingHours}
          branches={branches}
          assignedBranchIds={branchIds}
          onChange={setWorkingHours}
        />
        <ScheduleEditor
          title="Breaks"
          rows={breaks}
          branches={branches}
          assignedBranchIds={branchIds}
          onChange={setBreaks}
        />
        <button type="submit">Save operating profile</button>
      </form>
      <form className="form-card workspace-stack" onSubmit={addOverride}>
        <h2>Add availability override</h2>
        <select name="branchId" required>
          {branches
            .filter((branch) => branchIds.includes(value(branch, "id")))
            .map((branch) => (
              <option key={value(branch, "id")} value={value(branch, "id")}>
                {value(branch, "name")}
              </option>
            ))}
        </select>
        <label>
          Starts <input name="startsAt" type="datetime-local" required />
        </label>
        <label>
          Ends <input name="endsAt" type="datetime-local" required />
        </label>
        <select name="isAvailable">
          <option value="false">Unavailable</option>
          <option value="true">Available</option>
        </select>
        <input name="reason" placeholder="Reason" />
        <button type="submit">Add override</button>
      </form>
    </section>
  );
}

function ScheduleEditor({
  title,
  rows,
  branches,
  assignedBranchIds,
  onChange,
}: {
  title: string;
  rows: ScheduleRow[];
  branches: JsonRecord[];
  assignedBranchIds: string[];
  onChange: (rows: ScheduleRow[]) => void;
}) {
  const days = [
    "Sunday",
    "Monday",
    "Tuesday",
    "Wednesday",
    "Thursday",
    "Friday",
    "Saturday",
  ];
  function update(index: number, patch: Partial<ScheduleRow>) {
    onChange(
      rows.map((row, rowIndex) =>
        rowIndex === index ? { ...row, ...patch } : row,
      ),
    );
  }
  return (
    <fieldset>
      <legend>{title}</legend>
      {rows.map((row, index) => (
        <div className="record-row" key={`${title}-${index}`}>
          <select
            aria-label={`${title} branch ${index + 1}`}
            value={row.branchId}
            onChange={(event) =>
              update(index, { branchId: event.target.value })
            }
          >
            {branches
              .filter((branch) =>
                assignedBranchIds.includes(value(branch, "id")),
              )
              .map((branch) => (
                <option key={value(branch, "id")} value={value(branch, "id")}>
                  {value(branch, "name")}
                </option>
              ))}
          </select>
          <select
            aria-label={`${title} day ${index + 1}`}
            value={row.dayOfWeek}
            onChange={(event) =>
              update(index, { dayOfWeek: Number(event.target.value) })
            }
          >
            {days.map((day, dayIndex) => (
              <option key={day} value={dayIndex}>
                {day}
              </option>
            ))}
          </select>
          <input
            aria-label={`${title} start ${index + 1}`}
            type="time"
            value={row.startTime}
            onChange={(event) =>
              update(index, { startTime: event.target.value })
            }
          />
          <input
            aria-label={`${title} end ${index + 1}`}
            type="time"
            value={row.endTime}
            onChange={(event) => update(index, { endTime: event.target.value })}
          />
          <button
            type="button"
            onClick={() =>
              onChange(rows.filter((_, rowIndex) => rowIndex !== index))
            }
          >
            Remove
          </button>
        </div>
      ))}
      <button
        type="button"
        disabled={assignedBranchIds.length === 0}
        onClick={() =>
          onChange([
            ...rows,
            {
              branchId: assignedBranchIds[0] ?? "",
              dayOfWeek: 1,
              startTime: title === "Breaks" ? "13:00" : "09:00",
              endTime: title === "Breaks" ? "14:00" : "20:00",
            },
          ])
        }
      >
        Add {title.toLowerCase()} row
      </button>
    </fieldset>
  );
}

export function CustomerCrmProfile({ recordId }: { recordId: string }) {
  const [profile, setProfile] = useState<JsonRecord>();
  const [message, setMessage] = useState("");
  const load = useCallback(
    () =>
      request<JsonRecord>(`/customers/${recordId}/crm-profile`).then(
        setProfile,
      ),
    [recordId],
  );

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      void load().catch((error: Error) => setMessage(error.message));
    }, 0);
    return () => window.clearTimeout(timeout);
  }, [load]);

  async function addNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    try {
      await request(`/customers/${recordId}/notes`, {
        method: "POST",
        body: JSON.stringify({
          noteType: "Operational",
          content: data.get("text"),
          isSensitive: data.get("isSensitive") === "on",
        }),
      });
      event.currentTarget.reset();
      setMessage("Customer note added.");
      await load();
    } catch (error) {
      setMessage((error as Error).message);
    }
  }

  async function saveConsent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    try {
      await request(`/customers/${recordId}/consent`, {
        method: "PUT",
        body: JSON.stringify({
          email: data.get("marketingConsent") === "on",
          sms: data.get("smsConsent") === "on",
          whatsApp: data.get("whatsAppConsent") === "on",
        }),
      });
      setMessage("Consent record updated.");
      await load();
    } catch (error) {
      setMessage((error as Error).message);
    }
  }

  if (!profile) return <p>{message || "Loading customer profile..."}</p>;
  const customer = (profile.customer as JsonRecord) ?? profile;

  return (
    <section className="workspace-stack">
      <header className="page-heading">
        <div>
          <p className="eyebrow">Customer CRM</p>
          <h1>{value(customer, "displayName") || value(customer, "name")}</h1>
        </div>
        <p>{message}</p>
      </header>
      <div className="record-grid">
        <form className="form-card workspace-stack" onSubmit={addNote}>
          <h2>Add timeline note</h2>
          <textarea name="text" required placeholder="Operational note" />
          <label>
            <input name="isSensitive" type="checkbox" /> Sensitive note
          </label>
          <button type="submit">Add note</button>
        </form>
        <form className="form-card workspace-stack" onSubmit={saveConsent}>
          <h2>Consent</h2>
          <label>
            <input
              name="marketingConsent"
              type="checkbox"
              defaultChecked={Boolean(customer.marketingEmailConsent)}
            />{" "}
            Email marketing
          </label>
          <label>
            <input
              name="smsConsent"
              type="checkbox"
              defaultChecked={Boolean(customer.marketingSmsConsent)}
            />{" "}
            SMS marketing
          </label>
          <label>
            <input
              name="whatsAppConsent"
              type="checkbox"
              defaultChecked={Boolean(customer.marketingWhatsAppConsent)}
            />{" "}
            WhatsApp marketing
          </label>
          <button type="submit">Update consent</button>
        </form>
      </div>
      <div className="form-card">
        <h2>Timeline</h2>
        {((profile.notes as JsonRecord[]) ?? []).map((note) => (
          <article className="record-row" key={value(note, "id")}>
            <strong>{value(note, "content")}</strong>
            <small>{value(note, "createdAtUtc")}</small>
          </article>
        ))}
      </div>
    </section>
  );
}

export function ServiceResourceProfile({ recordId }: { recordId: string }) {
  const [service, setService] = useState<JsonRecord>();
  const [requirements, setRequirements] = useState<JsonRecord[]>([]);
  const [resources, setResources] = useState<JsonRecord[]>([]);
  const [message, setMessage] = useState("");

  const load = useCallback(async () => {
    const [nextService, nextRequirements, nextResources] = await Promise.all([
      request<JsonRecord>(`/services/${recordId}`),
      request<JsonRecord[]>(`/services/${recordId}/resource-requirements`),
      request<JsonRecord[]>("/resources"),
    ]);
    setService(nextService);
    setRequirements(nextRequirements);
    setResources(nextResources);
  }, [recordId]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      void load().catch((error: Error) => setMessage(error.message));
    }, 0);
    return () => window.clearTimeout(timeout);
  }, [load]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const resourceId = String(data.get("resourceId") ?? "");
    try {
      await request(`/services/${recordId}/resource-requirements`, {
        method: "PUT",
        body: JSON.stringify(
          resourceId
            ? [
                {
                  specificResourceId: resourceId,
                  resourceType: "Station",
                  quantityRequired: Number(data.get("quantity") ?? 1),
                  isMandatory: true,
                },
              ]
            : [],
        ),
      });
      setMessage("Resource requirements saved.");
      await load();
    } catch (error) {
      setMessage((error as Error).message);
    }
  }

  if (!service) return <p>{message || "Loading service..."}</p>;
  return (
    <section className="workspace-stack">
      <header className="page-heading">
        <div>
          <p className="eyebrow">Service operations</p>
          <h1>{value(service, "name")}</h1>
        </div>
        <p>{message}</p>
      </header>
      <form className="form-card workspace-stack" onSubmit={save}>
        <h2>Required resource</h2>
        <select
          name="resourceId"
          defaultValue={value(requirements[0] ?? {}, "specificResourceId")}
        >
          <option value="">No dedicated resource</option>
          {resources.map((resource) => (
            <option key={value(resource, "id")} value={value(resource, "id")}>
              {value(resource, "name")}
            </option>
          ))}
        </select>
        <label>
          Quantity{" "}
          <input
            name="quantity"
            type="number"
            min="1"
            defaultValue={Number(requirements[0]?.quantityRequired ?? 1)}
          />
        </label>
        <button type="submit">Save requirement</button>
      </form>
    </section>
  );
}
