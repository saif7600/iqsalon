import Link from "next/link";

export default function StaffHome() {
  return (
    <main className="staff-shell">
      <header>
        <Link className="staff-brand" href="/">
          ATIQ<span>/STAFF</span>
        </Link>
        <span className="secure-state">Restricted</span>
      </header>

      <section className="shift-intro">
        <p>Mobile operations channel</p>
        <h1>Workday control, without customer overexposure.</h1>
      </section>

      <section className="boundary-panel">
        <p className="panel-index">01 / ACCESS</p>
        <h2>Assignment-scoped foundation active</h2>
        <p>
          The staff application is installable and has a conservative offline
          shell. Schedule, attendance, consumption, customer, and home-service
          actions remain closed until staff identity, branch assignment, context
          permissions, revocation, and idempotent synchronization are connected
          and tested.
        </p>
      </section>

      <nav aria-label="Staff application">
        <Link aria-current="page" href="/">
          <span>01</span>Today
        </Link>
        <span aria-disabled="true">
          <b>02</b>Schedule
        </span>
        <span aria-disabled="true">
          <b>03</b>Clock
        </span>
        <span aria-disabled="true">
          <b>04</b>Tasks
        </span>
      </nav>
    </main>
  );
}
