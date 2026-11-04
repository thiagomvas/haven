---
version: 1
slug: "ion-haven-web-src-pages-dashboardpage-tsx-231e7857"
primary_target: "src/Presentation/Haven.Web/src/pages/DashboardPage.tsx"
related_targets: []
---

# Surface brief: Dashboard (Operate)

Scope: Operate. Main dashboard page plus the core design tokens and shared ui/layout components, which become Haven's v1 design system (replaces the teal "vibe coded" look; redesign).

Audience/job: solo devs, homelab, small teams. First question on opening: "is anything broken?" Then what changed, then where everything is. Live resource data (CPU/mem/uptime) endpoints will be added later by the user; design for it, do not fake it as real.

Constraints: i18n strings via react-i18next; keep component APIs; no new backend; useful and dense over pretty.

## Direction contract

THESIS: Harbor Chart. The dashboard is a nautical chart of the user's harbor: projects are harbors, environments are anchorages, services are marked buoys. Refuses the neutral-grey-plus-teal SaaS default and the card-per-metric hero template.
OWN-WORLD: Admiralty chart palette: pale shoal-blue work ground, deep chart-blue shell rail, chart-blue primary action, buoy green (ok), buoy red (fault), lighthouse amber (degraded), chart magenta (pending/annotation). Atkinson Hyperlegible Next with tabular "soundings" numerals and Atkinson Hyperlegible Mono for ids. Hairline contour borders, 4px base grid, small radii (3/6px).
STORY: Operator sees in one glance whether anything is faulted, which anchorage and since when, then scans every environment's buoys and acts.
FIRST VIEWPORT: Left chart-blue rail nav; top: one attention strip (fault/degraded count, worst anchorage, since-time, link) spanning the work area; below, left ~2/3 the Anchorages table (project, environment, buoy strip per service, up/total, last deploy), right ~1/3 recent events log with duration bars and a compact fleet tally; primary action "Create service" in the page header.
FORM: Harbor Chart, my pick (not the assigned form), seed:12ea6dbe3e2d15d1. Signature move: the buoy strip, service state drawn as shape+color marks (circle ok, triangle fault, diamond degraded, ring stopped, dashed deploying) in one line per environment.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
