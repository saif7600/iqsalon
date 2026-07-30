import { expect, test } from "@playwright/test";

test("login renders the production portal shell", async ({ page }) => {
  await page.goto("/login");
  await expect(page).toHaveTitle(/AtiqSalon AI Portal/);
  await expect(
    page.getByRole("heading", { name: "Welcome back" }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
});

test("protected operations redirect anonymous users to login", async ({
  page,
}) => {
  await page.goto("/appointments");
  await expect(page).toHaveURL(/\/login\?returnUrl=%2Fappointments/);
});

for (const route of [
  "/inventory",
  "/workforce",
  "/performance",
  "/growth",
  "/iqai",
]) {
  test(`${route} redirects anonymous users to login`, async ({ page }) => {
    await page.goto(route);
    await expect(page).toHaveURL(
      new RegExp(`/login\\?returnUrl=${encodeURIComponent(route)}`),
    );
  });
}

test("unavailable account recovery is not advertised", async ({ page }) => {
  await page.goto("/login");
  await expect(
    page.getByRole("link", { name: "Forgot password?" }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("link", { name: "Create account" }),
  ).toBeVisible();
});

test("security headers are present", async ({ request }) => {
  const response = await request.get("/login");
  expect(response.status()).toBe(200);
  expect(response.headers()["x-content-type-options"]).toBe("nosniff");
  expect(response.headers()["x-frame-options"]).toBe("DENY");
});
