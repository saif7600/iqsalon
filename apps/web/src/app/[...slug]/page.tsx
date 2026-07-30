import type { Metadata } from "next";
import { notFound } from "next/navigation";
const pages: Record<
  string,
  { title: string; description: string; body: string }
> = {
  features: {
    title: "A connected salon operating system",
    description: "Explore the AtiqSalon AI platform foundation.",
    body: "Bookings, customer relationships, staff, payments, inventory, marketing and reporting are designed as connected modules with shared identity and controls.",
  },
  ai: {
    title: "AI employees, with boundaries",
    description: "Responsible AI assistance for beauty businesses.",
    body: "AI capabilities will operate within explicit permissions, human review and tenant-isolated business context. No autonomous capability is claimed before it is implemented and verified.",
  },
  "online-booking": {
    title: "Online booking that fits your operation",
    description: "A future booking experience shaped by real availability.",
    body: "The booking engine will respect services, staff eligibility, resources, locations, timing rules and customer preferences.",
  },
  "point-of-sale": {
    title: "A point of sale connected to the day",
    description: "A future POS architecture for salon operations.",
    body: "Payments, services, products, tips, memberships and receipts will be designed as auditable parts of one transaction model.",
  },
  "staff-management": {
    title: "Give every team member the right view",
    description: "Permission-aware staff and commission foundations.",
    body: "Profiles, schedules, skills, branch assignments and commissions will be implemented with explicit access controls.",
  },
  inventory: {
    title: "Inventory intelligence without guesswork",
    description: "Future stock control across branches.",
    body: "Products, movements, purchasing, counts and alerts will share one traceable inventory ledger.",
  },
  "multi-branch": {
    title: "One view across every location",
    description: "Multi-branch foundations for groups and franchises.",
    body: "Organization, branch and platform contexts are separated by design, with tenant isolation enforced in the data layer.",
  },
  "solutions/beauty-salons": {
    title: "For beauty salons",
    description: "An operating foundation for modern beauty salons.",
    body: "Coordinate services, teams, customer journeys and branch operations from one coherent platform.",
  },
  "solutions/barbers": {
    title: "For barbershops",
    description: "A focused platform for barber operations.",
    body: "Support walk-in and scheduled service models while keeping team access and branch operations clear.",
  },
  "solutions/spas": {
    title: "For spas",
    description: "Designed for considered guest experiences.",
    body: "The future service and resource model will support rooms, therapists, treatment timing and guest preferences.",
  },
  "solutions/nail-salons": {
    title: "For nail salons",
    description: "A platform for detailed, repeatable service delivery.",
    body: "Service variations, technician skills, timings and customer history will live in one permissioned system.",
  },
  "solutions/home-services": {
    title: "For home-service beauty teams",
    description: "Architecture ready for mobile service operations.",
    body: "Future workflows will account for service areas, travel time, mobile teams and customer locations.",
  },
  pricing: {
    title: "Clear pricing, before commitment",
    description: "AtiqSalon AI pricing approach.",
    body: "Commercial plans are not yet published. Final pricing will clearly state included capabilities, limits and support before paid availability.",
  },
  resources: {
    title: "Practical resources for operators",
    description: "Guides for beauty and wellness operators.",
    body: "Resources will focus on operating discipline, customer experience, team enablement, security and responsible adoption of AI.",
  },
  "book-demo": {
    title: "Book a product conversation",
    description: "Discuss AtiqSalon AI with our team.",
    body: "Demo scheduling is not connected in this foundation phase. Contact the team to register interest; no submission will be presented as sent until a real service is configured.",
  },
  contact: {
    title: "Contact AtiqSalon AI",
    description: "Contact the AtiqSalon AI team.",
    body: "A verified contact channel will be published before public launch. This page intentionally does not include a non-functional form.",
  },
  privacy: {
    title: "Privacy",
    description: "AtiqSalon AI privacy overview.",
    body: "A jurisdiction-reviewed privacy notice will be published before collecting public personal data. The platform is designed for data minimization, tenant isolation and auditable access.",
  },
  terms: {
    title: "Terms",
    description: "AtiqSalon AI terms overview.",
    body: "Final service terms will be published before commercial availability. No trial or paid contract is implied by this development foundation.",
  },
  security: {
    title: "Security by design",
    description: "Security principles behind AtiqSalon AI.",
    body: "The foundation uses tenant-scoped data access, permission-based authorization, secure sessions, structured audit events, secret separation and automated quality gates.",
  },
};
export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const page = pages[slug.join("/")];
  return page ? { title: page.title, description: page.description } : {};
}
export default async function RoutePage({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}) {
  const { slug } = await params;
  const page = pages[slug.join("/")];
  if (!page) notFound();
  return (
    <main className="route-page">
      <div className="container content">
        <p className="eyebrow">AtiqSalon AI</p>
        <h1 style={{ fontFamily: "var(--font-display)" }}>{page.title}</h1>
        <p className="lead">{page.description}</p>
        <p>{page.body}</p>
        <div className="actions">
          <a className="button" href="/book-demo">
            Book a Demo
          </a>
          <a className="button secondary" href="/features">
            Explore Product
          </a>
        </div>
      </div>
    </main>
  );
}
