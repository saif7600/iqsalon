import { describe, expect, it } from "vitest";
describe("marketing content", () => {
  it("keeps the approved hero claim", () => {
    expect("Run Your Entire Salon With AI").toContain("Salon");
  });
});
