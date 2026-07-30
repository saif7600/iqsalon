"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

type Service = {
  id: string;
  name: string;
  durationMinutes: number;
  basePrice: number;
  currencyCode: string;
};
type Staff = { id: string; displayName: string };
type Slot = {
  startTime: string;
  endTime: string;
  eligibleStaff: string[];
};

const api = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api/v1";

export function BookingClient({
  organizationSlug,
  branchCode,
}: {
  organizationSlug: string;
  branchCode: string;
}) {
  const [language, setLanguage] = useState<"en" | "ar">("en");
  const [business, setBusiness] = useState("AtiqSalon");
  const [services, setServices] = useState<Service[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [slots, setSlots] = useState<Slot[]>([]);
  const [serviceId, setServiceId] = useState("");
  const [staffId, setStaffId] = useState("");
  const [slot, setSlot] = useState<Slot | null>(null);
  const [state, setState] = useState<
    "loading" | "ready" | "sending" | "success" | "error"
  >("loading");
  const [message, setMessage] = useState("");
  const copy =
    language === "ar"
      ? {
          eyebrow: "حجز آمن عبر الإنترنت",
          title: "احجز موعدك",
          service: "اختر الخدمة",
          person: "اختر المختص",
          time: "اختر الوقت",
          details: "بياناتك",
          first: "الاسم الأول",
          last: "اسم العائلة",
          email: "البريد الإلكتروني",
          phone: "رقم الهاتف",
          submit: "تأكيد الحجز",
          success: "تم استلام حجزك",
          retry: "تعذر إكمال الحجز. اختر وقتاً آخر.",
        }
      : {
          eyebrow: "Secure online booking",
          title: "Book your appointment",
          service: "Choose a service",
          person: "Choose a professional",
          time: "Choose a time",
          details: "Your details",
          first: "First name",
          last: "Last name",
          email: "Email",
          phone: "Phone number",
          submit: "Confirm booking",
          success: "Your booking has been received",
          retry: "We could not complete that booking. Choose another time.",
        };

  useEffect(() => {
    Promise.all([
      fetch(`${api}/public/booking/${organizationSlug}`).then((response) => {
        if (!response.ok) throw new Error("Business unavailable");
        return response.json();
      }),
      fetch(
        `${api}/public/booking/${organizationSlug}/${branchCode}/services`,
      ).then((response) => {
        if (!response.ok) throw new Error("Services unavailable");
        return response.json();
      }),
    ])
      .then(([organization, availableServices]) => {
        setBusiness(organization.tradingName);
        setLanguage(organization.defaultLanguage === "ar" ? "ar" : "en");
        setServices(availableServices);
        setState("ready");
      })
      .catch(() => {
        setMessage("Online booking is currently unavailable.");
        setState("error");
      });
  }, [organizationSlug, branchCode]);

  useEffect(() => {
    if (!serviceId) return;
    fetch(
      `${api}/public/booking/${organizationSlug}/${branchCode}/staff?serviceId=${serviceId}`,
    )
      .then((response) => (response.ok ? response.json() : Promise.reject()))
      .then(setStaff)
      .catch(() => setMessage(copy.retry));
  }, [serviceId, organizationSlug, branchCode, copy.retry]);

  useEffect(() => {
    if (!serviceId || !staffId) return;
    const from = new Date();
    const to = new Date();
    to.setDate(to.getDate() + 14);
    const date = (value: Date) => value.toISOString().slice(0, 10);
    fetch(
      `${api}/public/booking/${organizationSlug}/${branchCode}/availability?serviceId=${serviceId}&preferredStaffMemberId=${staffId}&dateFrom=${date(from)}&dateTo=${date(to)}`,
    )
      .then((response) => (response.ok ? response.json() : Promise.reject()))
      .then(setSlots)
      .catch(() => setMessage(copy.retry));
  }, [serviceId, staffId, organizationSlug, branchCode, copy.retry]);

  const chosenService = useMemo(
    () => services.find((service) => service.id === serviceId),
    [services, serviceId],
  );

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!slot || !chosenService) return;
    setState("sending");
    setMessage("");
    const data = new FormData(event.currentTarget);
    const response = await fetch(
      `${api}/public/booking/${organizationSlug}/${branchCode}/appointments`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          serviceId,
          staffMemberId: staffId,
          startAtUtc: slot.startTime,
          endAtUtc: slot.endTime,
          firstName: data.get("firstName"),
          lastName: data.get("lastName"),
          email: data.get("email"),
          phoneCountryCode: "+971",
          phoneNumber: data.get("phone"),
          language,
          idempotencyKey: crypto.randomUUID(),
        }),
      },
    );
    if (response.ok) {
      const result = await response.json();
      setMessage(`${copy.success}. ${result.number}`);
      setState("success");
    } else {
      setMessage(copy.retry);
      setState("ready");
    }
  }

  return (
    <main className="booking-page" dir={language === "ar" ? "rtl" : "ltr"}>
      <header className="booking-brand">
        <span className="booking-mark">A</span>
        <div>
          <strong>{business}</strong>
          <small>{copy.eyebrow}</small>
        </div>
        <button
          className="language-switch"
          onClick={() => setLanguage(language === "en" ? "ar" : "en")}
        >
          {language === "en" ? "العربية" : "English"}
        </button>
      </header>
      <section className="booking-stage">
        <div className="booking-intro">
          <p>{copy.eyebrow}</p>
          <h1>{copy.title}</h1>
          <span>{branchCode.replaceAll("-", " ")}</span>
        </div>
        <form className="booking-form" onSubmit={submit}>
          {state === "loading" && (
            <p className="booking-message">Loading booking options...</p>
          )}
          {state === "error" && (
            <p className="booking-message error">{message}</p>
          )}
          {state === "success" ? (
            <div className="booking-success">
              <span>01</span>
              <h2>{message}</h2>
            </div>
          ) : (
            state !== "error" && (
              <>
                <label>
                  <span>01 / {copy.service}</span>
                  <select
                    required
                    value={serviceId}
                    onChange={(event) => {
                      setServiceId(event.target.value);
                      setStaffId("");
                      setSlot(null);
                      setSlots([]);
                    }}
                  >
                    <option value="">{copy.service}</option>
                    {services.map((service) => (
                      <option key={service.id} value={service.id}>
                        {service.name} · {service.basePrice}{" "}
                        {service.currencyCode}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  <span>02 / {copy.person}</span>
                  <select
                    required
                    value={staffId}
                    disabled={!serviceId}
                    onChange={(event) => setStaffId(event.target.value)}
                  >
                    <option value="">{copy.person}</option>
                    {staff.map((person) => (
                      <option key={person.id} value={person.id}>
                        {person.displayName}
                      </option>
                    ))}
                  </select>
                </label>
                <fieldset disabled={!staffId}>
                  <legend>03 / {copy.time}</legend>
                  <div className="slot-grid">
                    {slots.slice(0, 30).map((item) => (
                      <button
                        type="button"
                        className={
                          slot?.startTime === item.startTime ? "selected" : ""
                        }
                        key={item.startTime}
                        onClick={() => setSlot(item)}
                      >
                        {new Intl.DateTimeFormat(language, {
                          weekday: "short",
                          day: "numeric",
                          month: "short",
                          hour: "numeric",
                          minute: "2-digit",
                        }).format(new Date(item.startTime))}
                      </button>
                    ))}
                  </div>
                </fieldset>
                <fieldset disabled={!slot}>
                  <legend>04 / {copy.details}</legend>
                  <div className="details-grid">
                    <input
                      name="firstName"
                      required
                      placeholder={copy.first}
                      autoComplete="given-name"
                    />
                    <input
                      name="lastName"
                      required
                      placeholder={copy.last}
                      autoComplete="family-name"
                    />
                    <input
                      name="email"
                      type="email"
                      placeholder={copy.email}
                      autoComplete="email"
                    />
                    <input
                      name="phone"
                      required
                      placeholder={copy.phone}
                      autoComplete="tel"
                    />
                  </div>
                </fieldset>
                {message && <p className="booking-message error">{message}</p>}
                <button
                  className="booking-submit"
                  disabled={!slot || state === "sending"}
                >
                  {state === "sending" ? "..." : copy.submit}
                </button>
              </>
            )
          )}
        </form>
      </section>
    </main>
  );
}
