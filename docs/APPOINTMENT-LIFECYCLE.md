# Appointment lifecycle

```mermaid
stateDiagram-v2
  Draft --> PendingConfirmation
  Draft --> Confirmed
  Draft --> Cancelled
  PendingConfirmation --> Confirmed
  PendingConfirmation --> Cancelled
  Confirmed --> CheckedIn
  Confirmed --> Cancelled
  Confirmed --> NoShow
  CheckedIn --> InProgress
  CheckedIn --> Cancelled
  InProgress --> Completed
```

Terminal statuses cannot be silently reopened.
