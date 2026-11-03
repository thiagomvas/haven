---
name: Haven
description: Self-hosted container orchestration dashboard, drawn as an Admiralty chart of your harbor.
colors:
  chart-blue: "#1457a6"
  chart-blue-hover: "#0f4686"
  chart-blue-mist: "#e3eefa"
  shell-navy: "#0c2b43"
  shell-navy-raised: "#143d5d"
  buoy-green: "#1b7f4b"
  buoy-red: "#c0352b"
  lighthouse-amber: "#9a5b00"
  amber-mark: "#c47a00"
  deploy-blue: "#1d74c9"
  chart-magenta: "#a3246b"
  chart-cyan: "#0a6f8a"
  shoal-ground: "#eef3f5"
  panel-white: "#ffffff"
  panel-tint: "#f5f8fa"
  contour: "#d0dce2"
  contour-strong: "#a9bcc6"
  chart-ink: "#0d2233"
  chart-ink-secondary: "#3f5566"
  chart-ink-muted: "#566b7a"
  night-ground: "#08161f"
  night-panel: "#0f2230"
  night-ink: "#e1ecf3"
typography:
  display:
    fontFamily: "Atkinson Hyperlegible Next, system-ui, sans-serif"
    fontSize: "1.625rem"
    fontWeight: 600
    lineHeight: 1.25
  headline:
    fontFamily: "Atkinson Hyperlegible Next, system-ui, sans-serif"
    fontSize: "1.375rem"
    fontWeight: 600
    lineHeight: 1.25
  title:
    fontFamily: "Atkinson Hyperlegible Next, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 600
    lineHeight: 1.25
  body:
    fontFamily: "Atkinson Hyperlegible Next, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Atkinson Hyperlegible Next, system-ui, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
    lineHeight: 1.2
  code:
    fontFamily: "Atkinson Hyperlegible Mono, ui-monospace, monospace"
    fontSize: "0.92em"
    fontWeight: 400
rounded:
  sm: "3px"
  md: "6px"
  lg: "8px"
  full: "9999px"
spacing:
  "1": "4px"
  "2": "8px"
  "3": "12px"
  "4": "16px"
  "6": "24px"
  "8": "32px"
components:
  button-primary:
    backgroundColor: "{colors.chart-blue}"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    height: "32px"
    padding: "0 12px"
  button-primary-hover:
    backgroundColor: "{colors.chart-blue-hover}"
  button-danger:
    backgroundColor: "{colors.buoy-red}"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    height: "32px"
  button-ghost:
    backgroundColor: "{colors.panel-white}"
    textColor: "{colors.chart-ink}"
    rounded: "{rounded.md}"
    height: "32px"
  input:
    backgroundColor: "{colors.panel-white}"
    textColor: "{colors.chart-ink}"
    rounded: "{rounded.md}"
    height: "32px"
    padding: "4px 12px"
  panel:
    backgroundColor: "{colors.panel-white}"
    rounded: "{rounded.lg}"
  badge:
    rounded: "{rounded.sm}"
    padding: "1px 8px"
---

# Design System: Haven

## Overview

**Creative North Star: "The Harbor Chart"**

Haven reads like a nautical chart of your own harbor: projects are harbors, environments are anchorages, services are marked buoys. The chart lends four things only: a palette (shoal-blue ground, chart-blue shell, buoy green / buoy red / lighthouse amber for state, chart magenta for pending work), a legible type voice with tabular "soundings" numerals, an instrument-grade density, and one signature move, the buoy strip. It never lends layout or controls: navigation is a standard left rail, data is standard tables, controls are standard buttons and inputs.

The system is built to be useful before it is pretty: show data, show state at a glance, make it convenient. Hierarchy comes from weight and position, not decoration. Surfaces are flat and hairline-bordered; state is always shape plus color.

