"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import {
  Badge,
  Button,
  Card,
  ErrorState,
  FormActions,
  FormField,
  FormSection,
  LoadingState,
  PageTitle,
} from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Module = "services" | "staff" | "customers" | "resources";
type RecordItem = {
  id: string;
  name?: string;
  displayName?: string;
  code?: string;
  employeeCode?: string;
  customerNumber?: string;
  email?: string;
  phone?: string;
  type?: string;
  durationMinutes?: number;
  basePrice?: number;
  currencyCode?: string;
  employmentStatus?: string;
  capacity?: number;
  isActive?: boolean;
};
type Branch = { id: string; organizationId: string; name: string };
type Category = { id: string; organizationId: string; name: string };
type Me = { roles: string[] };

const labels: Record<Module, string> = {
  services: "Services",
  staff: "Staff",
  customers: "Customers",
  resources: "Resources",
};

export function RecordsWorkspace({
  module,
  create = false,
}: {
  module: Module;
  create?: boolean;
}) {
  const [items, setItems] = useState<RecordItem[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const title = create
    ? `New ${labels[module].slice(0, -1).toLowerCase()}`
    : labels[module];

  useEffect(() => {
    async function load() {
      try {
        const requests: Promise<unknown>[] = [
          apiRequest<Branch[]>("/branches"),
          apiRequest<Me>("/me"),
        ];
        if (!create) requests.push(apiRequest<RecordItem[]>(`/${module}`));
        if (module === "services")
          requests.push(apiRequest<Category[]>("/service-categories"));
        const result = await Promise.all(requests);
        setBranches(result[0] as Branch[]);
        setRoles((result[1] as Me).roles);
        let cursor = 2;
        if (!create) setItems(result[cursor++] as RecordItem[]);
        if (module === "services") setCategories(result[cursor] as Category[]);
      } catch {
        setError(`${labels[module]} could not be loaded.`);
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, [create, module]);

  const canCreate = useMemo(() => {
    if (
      roles.includes("OrganizationOwner") ||
      roles.includes("OrganizationAdmin")
    )
      return true;
    return (
      module === "customers" &&
      (roles.includes("Receptionist") || roles.includes("BranchManager"))
    );
  }, [module, roles]);

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return items;
    return items.filter((item) =>
      [
        item.name,
        item.displayName,
        item.code,
        item.employeeCode,
        item.customerNumber,
        item.email,
      ].some((value) => value?.toLowerCase().includes(term)),
    );
  }, [items, search]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    const data = new FormData(event.currentTarget);
    const branch =
      branches.find((item) => item.id === data.get("branchId")) ?? branches[0];
    if (!branch) return;
    let body: Record<string, unknown>;
    if (module === "services") {
      const category = categories.find(
        (item) => item.id === data.get("categoryId"),
      );
      if (!category) return;
      body = {
        organizationId: category.organizationId,
        categoryId: category.id,
        name: data.get("name"),
        code: data.get("code"),
        durationMinutes: Number(data.get("durationMinutes")),
        cleanupMinutes: Number(data.get("cleanupMinutes")),
        basePrice: Number(data.get("basePrice")),
        currencyCode: "AED",
        onlineBookingEnabled: data.get("onlineBookingEnabled") === "on",
      };
    } else if (module === "staff") {
      body = {
        organizationId: branch.organizationId,
        defaultBranchId: branch.id,
        employeeCode: data.get("code"),
        firstName: data.get("firstName"),
        lastName: data.get("lastName"),
        email: data.get("email"),
        employmentStatus: "Active",
        onlineBookingEnabled: data.get("onlineBookingEnabled") === "on",
      };
    } else if (module === "customers") {
      body = {
        organizationId: branch.organizationId,
        preferredBranchId: branch.id,
        firstName: data.get("firstName"),
        lastName: data.get("lastName"),
        email: data.get("email"),
        phoneCountryCode: "+971",
        phoneNumber: data.get("phone"),
        source: "Reception",
      };
    } else {
      body = {
        organizationId: branch.organizationId,
        branchId: branch.id,
        name: data.get("name"),
        code: data.get("code"),
        type: data.get("type"),
        capacity: Number(data.get("capacity")),
        onlineBookingVisible: data.get("onlineBookingVisible") === "on",
      };
    }
    try {
      await apiRequest(`/${module}`, {
        method: "POST",
        body: JSON.stringify(body),
      });
      window.location.assign(`/${module}`);
    } catch {
      setError(
        `The ${labels[module].slice(0, -1).toLowerCase()} could not be created. Check required values and permissions.`,
      );
    }
  }

  return (
    <PortalShell title={title}>
      <PageTitle eyebrow="Operating records" title={title}>
        Live tenant-scoped records. Changes are validated and audited by the
        API.
      </PageTitle>
      {error && <ErrorState message={error} />}
      {loading ? (
        <LoadingState />
      ) : create ? (
        !canCreate ? (
          <ErrorState message="You do not have permission to create this record." />
        ) : (
          <Card className="record-form-shell">
            <form className="record-form" onSubmit={submit}>
              <FormSection
                title={
                  module === "customers" || module === "staff"
                    ? "Personal information"
                    : "Record details"
                }
                description={
                  module === "customers"
                    ? "Add the customer details your reception team will use for bookings and communication."
                    : module === "staff"
                      ? "Create the team member's operational profile. Access permissions are managed separately."
                      : `Define the ${labels[module].slice(0, -1).toLowerCase()} as it should appear across the salon.`
                }
              >
              {module === "staff" || module === "customers" ? (
                <>
                  <FormField label="First name" htmlFor="firstName" required>
                    <input id="firstName" name="firstName" autoComplete="given-name" required />
                  </FormField>
                  <FormField label="Last name" htmlFor="lastName" required>
                    <input id="lastName" name="lastName" autoComplete="family-name" required />
                  </FormField>
                </>
              ) : (
                <FormField
                  label={module === "services" ? "Service name" : "Resource name"}
                  htmlFor="name"
                  hint="Use a clear customer-facing name."
                  required
                >
                  <input id="name" name="name" required />
                </FormField>
              )}
              {module !== "customers" && (
                <FormField
                  label={module === "staff" ? "Employee code" : "Internal code"}
                  htmlFor="code"
                  hint="A short unique reference used in reports."
                  required
                >
                  <input id="code" name="code" autoCapitalize="characters" required />
                </FormField>
              )}
              {(module === "staff" || module === "customers") && (
                <FormField label="Email address" htmlFor="email" hint="Optional">
                  <input id="email" name="email" type="email" autoComplete="email" />
                </FormField>
              )}
              {module === "customers" && (
                <FormField
                  label="Mobile number"
                  htmlFor="phone"
                  hint="UAE country code +971 is applied automatically."
                  required
                >
                  <input id="phone" name="phone" type="tel" autoComplete="tel-national" placeholder="50 123 4567" required />
                </FormField>
              )}
              </FormSection>
              {module === "services" && (
                <FormSection
                  title="Duration and pricing"
                  description="Set the operational timing and standard selling price."
                >
                  <FormField label="Category" htmlFor="categoryId" required>
                    <select name="categoryId" required>
                      <option value="">Select category</option>
                      {categories.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                  </FormField>
                  <FormField label="Service duration" htmlFor="durationMinutes" hint="Minutes" required>
                    <input
                      id="durationMinutes"
                      name="durationMinutes"
                      type="number"
                      min="1"
                      defaultValue="45"
                      required
                    />
                  </FormField>
                  <FormField label="Cleanup time" htmlFor="cleanupMinutes" hint="Buffer before the next booking, in minutes." required>
                    <input
                      id="cleanupMinutes"
                      name="cleanupMinutes"
                      type="number"
                      min="0"
                      defaultValue="10"
                      required
                    />
                  </FormField>
                  <FormField label="Standard price" htmlFor="basePrice" hint="AED, before any discount." required>
                    <input
                      id="basePrice"
                      name="basePrice"
                      type="number"
                      min="0"
                      step="0.01"
                      required
                    />
                  </FormField>
                </FormSection>
              )}
              {module !== "services" && (
                <FormSection
                  title="Operational assignment"
                  description="Choose where this record is primarily managed."
                >
                <FormField label={module === "customers" ? "Preferred branch" : "Home branch"} htmlFor="branchId" required>
                  <select name="branchId" required>
                    {branches.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.name}
                      </option>
                    ))}
                  </select>
                </FormField>
              {module === "resources" && (
                <>
                  <FormField label="Resource type" htmlFor="type" required>
                    <select name="type">
                      <option>Chair</option>
                      <option>Room</option>
                      <option>Bed</option>
                      <option>Station</option>
                      <option>Equipment</option>
                      <option>Vehicle</option>
                      <option>Other</option>
                    </select>
                  </FormField>
                  <FormField label="Simultaneous capacity" htmlFor="capacity" hint="How many bookings can use this resource at once." required>
                    <input
                      id="capacity"
                      name="capacity"
                      type="number"
                      min="1"
                      defaultValue="1"
                      required
                    />
                  </FormField>
                </>
              )}
              {(module === "services" || module === "staff") && (
                <label className="choice-card field-wide">
                  <input name="onlineBookingEnabled" type="checkbox" />
                  <span>
                    Available for online booking
                    <small>Customers can select this option from the public booking experience.</small>
                  </span>
                </label>
              )}
              {module === "resources" && (
                <label className="choice-card field-wide">
                  <input name="onlineBookingVisible" type="checkbox" />
                  <span>
                    Visible online
                    <small>Show this resource where the booking experience supports resource selection.</small>
                  </span>
                </label>
              )}
              </FormSection>
              )}
              <FormActions note="Required fields are marked with an asterisk.">
                <a className="button secondary" href={`/${module}`}>Cancel</a>
                <Button type="submit">
                  Create {labels[module].slice(0, -1).toLowerCase()}
                </Button>
              </FormActions>
            </form>
          </Card>
        )
      ) : (
        <>
          <div className="records-toolbar">
            <input
              className="input"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={`Search ${labels[module].toLowerCase()}`}
            />
            {canCreate && (
              <a className="button" href={`/${module}/new`}>
                New {labels[module].slice(0, -1).toLowerCase()}
              </a>
            )}
          </div>
          <div
            className="records-table"
            role="table"
            aria-label={labels[module]}
          >
            {visible.map((item) => (
              <a
                className="record-row"
                role="row"
                href={
                  module === "resources"
                    ? "/resources"
                    : `/${module}/${item.id}`
                }
                key={item.id}
              >
                <div>
                  <strong>{item.name ?? item.displayName}</strong>
                  <small>
                    {item.code ??
                      item.employeeCode ??
                      item.customerNumber ??
                      item.email}
                  </small>
                </div>
                <span>
                  {module === "services"
                    ? `${item.durationMinutes} min · ${item.basePrice} ${item.currencyCode}`
                    : module === "resources"
                      ? `${item.type} · capacity ${item.capacity}`
                      : module === "staff"
                        ? item.employmentStatus
                        : item.email}
                </span>
                <Badge>{item.isActive === false ? "Inactive" : "Active"}</Badge>
              </a>
            ))}
            {visible.length === 0 && (
              <Card>
                <h2>No matching records</h2>
              </Card>
            )}
          </div>
        </>
      )}
    </PortalShell>
  );
}
