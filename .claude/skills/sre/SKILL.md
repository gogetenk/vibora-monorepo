---
name: sre
description: Trigger the SRE monitoring agent to check Sentry for errors, performance anomalies, and propose bugfix PRs autonomously. Use when you want to run an observability check.
user-invocable: true
argument-hint: "[focus-area]"
---

# SRE Monitoring Check

You MUST use the Agent tool to dispatch the `sre` subagent. Use `subagent_type: "sre"` and run it in the background.

Prompt to send to the agent:

```
Run a full SRE monitoring check now. Follow your monitoring procedure (Steps 1-5).

Additional focus area (if specified): $ARGUMENTS

Report back with your findings and any PRs created.
```

When the agent completes, display its report to the user.
