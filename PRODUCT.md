# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Solo developers, homelab users, and small dev teams running their own services on a single machine (home server or VPS). They manage several side projects or products at once, deploy from CI/CD, and want to see at a glance what is healthy, what changed, and what broke. Small teams additionally use invites and roles.

## Product Purpose

Haven is a self-hosted, single-machine platform for managing Docker containers and Compose stacks across projects and environments. It provides a Project → Environment → Service hierarchy, isolated networks per environment, shared secrets and environment variables, webhook-triggered deployments, and a dashboard tying it together. Success: infrastructure is reproducible, deployments plug into CI/CD, services spin up and tear down quickly, and everything running is visible.

## Positioning

Two things neighbors (Portainer, Coolify, Dokploy) do not combine: YAML manifests are the version-controllable source of truth, mirrored on every mutation and restorable or reconcilable from files; and a strict Project → Environment → Service hierarchy with per-environment network isolation. Haven builds on Docker and Compose rather than replacing or abstracting them.

## Operating Context

Runs as a Docker container with the Docker socket mounted, backed by PostgreSQL. Used alongside CI/CD pipelines via webhooks and the REST API, with notifications and alerts. Operators move between the web dashboard, the API, and manifest files in git. Early development: not recommended for critical production use.

## Capabilities and Constraints

- Entities: projects, environments, services, networks, sidecars, service registry, git credentials, notification channels, jobs (Hangfire), events, Traefik config.
- Auth: setup flow, login, password set, invite acceptance, account and settings pages.
- Frontend: React 19 + TypeScript + Vite SPA in `src/Presentation/Haven.Web`, served with a REST API (FastEndpoints).
- Internationalization is required: all user-facing strings must stay translatable (`src/i18n`).
- Branch `feat/design-v2` indicates a visual redesign is in progress; the visual direction is not decided here.

## Brand Commitments

Name "Haven". Logo assets exist in `assets/` (`logo-light.svg`, `logo-dark.svg`), with light and dark variants.

## Evidence on Hand

README, CONTRIBUTING, and `docs/` describe features and examples. No testimonials, customers, or benchmarks exist; do not fabricate any.

## Product Principles

1. Files are the truth: UI actions must reflect and never obscure what lands in the manifests.
2. Visibility first: health, change, and failure should be answerable at a glance.
3. Respect the hierarchy: Project → Environment → Service is the mental model everywhere.
4. Build on Docker, don't hide it: expose real container concepts and terminology.
5. Fast to spin up, fast to tear down: low ceremony for experimentation.

## Accessibility & Inclusion

No formal standard required. Strings must be translatable (i18n).
