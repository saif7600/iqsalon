import { redirect } from "next/navigation";

/** Alias for /login — keep both URLs working across brands. */
export default async function SignInPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const params = await searchParams;
  const qs = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (typeof value === "string") qs.set(key, value);
    else if (Array.isArray(value)) for (const item of value) qs.append(key, item);
  }
  const suffix = qs.toString();
  redirect(suffix ? `/login?${suffix}` : "/login");
}
