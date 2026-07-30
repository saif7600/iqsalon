# Booking engine

Appointment is the booking and operational aggregate. Creation uses a serializable PostgreSQL transaction, final conflict check, immutable pricing and duration snapshots, and idempotency architecture.

```mermaid
sequenceDiagram
  participant C as Client
  participant A as API
  participant D as PostgreSQL
  C->>A: Request availability
  A->>D: Tenant-scoped schedule query
  D-->>A: Ordered slots
  C->>A: Create appointment
  A->>D: Serializable final conflict check
  D-->>A: Appointment or conflict
```

```mermaid
erDiagram
  APPOINTMENT ||--|{ APPOINTMENT_SERVICE : contains
  APPOINTMENT ||--o{ APPOINTMENT_STATUS_HISTORY : records
  APPOINTMENT_SERVICE ||--o{ RESOURCE_RESERVATION : reserves
  CUSTOMER ||--o{ APPOINTMENT : books
  STAFF_MEMBER ||--o{ APPOINTMENT_SERVICE : performs
```
