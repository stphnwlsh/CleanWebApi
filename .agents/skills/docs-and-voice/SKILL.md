---
name: docs-and-voice
description: Where documentation lives (README, docs/RUN.md, the Lean Mean Clean Apps docs site) and the house voice - professional with a dry sense of humour, Australian spelling, no humour in reference or steps. Use when writing a README, docs page, release note, issue reply or pull request description.
metadata:
  scope: repository
---

# Docs and voice

## The rule

| What | Where |
| --- | --- |
| A short introduction, install, the options table and links | `README.md` and `NUGET.md` (the package page) |
| Running the app, environments, database and tests | `docs/RUN.md`, which also ships in every generated solution |
| Full guides | The docs site, <https://leanmeancleanapps.github.io>, in the `leanmeancleanapps.github.io` repository, with a home page per template |
| How to contribute | `CONTRIBUTING.md` |
| How agents work here | `AGENTS.md` and `.agents/` |

The voice is a small team that knows its stuff and doesn't take itself too seriously: the Lean Mean Clean Apps team, which is Stephen Walsh for now and open to more. Professional first, with the odd dry line.

| Kind of writing | Humour |
| --- | --- |
| Home pages, introductions, explanations, contributing guides | Yes, lightly, one line at a time |
| Reference tables, steps, commands, error messages, skills | None |

The templates are always listed in the same order: Minimal API, MCP, GraphQL, Web API, Lambda Function.

## Steps

1. Lead with the point. Short sentences, plain words, Australian spelling (behaviour, colour, initialise).
2. Show the command or the code instead of describing it.
3. Change the docs in the same pull request as the behaviour they describe.
4. Guides go to the docs site repository, not into this repository.
5. Release notes come from pull request titles, so write titles as plain sentences about the change.

## Don't

- Use marketing words ("blazing fast", "revolutionary", "seamless").
- Put jokes in steps, tables or error messages.
- Duplicate a guide from the docs site in the README.
- Write in the first person singular; it's "we" and "the team".

## Check

Read it out loud. If a sentence needs a second breath, split it.
