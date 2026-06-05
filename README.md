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
- **Automated deploys** by quickly adding a webhook into your CI/CD pipelines and having haven handle the rest.
- **Disaster Recovery** in case everything breaks and now you need to set everything up again, or, in a less anarchic view, just sharing setups or migrating to a new machine.
- **Quickly spinning up services** for experimentation and testing in an isolated environment where you can quickly delete without issues.
- **Configuring environments** for small teams, where you can have Development, Staging or even Production environments somewhere you can easily and quickly manage

> [!WARNING]
> Haven is in early development and is not recommended to be used in critical scenarios (such as your own production server at work),
> use at your own risk. 