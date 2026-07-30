# Availability engine

Calculation order is branch hours, closures, branch assignment, staff hours, breaks, overrides, appointment conflicts, resources, capability and booking rules. Local branch times are converted to UTC.

```mermaid
flowchart LR
  H[Branch hours] --> C[Closures]
  C --> A[Staff assignment and hours]
  A --> B[Breaks and overrides]
  B --> X[Appointment conflicts]
  X --> R[Resources]
  R --> S[Capability and rules]
  S --> O[Ordered slots]
```
