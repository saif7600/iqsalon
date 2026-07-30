import { Badge, Card } from "@atiqsalon/ui";
const modules = [
  [
    "Booking, without the chaos",
    "Shape availability, services and client journeys around the way your team actually works.",
  ],
  [
    "One calm front desk",
    "Bring customer context, daily operations and team coordination into a single workspace.",
  ],
  [
    "Decisions with context",
    "See the signals that matter across branches without turning your business into a spreadsheet.",
  ],
];
export default function Home() {
  return (
    <main>
      <section className="hero">
        <div className="container hero-grid">
          <div>
            <Badge>AtiqSalon AI</Badge>
            <h1 style={{ fontFamily: "var(--font-display)" }}>
              Run Your Entire Salon With AI
            </h1>
            <p className="lead">
              Manage bookings, customers, staff, payments, inventory, marketing,
              and business performance from one intelligent platform.
            </p>
            <div className="actions">
              <a className="button" href="http://localhost:3001/register">
                Start Free Trial
              </a>
              <a className="button secondary" href="/book-demo">
                Book a Demo
              </a>
            </div>
          </div>
          <div
            className="dashboard"
            aria-label="Illustrative product interface"
          >
            <div className="dash-top">
              <div className="dash-panel">
                <small>Today</small>
                <b>Studio overview</b>
                <p>Foundational setup and team access</p>
              </div>
              <div className="dash-panel">
                <small>Platform</small>
                <b>Healthy</b>
              </div>
            </div>
            <div className="dash-grid">
              <div className="dash-panel">
                <small>Setup</small>
                <b>4 of 6</b>
                <p>Complete your organization profile</p>
              </div>
              <div className="dash-panel">
                <small>Branches</small>
                <b>1 active</b>
                <p>Dubai flagship</p>
              </div>
            </div>
          </div>
        </div>
      </section>
      <section className="section">
        <div className="container">
          <p className="eyebrow">A more considered way to operate</p>
          <h2 style={{ fontFamily: "var(--font-display)" }}>
            Built around the rhythm of beauty and wellness businesses.
          </h2>
          <div className="section-grid">
            {modules.map(([title, text]) => (
              <Card key={title}>
                <h3>{title}</h3>
                <p>{text}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>
      <section className="section band">
        <div className="container">
          <p className="eyebrow">AI employees</p>
          <h2 style={{ fontFamily: "var(--font-display)" }}>
            Assistance that respects your team, customers and operating rules.
          </h2>
          <div className="section-grid">
            <article>
              <h3>Front desk support</h3>
              <p>
                Architecture for handling routine questions and booking
                assistance with human oversight.
              </p>
            </article>
            <article>
              <h3>Business guidance</h3>
              <p>
                Future intelligence grounded in permissioned, tenant-isolated
                operational data.
              </p>
            </article>
            <article>
              <h3>Marketing assistance</h3>
              <p>
                Campaign preparation designed around consent, brand voice and
                review.
              </p>
            </article>
          </div>
        </div>
      </section>
      <section className="section">
        <div className="container">
          <p className="eyebrow">One platform</p>
          <h2 style={{ fontFamily: "var(--font-display)" }}>
            Booking, point of sale, staff, inventory and every branch.
          </h2>
          <div className="section-grid">
            {[
              "Online booking",
              "Point of sale",
              "Staff & commissions",
              "Inventory intelligence",
              "Multi-branch control",
              "Customer relationships",
            ].map((x) => (
              <Card key={x}>
                <h3>{x}</h3>
                <p>
                  Designed as a native module of one operating system, with
                  shared identity, permissions and audit controls.
                </p>
              </Card>
            ))}
          </div>
        </div>
      </section>
      <section className="section">
        <div className="container">
          <p className="eyebrow">For your business model</p>
          <h2 style={{ fontFamily: "var(--font-display)" }}>
            From independent studios to franchise networks.
          </h2>
          <div className="section-grid">
            {[
              "Beauty salons & nail studios",
              "Barbershops & spas",
              "Wellness & home services",
            ].map((x) => (
              <Card key={x}>
                <h3>{x}</h3>
                <p>
                  Flexible organization and branch foundations ready for the
                  workflows your business needs.
                </p>
              </Card>
            ))}
          </div>
        </div>
      </section>
      <section className="section">
        <div className="container">
          <p className="eyebrow">Pricing preview</p>
          <h2 style={{ fontFamily: "var(--font-display)" }}>
            Plans that can grow with your operation.
          </h2>
          <div className="price-grid">
            {["Essential", "Growth", "Multi-location"].map((x) => (
              <Card key={x}>
                <h3>{x}</h3>
                <p>
                  Final plan inclusions and commercial terms will be published
                  before paid availability.
                </p>
                <a href="/pricing">View pricing approach →</a>
              </Card>
            ))}
          </div>
        </div>
      </section>
      <section className="section faq">
        <div className="container">
          <h2 style={{ fontFamily: "var(--font-display)" }}>
            Frequently asked questions
          </h2>
          <details>
            <summary>Is AtiqSalon AI available today?</summary>
            <p>
              The platform foundation is in development. Trial availability will
              be announced when the core workflows pass production readiness
              checks.
            </p>
          </details>
          <details>
            <summary>Will Arabic be supported?</summary>
            <p>
              Yes. The architecture supports English and Arabic with LTR and RTL
              layouts.
            </p>
          </details>
          <details>
            <summary>Can it support multiple branches?</summary>
            <p>
              Yes. Organizations and branches are foundational platform
              concepts, with permissions scoped accordingly.
            </p>
          </details>
        </div>
      </section>
      <section className="section band">
        <div className="container">
          <h2 style={{ fontFamily: "var(--font-display)" }}>
            Give your team a calmer operating day.
          </h2>
          <div className="actions">
            <a className="button" href="http://localhost:3001/register">
              Start Free Trial
            </a>
            <a
              className="button secondary"
              style={{ color: "white" }}
              href="/book-demo"
            >
              Book a Demo
            </a>
          </div>
        </div>
      </section>
    </main>
  );
}
