# AtiqSalon AI Design System

## Product character

AtiqSalon AI uses quiet confidence, operational clarity, and restrained
intelligence. It serves salons, barbers, spas, grooming, wellness, home-service,
independent, multi-branch, and franchise businesses without gendered defaults.

## Token architecture

Tokens live as CSS custom properties in the shared UI layer and application
roots. Components consume semantic tokens rather than fixed palette values.

### Color

| Token               | Light     | Dark      | Use                      |
| ------------------- | --------- | --------- | ------------------------ |
| `--color-canvas`    | `#f7f8fc` | `#080c18` | Application background   |
| `--color-surface`   | `#ffffff` | `#0f1525` | Primary panels           |
| `--color-elevated`  | `#fcfcfe` | `#151c2e` | Menus and raised panels  |
| `--color-text`      | `#111827` | `#f7f8fc` | Primary text             |
| `--color-muted`     | `#667085` | `#99a2b3` | Secondary text           |
| `--color-border`    | `#e6e8ef` | `#252d3e` | Separation               |
| `--color-primary`   | `#7c3aed` | `#9b6cff` | Primary action and focus |
| `--color-secondary` | `#4f46e5` | `#6d7cff` | Secondary data series    |
| `--color-teal`      | `#0ea5a4` | `#22c7c9` | Operational accent       |
| `--color-success`   | `#16a34a` | `#34d399` | Confirmed and healthy    |
| `--color-warning`   | `#d97706` | `#f59e0b` | Attention required       |
| `--color-danger`    | `#dc2626` | `#f87171` | Failure and destructive  |

Status is never communicated by color alone. Every state includes text and,
where useful, an icon or border pattern.

### Typography

- Operational UI: Manrope or Geist-compatible sans serif.
- Numeric values: tabular numerals.
- Arabic companion: Noto Sans Arabic or a verified equivalent.
- Headings use medium or semibold weight, never decorative display styling.
- Body text is at least 14px desktop and 15px mobile.

### Spacing and shape

- Spacing unit: 8px.
- Compact exceptions: 4px for icon-label and metadata relationships.
- Control radius: 8px.
- Input and button radius: 10px.
- Card radius: 14px.
- Large panel radius: 18px.
- Shadows are subtle; borders and surface tones provide primary separation.

### Motion

- Fast state change: 120ms.
- Standard transition: 180ms.
- Drawer transition: 220ms.
- Motion uses opacity and small transforms only.
- `prefers-reduced-motion: reduce` disables nonessential movement.

## Shared components

The shared UI package owns:

- Application shell primitives
- Sidebar and navigation item
- Utility header
- Metric card
- Insight card
- Status badge
- Data table and filter bar
- Command search
- Loading, empty, error, and permission states
- Approval card
- Timeline and activity feed
- Profile header
- Mobile bottom navigation
- Theme and language switchers

Business modules own domain composition, not duplicate visual primitives.

## Data visualization

- Maximum five primary metric cards per row.
- Charts require metric, unit, period, comparison, filter state, and source.
- No decorative sparklines without a real series.
- Chart palette is violet, indigo, teal, emerald, amber, and rose with adequate
  luminance separation in both themes.
- Detailed tables remain available where charts summarize operational data.

## Interaction

- Touch target minimum: 44 by 44px.
- Focus rings remain visible.
- Internal navigation uses client-side routing.
- Loading preserves layout geometry.
- Destructive operations require explicit confirmation and permission.
- AI recommendations show evidence, period, confidence, and approval boundary.
