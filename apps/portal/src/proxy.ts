import { NextRequest, NextResponse } from "next/server";
const protectedPaths = [
  "/dashboard",
  "/settings",
  "/platform",
  "/audit",
  "/onboarding",
  "/calendar",
  "/appointments",
  "/customers",
  "/services",
  "/staff",
  "/resources",
  "/pos",
  "/reports",
  "/commercial",
  "/inventory",
  "/workforce",
  "/performance",
  "/growth",
  "/iqai",
];
export function proxy(request: NextRequest) {
  const requiresAuth = protectedPaths.some((path) =>
    request.nextUrl.pathname.startsWith(path),
  );
  const hasSession = Boolean(request.cookies.get("atiqsalon_session"));
  if (requiresAuth && !hasSession)
    return NextResponse.redirect(
      new URL(
        `/login?returnUrl=${encodeURIComponent(request.nextUrl.pathname)}`,
        request.url,
      ),
    );
  return NextResponse.next();
}
export const config = {
  matcher: [
    "/dashboard/:path*",
    "/settings/:path*",
    "/audit/:path*",
    "/onboarding/:path*",
    "/calendar/:path*",
    "/appointments/:path*",
    "/customers/:path*",
    "/services/:path*",
    "/staff/:path*",
    "/resources/:path*",
    "/pos/:path*",
    "/reports/:path*",
    "/commercial/:path*",
    "/inventory/:path*",
    "/workforce/:path*",
    "/performance/:path*",
    "/growth/:path*",
    "/iqai/:path*",
  ],
};
