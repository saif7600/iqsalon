# Accessibility Design

## Standard

AtiqSalon targets WCAG 2.2 AA for product interfaces.

## Keyboard and focus

- Every action is keyboard reachable.
- Focus order follows visual and document order.
- Focus indicators use a high-contrast 2px ring with offset.
- Drawers and dialogs trap focus, restore focus on close, and support Escape.
- Skip links are provided for repeated portal navigation.

## Semantics

- Landmarks identify navigation, header, main, and contextual regions.
- Controls use native elements where possible.
- Inputs have visible labels and associated descriptions/errors.
- Tables use captions, headers, scope, and accessible overflow.
- Charts include text summaries and tabular alternatives.
- Icon-only buttons have accessible names.

## Visual access

- Text and meaningful controls meet contrast expectations in both themes.
- Status never relies on color alone.
- Touch targets are at least 44px.
- Zoom to 200% does not remove functionality.
- Content reflows without page-level horizontal scrolling.

## Motion and feedback

- Reduced-motion preference disables nonessential transitions.
- Loading, success, failure, pending sync, and offline states are announced.
- Errors provide a summary and field association.
- AI streaming can be paused and final content remains readable.

## Product-specific requirements

- Calendar appointments expose customer, service, time, staff, and status.
- POS totals and payment state are announced after changes.
- Offline staff actions clearly state pending versus synchronized.
- Destructive tenant, financial, and security actions require explicit,
  accessible confirmation.

## Verification

Automated checks supplement, but do not replace:

- Keyboard-only workflow
- Screen-reader landmarks and form review
- Light and dark contrast review
- English and Arabic review
- Mobile touch and keyboard review
- Loading, empty, error, permission, offline, and reduced-motion states
