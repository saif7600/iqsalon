import Link from "next/link";

export default function CustomerHome() {
  return (
    <main className="mobile-shell">
      <header className="app-bar">
        <Link className="brand" href="/">
          <span>A</span>
          AtiqSalon
        </Link>
        <span className="channel-label">Customer</span>
      </header>

      <section className="hero">
        <p className="eyebrow">Your salon, close at hand</p>
        <h1>Care planned around your day.</h1>
        <p>
          This secure customer channel is being connected to verified customer
          identity and booking services. Operator credentials cannot be used
          here.
        </p>
      </section>

      <section className="status-card" aria-labelledby="access-heading">
        <div>
          <span className="status-dot" aria-hidden="true" />
          <p className="eyebrow">Channel status</p>
        </div>
        <h2 id="access-heading">Secure access boundary established</h2>
        <p>
          The installable application shell and conservative offline fallback
          are active. Customer sign-in will open only after passwordless
          verification, session revocation, and cross-customer denial are
          implemented and tested.
        </p>
      </section>

      <nav className="bottom-nav" aria-label="Customer application">
        <Link aria-current="page" href="/">
          Home
        </Link>
        <span aria-disabled="true">Book</span>
        <span aria-disabled="true">Visits</span>
        <span aria-disabled="true">Profile</span>
      </nav>
    </main>
  );
}
