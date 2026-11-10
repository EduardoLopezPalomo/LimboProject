# Ciro's Vengeance

## Overview
This a Unity project. This is a game we are currently developing

---

## Agent memory lives in the Obsidian vault (NOT in this repo)

Since 11/10/2026 the agent work log is split into tasks and reports inside the vault, use Obsidian MCP:

- `Agents Home.md` - start here. Session protocol and writing rules.
- `Dashboard.md` - what is open, what is blocked on a decision from the user, current baselines.
- `Working Agreement.md` - Definition of Done, verification ladder, code rules, git rule.
- `Tasks/` (T-001...) - one file per open item. `Decisions/` (D-01...) - one file per question for the user.
- `Reports/` - one write-once file per finished pass. `Reference/` - durable how-to recipes.
- `01` to `07` in the same vault - how the game works (reference documentation, verified 11/10/2026).

The old repo files MEMORY.md, NOTES.md and SYSTEMS_REVIEW_PLAN.md were moved there and deleted. Their originals are kept in `08 Agents\_Archive\`.

## Workflow

1. Read `08 Agents\Dashboard.md` to pick up the task where the last agent left it
2. Read the `Tasks\T-###` file you are working on, and `Working Agreement.md`
3. Explore the repository for the task's files. Update the task file with your findings
4. Plan the needed changes to fullfil the task and propose it to the user
5. Consider the user's feedback on the planing. Record the final plan in the task file
6. Implement the changes and keep the task file up to date. The user will test them
7. Write a report: `08 Agents\Reports\YYYY-MM-DD <title>.md` (write-once), then close the task and refresh `Dashboard.md`

## Environment

We are on windows, you have access to powershell

## About the Tasks and Reports folders

They are extremely important - keep them up to date with any advancement done. A task is a file, a finished pass is a file, and a report is never edited after it is written: if you disprove something, write a new report.

## aboit GIT

Do NOT commit changes on your own. Leave things to be committed by the user.