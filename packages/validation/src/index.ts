import { z } from "zod";
export const loginSchema = z.object({
  email: z.email(),
  password: z.string().min(12),
});
export const registerSchema = loginSchema.extend({
  displayName: z.string().trim().min(2).max(120),
});
export const organizationSchema = z.object({
  legalName: z.string().trim().min(2).max(200),
  tradingName: z.string().trim().min(2).max(200),
  countryCode: z.string().length(2),
  currency: z.string().length(3),
  language: z.enum(["en", "ar"]),
  timeZone: z.string().min(3),
});
