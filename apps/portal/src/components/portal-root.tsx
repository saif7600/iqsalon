"use client";

import { usePathname } from "next/navigation";
import { PortalShell } from "./portal-shell";

const protectedPaths = [
  "/dashboard",
  "/settings",
  "/platform",
  "/audit",
  "/onboarding",
  "/calendar",
  "/appointments",
  "/customers",
  "/services",
  "/staff",
  "/resources",
  "/pos",
  "/reports",
  "/commercial",
  "/inventory",
  "/workforce",
  "/performance",
  "/growth",
  "/iqai",
];

const routeTitles: Record<string, string> = {
  dashboard: "Dashboard",
  settings: "Settings",
  platform: "SaaS Administration",
  audit: "Audit",
  onboarding: "Onboarding",
  calendar: "Calendar",
  appointments: "Appointments",
  customers: "Customers",
  services: "Services",
  staff: "Staff",
  resources: "Resources",
  pos: "Point of sale",
  reports: "Reports",
  commercial: "Commercial administration",
  inventory: "Inventory",
  workforce: "Workforce",
  performance: "Performance",
  growth: "Loyalty & referrals",
  iqai: "IQAI",
};

export function PortalRoot({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const protectedRoute = protectedPaths.some(
    (path) => pathname === path || pathname.startsWith(`${path}/`),
  );
  if (!protectedRoute) return <>{children}</>;

  const segment = pathname.split("/").filter(Boolean)[0] ?? "dashboard";
  return (
    <PortalShell title={routeTitles[segment] ?? "Workspace"}>
      {children}
    </PortalShell>
  );
}
