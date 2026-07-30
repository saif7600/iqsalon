const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? "/api/v1";
export async function apiRequest<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    credentials: "include",
    headers: { "Content-Type": "application/json", ...init?.headers },
  });
  if (!response.ok) throw await response.json();
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
