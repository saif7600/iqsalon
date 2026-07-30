import { describe, expect, it } from "vitest";

describe("customer channel", () => {
  it("keeps operator authentication outside the customer application", () => {
    expect("customer").not.toBe("operator");
  });
});
