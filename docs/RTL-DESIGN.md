# RTL and Arabic Design

## Direction

Arabic sets `lang="ar"` and `dir="rtl"` on the document root. Layout uses
logical CSS properties such as `margin-inline`, `padding-inline`,
`border-inline`, `inset-inline`, and `text-align: start`.

## Navigation and structure

- Sidebar moves to the right.
- Directional navigation icons mirror.
- Non-directional operational icons do not mirror.
- Breadcrumb order, drawer entry, and back affordances follow RTL expectations.
- Calendar time progression and column order are explicitly tested.

## Typography and data

- Arabic uses a professional Arabic companion font.
- Mixed Arabic/Latin identifiers use isolated bidirectional spans.
- Currency and numbers use locale-aware `Intl` formatting.
- Phone numbers, invoice codes, SKUs, and email addresses preserve LTR
  readability inside RTL layouts.
- Tables align descriptive text to start and numeric values consistently.

## Translation quality

- No concatenated sentence fragments.
- No clipped navigation labels.
- Operational terminology is reviewed in context.
- Security, consent, billing, and error messages remain platform-controlled.

## Verification

Arabic is verified in dashboard, calendar, POS, customer and staff mobile,
profile, invoice, reports, theme switching, dialogs, tables, and empty/error
states.