**Key Characteristics:**
- Always-dark chart-navy navigation rail; light shoal-blue work ground (dark theme available)
- One signature: the **buoy strip**, one mark per service, worst state first
- State is shape + color: circle ok, diamond degraded, triangle fault, ring stopped, dashed ring deploying, square pending, dotted ring unknown
- Fixed 14px-based type scale, tabular numerals everywhere
- 32px control height shared by buttons, inputs, selects

## Colors

A committed chart palette: cool, ink-tinted neutrals, one action blue, and a small set of state colors that never decorate.

### Primary
- **Chart Blue** (#1457a6): primary actions, links, focus, selected nav. Hover #0f4686; tint #e3eefa. On the dark theme it lifts to #5da3ec.

### Secondary
- **Buoy Green** (#1b7f4b): running / success. **Buoy Red** (#c0352b): fault, danger, destructive. **Lighthouse Amber** (#9a5b00 text, #c47a00 mark): degraded / caution.

### Tertiary
- **Deploy Blue** (#1d74c9): deploying only. **Chart Magenta** (#a3246b): deployment pending only. **Chart Cyan** (#0a6f8a): informational.

### Neutral
- **Shoal Ground** (#eef3f5) page; **Panel White** (#ffffff) panels and inputs; **Panel Tint** (#f5f8fa) table headers and readouts; **Contour** (#d0dce2) and **Contour Strong** (#a9bcc6) borders.
- **Chart Ink** (#0d2233), **Secondary** (#3f5566), **Muted** (#566b7a): text tiers.
- **Shell Navy** (#0c2b43) rail with raised #143d5d; dark theme ground #08161f, panels #0f2230, text #e1ecf3.

### Named Rules
**The Shape-And-Color Rule.** Status is never color alone. Use `StatusMark`; never draw a bare colored dot.
**The Signal Scarcity Rule.** Red, amber, magenta and deploy-blue exist only to report state. Chart blue is the only action color.

## Typography

**Sans:** Atkinson Hyperlegible Next (400/500/600/700) with system fallbacks. **Mono:** Atkinson Hyperlegible Mono for identifiers, code and logs.

**Character:** A deliberately legible workhorse: distinct letterforms at small sizes, numerals set tabular so columns of figures align like chart soundings.

### Hierarchy
- **Display** (600, 1.625rem, 1.25): page title (h1).
- **Headline** (600, 1.375rem): major sections.
- **Title** (600, 0.875rem): panel titles.
- **Body** (400, 0.875rem, 1.5): all UI text, tables, events.
- **Label** (600, 0.75rem): table headers, readout labels, badges.
- **Code** (400, 0.92em, mono): ids, paths, commands.

### Named Rules
**The Tabular Rule.** `font-variant-numeric: tabular-nums` is set on `body`; numbers in columns must align.

## Layout

Fixed left rail (232px, collapses to 64px) plus a 52px top bar; content on a 4px grid with 16px between regions. The dashboard reads top to bottom as a triage: attention strip, fleet readout, then the Environments table (2/3) beside Recent Events (1/3). Worst projects and environments sort first so a fault is never below the fold. Under 1100px the two columns stack; under 768px the rail is hidden and tables scroll horizontally.

## Elevation & Depth

Flat. Depth is tonal (ground, panel, tint) plus 1px contour borders. Shadows (`0 2px 6px rgb(13 34 51 / 0.1)`) appear only on overlays, dropdowns and the active segmented-control pill.

### Named Rules
**The Flat-At-Rest Rule.** Panels carry no shadow and no hover lift.

## Shapes

Small, precise radii: 3px badges and chips, 6px controls, 8px panels. 1px hairlines everywhere; no accent side-borders. Fully round only for avatars.

## Components

### Panel
The standard titled container: one 44px header row (title, meta, actions), one body, optional `flush` body for tables and lists. Prefer it to ad-hoc cards.

### Buttons
- 32px tall (xs 24, sm 28, lg 40, xl 44), 6px radius, weight 500.
- **Primary:** chart blue fill. **Ghost:** white with strong contour border. **Danger:** buoy red. Also secondary, outline, success, warning, text.
- Focus: 2px surface gap then 2px chart-blue ring.

### Inputs
32px tall, white fill, strong contour border; focus shifts to chart blue with a 3px mist ring; error uses buoy red.

### Table
40px rows, hairline row dividers, label-style header on panel tint, hover row tint. Project groups separate with a strong contour rule.

### Banner
Tinted ground and a 1px state-colored border; the icon carries the hue; clickable banners tint on hover.

### StatGrid (readout)
Cells divided by 1px hairlines (grid gap), label above a 1.125rem semibold figure; auto-fits by width.

### Segmented control (FilterPillGroup)
Inset track with a raised active segment.

### Navigation rail
Always chart-navy. Active item: raised navy fill plus a 3px white marker flush to the rail edge.

### Configuration layout (ConfigurationPageLayout)
The shared shell for Settings, Account, Project, Environment and Service configuration. A 232px section rail (200px under 1100px) sits beside the content with a 16px gap; the content area keeps 24px of right padding so nothing meets the page or scrollbar edge. Under 768px the rail stacks above the content as a wrapping row and the padding drops to 0.
- **Rail:** a flat hairline panel (surface, 1px contour, 8px radius). Items are 32px, 6px radius, secondary ink; hover uses the surface-hover tint; the active item is a primary-subtle fill with primary text at weight 600 and `aria-current="page"`. No side stripe. Groups are divided by a hairline and carry a small uppercase muted label.
- **Header actions:** Configure and Close are the shared `Button` (primary and ghost), never bespoke buttons.

### ConfigSection, ConfigAction, ConfigContent
The building blocks of every configuration tab (`components/layout/ConfigSection.tsx`). Build tabs from these, not ad-hoc markup.
- **ConfigContent:** vertical stack, 16px between sections, max 840px wide.
- **ConfigSection:** flat hairline panel: header (title at body size 600, description in secondary ink at 65ch max, optional `actions` aligned to the end), body (16px padding, 16px gap), optional `footer` on a panel-tint strip with a top hairline that holds the commit action (Save, right-aligned, as a real submit button). `flush` removes body padding for lists and tables.
- **tone="danger":** danger-subtle header, danger title, border mixed from danger and contour. Used by `DangerZone`; destructive actions live only here.
- **ConfigAction:** a row inside a section: title (500) and description on the left, the control on the right, hairline between consecutive rows; stacks under 600px.
- **Form fields** (`TextInput`, `TextArea`, `Select` from `DetailsPageForm`): 32px controls, sentence-case 13px labels, strong contour border, mist focus ring, muted helper text and character count. `SettingsFormContainer` renders a `ConfigSection` and takes `description` and `footer`.
- A single-action section (Clone, Export) puts its button in the header `actions` slot and has no body.

### StatusMark and BuoyStrip (signature)
`StatusMark` draws the seven state shapes as 12px SVG. `BuoyStrip` renders one mark per service from aggregate counts, ordered degraded, stopped, deploying, pending, unknown, running, capped at 20 with a "+n" overflow and an accessible summary label.

## Do's and Don'ts

### Do:
- **Do** consume semantic tokens from `index.css`; never hard-code hex in a component stylesheet.
- **Do** define a dark value for every new color token.
- **Do** use `StatusMark` / `BuoyStrip` / `HealthIndicator` for any service or environment state.
- **Do** build regions with `Panel`, tables with `Table`, readouts with `StatGrid`.
- **Do** keep control height at 32px so buttons, inputs and selects align in a row.
- **Do** compose configuration tabs from `ConfigContent` / `ConfigSection` / `ConfigAction` and put Save in the section footer.

### Don't:
- **Don't** use color alone to convey state.
- **Don't** add shadows at rest, nested cards, or accent side-borders.
- **Don't** use chart magenta or deploy blue as brand accents.
- **Don't** hand-roll danger frames, action rows, or section headers in a settings form; use the shared config components.
- **Don't** invent radii outside 3 / 6 / 8 / full, or text sizes outside the scale.
- **Don't** fake live data: resource gauges ship only when the backend provides them.
