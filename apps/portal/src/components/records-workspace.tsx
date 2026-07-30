"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
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
          <Card>
            <form className="record-form" onSubmit={submit}>
              {module === "staff" || module === "customers" ? (
                <>
                  <label>
                    First name
                    <input name="firstName" required />
                  </label>
                  <label>
                    Last name
                    <input name="lastName" required />
                  </label>
                </>
              ) : (
                <label>
                  Name
                  <input name="name" required />
                </label>
              )}
              {module !== "customers" && (
                <label>
                  Code
                  <input name="code" required />
                </label>
              )}
              {(module === "staff" || module === "customers") && (
                <label>
                  Email
                  <input name="email" type="email" />
                </label>
              )}
              {module === "customers" && (
                <label>
                  Phone
                  <input name="phone" required />
                </label>
              )}
              {module === "services" && (
                <>
                  <label>
                    Category
                    <select name="categoryId" required>
                      <option value="">Select category</option>
                      {categories.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Duration minutes
                    <input
                      name="durationMinutes"
                      type="number"
                      min="1"
                      defaultValue="45"
                      required
                    />
                  </label>
                  <label>
                    Cleanup minutes
                    <input
                      name="cleanupMinutes"
                      type="number"
                      min="0"
                      defaultValue="10"
                      required
                    />
                  </label>
                  <label>
                    Base price
                    <input
                      name="basePrice"
                      type="number"
                      min="0"
                      step="0.01"
                      required
                    />
                  </label>
                </>
              )}
              {module !== "services" && (
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
              )}
              {module === "resources" && (
                <>
                  <label>
                    Type
                    <select name="type">
                      <option>Chair</option>
                      <option>Room</option>
                      <option>Bed</option>
                      <option>Station</option>
                      <option>Equipment</option>
                      <option>Vehicle</option>
                      <option>Other</option>
                    </select>
                  </label>
                  <label>
                    Capacity
                    <input
                      name="capacity"
                      type="number"
                      min="1"
                      defaultValue="1"
                      required
                    />
                  </label>
                </>
              )}
              {(module === "services" || module === "staff") && (
                <label className="check">
                  <input name="onlineBookingEnabled" type="checkbox" />{" "}
                  Available online
                </label>
              )}
              {module === "resources" && (
                <label className="check">
                  <input name="onlineBookingVisible" type="checkbox" /> Visible
                  online
                </label>
              )}
              <Button type="submit">
                Create {labels[module].slice(0, -1).toLowerCase()}
              </Button>
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
