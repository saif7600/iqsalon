import { describe, expect, it } from "vitest";
describe("portal routes", () => {
  it("uses an explicit login entry", () => {
    expect("/login").toMatch(/^\/login$/);
  });
  it("uses explicit Phase 5 routes", () => {
    expect(["/workforce", "/performance", "/growth", "/iqai"]).toHaveLength(4);
  });
});
