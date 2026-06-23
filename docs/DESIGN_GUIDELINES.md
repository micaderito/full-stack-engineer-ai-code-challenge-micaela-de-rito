# Stratus Fab Tracker — Design Guidelines

Theme: **Modern Teal**. Calm teal brand with low-saturation neutrals so data and
status read clearly. Supports light and dark mode.

All tokens live in [`theme.css`](../src/stratus-fab-tracker-web/src/styles/theme.css)
as CSS custom properties. **Always consume the semantic `--color-*` tokens in
components — never the raw ramps or hardcoded hex.** That keeps light/dark mode
and any future re-theme to a single file.

---

## 1. Theming

Dark mode resolves in this order:

1. `<html data-theme="dark">` or `data-theme="light"` — explicit, always wins.
2. Otherwise the OS preference (`prefers-color-scheme`).

A theme toggle just sets/removes `data-theme` on `<html>`.

---

## 2. Color palette

### Brand

| Token | Light | Dark |
|---|---|---|
| `--color-brand` | `#0F6E56` | `#2FA483` |
| `--color-brand-hover` | `#0B5644` | `#3BB592` |
| `--color-brand-active` | `#094538` | `#259079` |
| `--color-brand-subtle` (tint) | `#E1F5EE` | `#0E3A2D` |
| `--color-text-on-brand` | `#FFFFFF` | `#04140F` |

### Surfaces & text

| Token | Light | Dark |
|---|---|---|
| `--color-bg-page` | `#F2F5F4` | `#0C1512` |
| `--color-bg-surface` | `#FFFFFF` | `#13201B` |
| `--color-bg-surface-alt` | `#F2F5F4` | `#1A2B24` |
| `--color-bg-inset` | `#E4EAE7` | `#1A2B24` |
| `--color-text-primary` | `#13201B` | `#E2EDE8` |
| `--color-text-secondary` | `#566660` | `#8AA89D` |
| `--color-text-tertiary` | `#7C8C86` | `#647A70` |
| `--color-border` | `#D3DEDA` | `#27392F` |
| `--color-border-strong` | `#B9C8C2` | `#354B40` |

### Status

Each status has a text/foreground token and a tint background for banners and pills.

| Meaning | `--color-*` (light → dark) | `--color-*-bg` (light → dark) |
|---|---|---|
| Success | `#3B6D11` → `#97C459` | `#EAF3DE` → `#1B2A0E` |
| Warning | `#854F0B` → `#EF9F27` | `#FAEEDA` → `#2E2008` |
| Danger | `#A32D2D` → `#F09595` | `#FCEBEB` → `#2E1414` |
| Info | `#185FA5` → `#85B7EB` | `#E6F1FB` → `#0E2A47` |

`--color-danger-solid` (`#C0392B` / `#D9534F`) is the filled destructive-button color.

---

## 3. Station colors

The six stations are a **categorical** set — each a distinct hue so they're
instantly distinguishable in badges, kanban columns, and charts. Past-due is a
seventh, status-style color that overrides a station's normal appearance when a
spool is overdue.

| Station | `--station-*` (solid) | Light fill / text | Dark fill / text |
|---|---|---|---|
| Detailing | Purple `#534AB7` | `#EEEDFE` / `#3C3489` | `#25224A` / `#AFA9EC` |
| Cut | Blue `#185FA5` | `#E6F1FB` / `#0C447C` | `#0E2A47` / `#85B7EB` |
| Weld | Coral `#D85A30` | `#FAECE7` / `#993C1D` | `#3A1F12` / `#F0997B` |
| QC | Pink `#D4537E` | `#FBEAF0` / `#993556` | `#3A1622` / `#ED93B1` |
| Shipped | Teal `#1D9E75` | `#E1F5EE` / `#0F6E56` | `#0E3A2D` / `#5DCAA5` |
| Installed | Green `#3B6D11` | `#EAF3DE` / `#27500A` | `#1B2A0E` / `#97C459` |
| **Past due** | Red `#C0392B` | `#FCEBEB` / `#A32D2D` | `#2E1414` / `#F09595` |

Each station exposes three tokens — `--station-{name}` (solid: dots, chart bars,
status dots), `--station-{name}-bg` (badge fill), `--station-{name}-fg` (badge
text). Use the `.station--{name}` classes for badges.

**Never rely on color alone.** Always pair a station color with its label (and,
for past-due, an icon). This covers color-blind users and the shop floor.

---

## 4. Components

### Buttons — `.btn`

36px tall, `--radius-md`, weight 500, label `--text-sm`. Active state scales to
`0.98`. Disabled drops to 0.5 opacity.

| Variant | Class | Use |
|---|---|---|
| Primary | `.btn--primary` | The main action (e.g. "Advance station"). One per view. |
| Secondary | `.btn--secondary` | Transparent + border. Secondary actions ("Cancel"). |
| Ghost | `.btn--ghost` | Low-emphasis / icon buttons in toolbars. |
| Danger | `.btn--danger` | Destructive, confirmed actions. |

```html
<button class="btn btn--primary">Advance station</button>
<button class="btn btn--secondary">Cancel</button>
```

### Inputs — `.input`, `.select`, `.textarea`

36px tall (textarea auto-grows), `--radius-md`, 1px `--color-border`. Hover
strengthens the border; focus switches to a brand border + 3px focus ring. Pair
with `.label`; show validation errors via `.input--error` + `.field-error`.

```html
<label class="label" for="spool">Spool number</label>
<input class="input" id="spool" placeholder="SP-1042" />
```

### Cards — `.card`

Surface background, 1px border, `--radius-lg`, padding `16px 20px`. The default
container for grouped content.

### Metric cards — `.metric`

For the dashboard summary numbers (WIP per station, past-due count): alt-surface
background, muted 13px label, 22px/500 value. Use in a responsive grid.

### Badges — `.badge` + `.station--*`

Pill, 12px/500, tint fill + same-hue text, leading status dot.

```html
<span class="badge station--weld"><span class="badge__dot"></span>Weld</span>
<span class="badge station--pastdue"><span class="badge__dot"></span>Past due</span>
```

---

## 5. Typography & spacing

- **Font:** system sans stack (`--font-sans`); `--font-mono` for spool/part IDs.
- **Sizes:** `--text-xs` 12 · `--text-sm` 13 · `--text-base` 14 · `--text-lg` 16
  · `--text-xl` 18 · `--text-2xl` 22 · `--text-3xl` 28.
- **Weights:** only 400 and 500. Avoid 600/700.
- **Spacing scale:** `--space-1`…`--space-10` (4 → 40px). Use the scale, not
  arbitrary px.
- **Radius:** `--radius-sm` 6 · `-md` 8 (controls) · `-lg` 12 (cards) · `-xl` 16
  · `-pill` (badges).

---

## 6. Accessibility

- **Contrast:** body text/background pairs target WCAG **AA (≥4.5:1)**; large
  text, borders, and disabled controls target **≥3:1**. Button labels clear AA on
  their fills in both modes.
- **Status never color-only:** always accompany color with text/icon/shape.
- **Focus:** every interactive element shows a visible 3px `--color-focus-ring`
  via `:focus-visible`. Don't remove outlines without a replacement.
- **Dark mode is re-tuned, not inverted:** fills and text are chosen per mode (see
  the station and status tables).
- **Hit targets:** interactive controls are ≥36px in their smallest dimension.
