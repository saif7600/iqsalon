import { Card, PageTitle } from "@atiqsalon/ui";
export default function Onboarding() {
  const steps = [
    "Account",
    "Verify email",
    "Organization",
    "Country",
    "Language",
    "Currency",
    "Timezone",
    "First branch",
    "Invite team",
    "Finish",
  ];
  return (
    <main className="portal-content onboarding">
      <PageTitle eyebrow="Welcome" title="Set up your business">
        Create the secure organization and first branch that will scope your
        workspace.
      </PageTitle>
      <div className="steps">
        {steps.map((x, i) => (
          <span className="step" key={x}>
            {i + 1}. {x}
          </span>
        ))}
      </div>
      <Card>
        <h2>Initial regional defaults</h2>
        <dl>
          <dt>Country</dt>
          <dd>United Arab Emirates</dd>
          <dt>Currency</dt>
          <dd>AED</dd>
          <dt>Timezone</dt>
          <dd>Asia/Dubai</dd>
          <dt>Languages</dt>
          <dd>English / العربية</dd>
        </dl>
        <p className="notice">
          Submission requires the running API and a verified account. This
          foundation does not silently create local business data.
        </p>
      </Card>
    </main>
  );
}
