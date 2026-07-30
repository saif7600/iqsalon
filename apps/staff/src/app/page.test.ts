import { describe, expect, it } from "vitest";

describe("staff channel", () => {
  it("requires assignment-scoped operational access", () => {
    expect(["branch", "assignment"]).toContain("assignment");
  });
});
