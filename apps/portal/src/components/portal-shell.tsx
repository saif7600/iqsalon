"use client";
import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { apiRequest } from "@atiqsalon/sdk";
import { Breadcrumb } from "@atiqsalon/ui";
import {
  BarChart3,
  Boxes,
  Building2,
  CalendarCheck,
  CalendarDays,
  ChartNoAxesCombined,
  Clock3,
  HeartHandshake,
  LayoutDashboard,
  PackageSearch,
  ReceiptText,
  Scissors,
  ServerCog,
  ShieldCheck,
  ShoppingBag,
  SlidersHorizontal,
  Sparkles,
  Store,
  UserRound,
  UsersRound,
  type LucideIcon,
} from "lucide-react";
type Me = { displayName: string; roles: string[]; permissions: string[] };
type Branch = { id: string; name: string };
type Link = {
  label: string;
  href: string;
  icon: LucideIcon;
  permission?: string;
};
const groups: { label: string; links: Link[] }[] = [
  {
    label: "Operate",
    links: [
      { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
      {
        label: "Calendar",
        href: "/calendar",
        icon: CalendarDays,
        permission: "appointments.read",
      },
      {
        label: "Appointments",
        href: "/appointments",
        icon: CalendarCheck,
        permission: "appointments.read",
      },
      {
        label: "Point of sale",
        href: "/pos",
        icon: ShoppingBag,
        permission: "pos.access",
      },
      {
        label: "Sales",
        href: "/pos/sales",
        icon: ReceiptText,
        permission: "reports.sales",
      },
    ],
  },
  {
    label: "People",
    links: [
      {
        label: "Customers",
        href: "/customers",
        icon: UsersRound,
        permission: "customers.read",
      },
      {
        label: "Staff",
        href: "/staff",
        icon: UserRound,
        permission: "staff.read",
      },
      {
        label: "Workforce",
        href: "/workforce",
        icon: Clock3,
        permission: "shifts.read",
      },
      {
        label: "Performance",
        href: "/performance",
        icon: ChartNoAxesCombined,
        permission: "performance.read",
      },
      {
        label: "Loyalty & referrals",
        href: "/growth",
        icon: HeartHandshake,
        permission: "loyalty.read",
      },
    ],
  },
  {
    label: "Catalogue",
    links: [
      {
        label: "Services",
        href: "/services",
        icon: Scissors,
        permission: "services.read",
      },
      {
        label: "Resources",
        href: "/resources",
        icon: Boxes,
        permission: "resources.read",
      },
      {
        label: "Inventory",
        href: "/inventory",
        icon: PackageSearch,
        permission: "inventory.read",
      },
    ],
  },
  {
    label: "Control",
    links: [
      {
        label: "Commercial reports",
        href: "/reports/commercial",
        icon: BarChart3,
        permission: "reports.sales",
      },
      {
        label: "Commercial admin",
        href: "/commercial/admin",
        icon: SlidersHorizontal,
        permission: "settings.update",
      },
      {
        label: "Organization",
        href: "/settings/organization",
        icon: Building2,
        permission: "organization.read",
      },
      {
        label: "Branches",
        href: "/settings/branches",
        icon: Store,
        permission: "branch.read",
      },
      {
        label: "Audit",
        href: "/audit",
        icon: ShieldCheck,
        permission: "audit.read",
      },
    ],
  },
  {
    label: "Platform",
    links: [
      {
        label: "SaaS Admin",
        href: "/platform",
        icon: ServerCog,
        permission: "platform.dashboard.read",
      },
    ],
  },
];
const PortalShellContext = createContext(false);

export function PortalShell({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  const nested = useContext(PortalShellContext);
  if (nested) return <>{children}</>;
  return (
    <PortalShellContext.Provider value>
      <PortalShellFrame title={title}>{children}</PortalShellFrame>
    </PortalShellContext.Provider>
  );
}

function PortalShellFrame({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const initialPathname = useRef(pathname);
  const [me, setMe] = useState<Me | null>(null);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [branchId, setBranchId] = useState("");
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [theme, setTheme] = useState<"light" | "dark">("light");
  useEffect(() => {
    const stored = localStorage.getItem("atiqsalon_theme");
    const resolved =
      stored === "dark" ||
      (stored !== "light" &&
        window.matchMedia("(prefers-color-scheme: dark)").matches)
        ? "dark"
        : "light";
    document.documentElement.dataset.theme = resolved;
    const frame = window.requestAnimationFrame(() => setTheme(resolved));
    return () => window.cancelAnimationFrame(frame);
  }, []);
  useEffect(() => {
    Promise.all([apiRequest<Me>("/me"), apiRequest<Branch[]>("/branches")])
      .then(([user, rows]) => {
        setMe(user);
        setBranches(rows);
        setBranchId(sessionStorage.getItem("atiqsalon_branch") ?? "");
      })
      .catch(() =>
        window.location.assign(
          `/login?returnUrl=${encodeURIComponent(initialPathname.current)}`,
        ),
      );
  }, []);
  const visible = useMemo(
    () =>
      groups
        .map((group) => ({
          ...group,
          links: group.links.filter(
            (link) =>
              !link.permission || me?.permissions.includes(link.permission),
          ),
        }))
        .filter((group) => group.links.length),
    [me],
  );
  function chooseBranch(value: string) {
    setBranchId(value);
    sessionStorage.setItem("atiqsalon_branch", value);
    window.dispatchEvent(
      new CustomEvent("atiqsalon:branch", { detail: value }),
    );
  }
  async function logout() {
    try {
      await apiRequest("/auth/logout", { method: "POST" });
    } finally {
      sessionStorage.removeItem("atiqsalon_branch");
      window.location.assign("/login");
    }
  }
  function toggleTheme() {
    const next = theme === "light" ? "dark" : "light";
    setTheme(next);
    localStorage.setItem("atiqsalon_theme", next);
    document.documentElement.dataset.theme = next;
  }
  function search(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = query.trim().toLowerCase();
    if (!value) return;
    const match = visible
      .flatMap((group) => group.links)
      .find((link) => link.label.toLowerCase().includes(value));
    if (match) {
      setQuery("");
      router.push(match.href);
    }
  }
  return (
    <div className="portal">
      <a className="skip-link" href="#portal-workspace">
        Skip to workspace
      </a>
      <aside className={`sidebar ${open ? "is-open" : ""}`}>
        <div className="sidebar-brand">
          <Link className="wordmark" href="/dashboard">
            <span className="brand-mark" aria-hidden="true">
              A
            </span>
            <span>
              AtiqSalon AI
              <small>Business Operating System</small>
            </span>
          </Link>
          <button
            className="icon-button mobile-only"
            onClick={() => setOpen(false)}
            aria-label="Close menu"
          >
            ×
          </button>
        </div>
        <div className="tenant-context">
          <strong>{me?.displayName ?? "Loading workspace..."}</strong>
          <small>{me?.roles.join(", ")}</small>
        </div>
        <nav className="side-links" aria-label="Portal">
          {visible.map((group) => (
            <section key={group.label}>
              <small>{group.label}</small>
              {group.links.map((link) => (
                <Link
                  className={
                    pathname === link.href ||
                    pathname.startsWith(`${link.href}/`)
                      ? "active"
                      : ""
                  }
                  aria-current={pathname === link.href ? "page" : undefined}
                  key={link.href}
                  href={link.href}
                >
                  <link.icon aria-hidden="true" />
                  {link.label}
                </Link>
              ))}
            </section>
          ))}
        </nav>
        <button className="button secondary sidebar-logout" onClick={logout}>
          Sign out
        </button>
      </aside>
      {open && (
        <button
          className="portal-scrim"
          aria-label="Close menu"
          onClick={() => setOpen(false)}
        />
      )}
      <main className="portal-main" id="portal-workspace">
        <header className="portal-header">
          <button
            className="icon-button mobile-only"
            onClick={() => setOpen(true)}
            aria-label="Open menu"
          >
            ☰
          </button>
          <label className="branch-picker">
            <span>Branch</span>
            <select
              value={branchId}
              onChange={(e) => chooseBranch(e.target.value)}
            >
              <option value="">All permitted branches</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
          </label>
          <form className="portal-search" role="search" onSubmit={search}>
            <label className="sr-only" htmlFor="portal-search">
              Search modules
            </label>
            <input
              id="portal-search"
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search modules..."
            />
          </form>
          <Link className="button portal-create" href="/appointments/new">
            New appointment
          </Link>
          <Link className="iqai-entry" href="/iqai">
            <Sparkles aria-hidden="true" />
            Ask IQAI
          </Link>
          <button
            className="theme-switch"
            type="button"
            onClick={toggleTheme}
            aria-label={`Switch to ${theme === "light" ? "dark" : "light"} mode`}
          >
            <span aria-hidden="true">
              {theme === "light" ? "Dark" : "Light"}
            </span>
          </button>
        </header>
        <div className="portal-content">
          <Breadcrumb items={["Workspace", title]} />
          {children}
        </div>
      </main>
    </div>
  );
}
