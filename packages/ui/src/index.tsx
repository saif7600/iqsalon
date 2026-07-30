import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
} from "react";

export function Button({
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement>) {
  return <button className={`button ${className}`} {...props} />;
}
export function Input(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input className="input" {...props} />;
}
export function Card({
  children,
  className = "",
}: {
  children: ReactNode;
  className?: string;
}) {
  return <section className={`card ${className}`}>{children}</section>;
}
export function Badge({ children }: { children: ReactNode }) {
  return <span className="badge">{children}</span>;
}
export function PageTitle({
  eyebrow,
  title,
  children,
}: {
  eyebrow?: string;
  title: string;
  children?: ReactNode;
}) {
  return (
    <header className="page-title">
      {eyebrow ? <p className="eyebrow">{eyebrow}</p> : null}
      <h1>{title}</h1>
      {children ? <p>{children}</p> : null}
    </header>
  );
}
export function EmptyState({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <Card>
      <h2>{title}</h2>
      <p>{description}</p>
    </Card>
  );
}
export function LoadingState() {
  return (
    <div aria-live="polite" aria-busy="true" className="skeleton">
      Loading
    </div>
  );
}
export function ErrorState({ message }: { message: string }) {
  return (
    <div role="alert" className="error-state">
      {message}
    </div>
  );
}
export function StatCard({
  label,
  value,
  note,
}: {
  label: string;
  value: string;
  note: string;
}) {
  return (
    <Card>
      <p className="muted">{label}</p>
      <strong className="stat">{value}</strong>
      <p>{note}</p>
    </Card>
  );
}
export function Breadcrumb({ items }: { items: string[] }) {
  return (
    <nav aria-label="Breadcrumb">
      <ol className="breadcrumbs">
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ol>
    </nav>
  );
}
export function FormField({
  label,
  htmlFor,
  error,
  hint,
  required = false,
  children,
}: {
  label: string;
  htmlFor: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: ReactNode;
}) {
  return (
    <div className={`field${error ? " field-error" : ""}`}>
      <label htmlFor={htmlFor}>
        {label}
        {required ? <span aria-hidden="true"> *</span> : null}
      </label>
      {children}
      {hint && !error ? <p className="field-hint">{hint}</p> : null}
      {error ? <p className="field-message" role="alert">{error}</p> : null}
    </div>
  );
}

export function FormSection({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <fieldset className="form-section">
      <legend>{title}</legend>
      {description ? <p className="form-section-copy">{description}</p> : null}
      <div className="form-grid">{children}</div>
    </fieldset>
  );
}

export function FormActions({
  children,
  note,
}: {
  children: ReactNode;
  note?: string;
}) {
  return (
    <footer className="form-actions">
      {note ? <p>{note}</p> : <span />}
      <div>{children}</div>
    </footer>
  );
}
