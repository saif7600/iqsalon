# Public booking

Public routes expose business display data and online-enabled services without tenant IDs, internal notes or user IDs. Submission architecture requires rate limiting, an idempotency key and a final server conflict check. Deposit-required bookings remain pending unless explicitly allowed without payment.
