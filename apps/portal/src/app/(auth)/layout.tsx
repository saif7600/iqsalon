export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <main className="auth-shell">
      <section className="auth-story">
        <p className="wordmark">AtiqSalon AI</p>
        <h1>Space for your team to do exceptional work.</h1>
        <p>Operating System for Beauty & Wellness Businesses</p>
      </section>
      <section className="auth-panel">{children}</section>
    </main>
  );
}
