# Contributing Guidelines
Thanks for your interest in contributing to Haven! This document covers everything you need to get started.

## Before we start
Haven is pre-1.0 and under active design/development. APIs, domain models, and even some architectural decisions are still in flux so expect things to move a bit quickly and occasionally break. 
If you're contributing during this phase, we have some requests:
- Try to favor small PRs over large ones since they're easier to review and less likely to conflict with possible changes
- Check open issues before starting significant work, in case something's already in flight or the design is still being decided
- Don't be surprised if we hold off a PR because it's touching an area that's still being actively redesigned.
  Usually we'll also have an issue for the redesign to keep it public that it's being worked on

## Proposing Features and Reporting Bugs
There are issue templates for helping you write bug reports and feature requests, that said we have a few suggestions for writing them:
- Ideally we should be concise, we don't need an entire essay on why a label not being aligned is a bug, but we still need enough context to understand and reproduce it
- Feature requests ideally should solve a pain point that you found when using the project rather than creating features for the sake of it
- Open issues that were obviously written by AI will be closed and not considered.
  While we allow AI assistance when developing (as long as the features have the proper tests and validation), no one likes to read AI slop.
  See the-AI Assisted Contributions section for more information

## Project Structure
Haven (mostly) follows Clean Architecture with Domain-Driven Design and CQRS. The dependency rule is simple: dependencies only point inward, toward the domain. There are some exceptions but 
they rarely happen such as the domain layer having `Mediator.Abstractions` for the `INotification` marker interface

## Development Workflow
We follow a trunk-based workflow with master being the only long-lived branch, meaning there isn't a `develop` branch to target, meaning it is very important that our
changes don't break anything else and as mentioned before are small and focused to make sure we don't have any undesired side effects.
1. Fork the repository
2. Branch off master

```bash
git switch -c feat/my-feature
```
3. Make your changes, following the coding standards and testing guidance below
4. Open a pull request against `master`

## Coding Standards
- All formatting and code styles are enforced by the [.editorconfig](https://github.com/thiagomvas/haven/blob/master/.editorconfig) and
  [ESLint rules](https://github.com/thiagomvas/haven/blob/master/src/Presentation/Haven.Web/eslint.config.js)
- CI runs analyzers and formatting checks on every PR. While these checks aren't required to merge, it is still recommended to run a
  quick `dotnet format` or `npm run lint:fix` to fix any formatting issues before opening the PR (you'll see a bunch of `style: fix linting` commits on the history)
- Follow the existing CQRS conventions, with any write operations to the database happening on `Command`s and external side effects from events
  happening on the respective domain event's notification handlers

## Testing
Since the project is being actively developed and as a consequence breaking changes can happen, we *strongly encourage* writing tests for your feature
or your fix to make sure the bug wont happen again (especially if its an edge case). Writing tests isn't a hard requirement but if your logic is easy enough to write tests for
then we'll probably end up asking that you write some tests too. It's just a good practice either way and helps a lot

## Commit Messages
We follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) rules for messages
```ascii
<type>(<optional scope>): <description>

[optional body]
```

Common types:
 
| Type | Use for |
|---|---|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation-only changes |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or correcting tests |
| `chore` | Build process, tooling, or dependency changes |
| `perf` | Performance improvements |
| `style` | Code formatting changes |

## Submitting a PR
1. Make sure your branch is up to date with `master`
2. Open the PR against `master` with a clear title (which should follow Conventional Commits format) and a short and sweet description of your change
3. Again, keep PRs small and focused to make reviewing easier
4. A maintainer will review and possibly ask for changes
5. Once approved, the PR will be merged.

## AI-Assisted Contributions
Haven does not prohibit the use of AI tools (Claude, Copilot, ChatGPT, or similar) for your contributions. Use whatever helps you work effectively.
That said, the bar for a PR doesn't change based on how it was written:
- You are responsible for the code you submit regardless of whether a human or an AI tool authored the initial draft.
- AI-assisted changes should be tested at least as rigorously as hand-written ones. Given Haven's current testing posture (see Testing),
  this mostly means: don't lean on relaxed test expectations as an excuse to submit unreviewed AI output.
- Large mostly-AI-generated PRs will very likely be denied, especially if it's obvious that it isn't using existing implementations
  and our conventions
