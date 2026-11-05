---
name: fashionfit-performance
description: "Review real app timing data or prepare a user-driven Meta Link capture."
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash
---

Read Docs/STUDIO_WORKFLOW.md and the requested performance.json plus frames.csv.
If no capture exists, say NOT_ASSESSED. Use `Record_VR_Performance.bat --check`
to check the installed capture path, then launch recording only within the user's
requested scope. The actual user operates Quest Link and Touch. Do not label
injected input physical testing. Never enter API keys or send captures elsewhere.
Summarize p50/p95/p99, sample count, capture length, resolution, XR/tracked counts,
vSync/target frame rate and the proposed 72 Hz budget. Missing counters remain
NOT_ASSESSED; wall time includes pacing. Identify top measured bottlenecks and
one focused next experiment. Do not claim fixes from a lower cached-mesh-load time.
