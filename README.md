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

## Running Haven
### As a Docker Container 
To run Haven as a docker container, you need to mount the Docker socket to allow haven to deploy your services. 

You can quickly get Haven started by running the following command:
```
docker run -d --name thiagomvas/haven:latest -p 8080:8080 -v haven-data:/data -v /var/run/docker.sock:/var/run/docker.sock haven
```

### Using Docker Compose
A ready-to-use Compose file is available at [`docs/examples/docker-compose.yml`](docs/examples/docker-compose.yml). Copy it to your working directory and run:

```bash
docker compose up -d
```

Or use the inline version below:

```yaml
services:
  haven:
    image: thiagomvas/haven:latest
    container_name: haven
    restart: unless-stopped
    ports:
      - "8080:8080"
    volumes:
      - haven-data:/data # Haven's database is here
      - /var/run/docker.sock:/var/run/docker.sock

volumes:
  haven-data:
```