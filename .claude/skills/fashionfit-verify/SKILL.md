---
name: fashionfit-verify
description: "Run fresh FashionFit automated verification and rendered Ivory/red-hat trial."
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash
---

Read Tools/Studio/studio.json and Docs/STUDIO_WORKFLOW.md. Use the project's
.ml-runtime/python/python.exe if present, otherwise Python 3.

From the project root run `Verify_FashionFit.bat --suite full`. For the targeted
Ivory/red-hat scope use `--suite trial`. Use `--unity-project ABSOLUTE_COPY` only
when that copy has the same reviewed source/resources; preserve a source manifest.
Do not open a second Editor on an already open project or kill existing games.
The runner builds its own player and generates a NEW output folder. It does not
install/promote it. Read its summary.json/REPORT.md and detailed evidence; do not
use an older report after a failure. Retain NOT_ASSESSED for missing dependencies.
Report tested behaviors and counts, performance measurement limits, physical
HMD acceptance still pending, and exact result paths.
