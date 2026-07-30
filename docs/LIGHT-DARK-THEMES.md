# Light and Dark Themes

## Theme resolution

Theme values are `light`, `dark`, and `system`.

1. An inline pre-hydration script reads the persisted preference.
2. `system` resolves through `prefers-color-scheme`.
3. The resolved theme is applied to the root `data-theme` attribute.
4. Changes apply immediately and persist locally.
5. System-mode changes remain reactive.

The server renders theme-independent structure to avoid hydration mismatch.

## Light mode

Light mode uses a warm neutral canvas, white surfaces, deep navy text, restrained
violet actions, subtle borders, and low-elevation shadows. It avoids clinical
white repetition and low-contrast gray text.

## Dark mode

Dark mode uses deep navy-black canvas and layered blue-charcoal surfaces. It is
not a color inversion. Borders, muted text, charts, focus rings, errors, and
success states have independently validated dark values.

## Component requirements

- Charts use theme-specific grid, label, tooltip, and series colors.
- Native controls declare `color-scheme`.
- Logos work on both backgrounds without glow.
- Images and thumbnails retain natural color.
- Skeletons do not flash bright white in dark mode.
- Overlays maintain readable contrast.

## Validation matrix

- Light English
- Dark English
- Light Arabic
- Dark Arabic
- System preference change while open
- Persisted preference after reload
- No wrong-theme flash
- High contrast for text, focus, errors, and charts
