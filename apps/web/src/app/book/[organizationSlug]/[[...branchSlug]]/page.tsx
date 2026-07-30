"use client";
import { useState } from "react";
import { Button, Card, FormField, Input, PageTitle } from "@atiqsalon/ui";
export default function PublicBooking() {
  const [step, setStep] = useState(1);
  const [message, setMessage] = useState("");
  const labels = ["Branch", "Services", "Staff", "Time", "Details", "Review"];
  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    setMessage(
      "Connect the API and choose a real available slot before submission.",
    );
  };
  return (
    <main className="route-page">
      <div className="container content">
        <PageTitle eyebrow="Online booking" title="Book your visit">
          Choose a real service, eligible staff member and available time. No
          payment details are collected.
        </PageTitle>
        <div className="steps" aria-label="Booking progress">
          {labels.map((label, index) => (
            <button
              className="step"
              aria-current={step === index + 1 ? "step" : undefined}
              key={label}
              onClick={() => setStep(index + 1)}
            >
              {index + 1}. {label}
            </button>
          ))}
        </div>
        <Card>
          <form className="booking-form" onSubmit={submit}>
            <h2>
              {step}.{" "}
              {
                [
                  "Select branch",
                  "Select services",
                  "Choose staff",
                  "Choose an available time",
                  "Your details",
                  "Review booking",
                ][step - 1]
              }
            </h2>
            {step === 5 ? (
              <>
                <FormField label="Name" htmlFor="booking-name">
                  <Input id="booking-name" required />
                </FormField>
                <FormField label="Phone" htmlFor="booking-phone">
                  <Input id="booking-phone" type="tel" required />
                </FormField>
                <label>
                  <input type="checkbox" required /> I accept the booking and
                  privacy policy.
                </label>
                <label>
                  <input type="checkbox" /> I separately consent to marketing
                  messages.
                </label>
              </>
            ) : (
              <p>
                Options load from the tenant-scoped public API. Unavailable and
                online-disabled records are never displayed.
              </p>
            )}
            <Button type="submit">Continue</Button>
            {message ? (
              <p role="status" className="notice">
                {message}
              </p>
            ) : null}
          </form>
        </Card>
      </div>
    </main>
  );
}
