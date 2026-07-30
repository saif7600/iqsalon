export type Locale = "en" | "ar";
export type TenantStatus = "active" | "suspended" | "closed";
export interface ApiProblem {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  traceId?: string;
}
export interface OrganizationSummary {
  id: string;
  legalName: string;
  tradingName: string;
  branchCount: number;
  activeUserCount: number;
}
