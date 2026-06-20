<div align="center">
<br />
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/logo-dark.svg">
  <img alt="Haven" src="./assets/logo-light.svg" width="160">
</picture>
<br />
<br />
<h1>Haven</h1>
<p><em>Self-hosted application orchestration for small teams and solo developers.</em></p>
<br />
</div>


Haven is a single-machine platform for managing Docker containers and Compose stacks across multiple projects and environments. 
It gives you a structured project/environment/service hierarchy, isolated networks per environment, a shared secrets and environment variable system, webhook-triggered deployments, and a dashboard to tie it all together.

It **does not replace** Docker, Docker Compose, or Kubernetes, it builds on top of them. The goal is to make four things easy:
- **Reproducible Infrastructure** so your entire setup lives in version-controlled files, meaning you can rebuild, share, or migrate everything at any time without starting from scratch.
- **Integrated Deployments** by sitting naturally inside your CI/CD pipelines, with webhooks, alerts, notifications and a proper API surface for your tooling to interact with.
- **Unified Service Management** across every project and environment in one place, where you can quickly spin up services for experimentation and tear them down just as fast.
- **Visibility** over everything running on your machine, so you always know what's healthy, what changed, and what broke.

> [!WARNING]
> Haven is in early development and is not recommended to be used in critical scenarios (such as your own production server at work),
> use at your own risk. 
