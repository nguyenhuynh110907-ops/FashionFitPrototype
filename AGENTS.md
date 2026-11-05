# FashionFit project context

Read `Docs/STUDIO_WORKFLOW.md` for the focused studio workflow and exact checks.
Engine: Unity 2022.3.62f1; target: Windows x64 / Meta Quest Link PCVR.
Runtime and Editor code live under `Assets/FashionFit/`, resources under
`Assets/Resources/`. Use the existing controllers, solver and verification paths.

Implement reversible work already requested by the user without repeating
per-file permission questions. Preserve .meta GUIDs and author Unity assets
through the Editor API. Keep old playable releases; install alongside them.
Do not kill or overwrite an existing game or modify the source while its Editor
is compiling. Do not commit/push/publish without authorization for that action.

Roles in `.claude/agents/` are sequential review lenses by default. No automatic
delegation or concurrent Unity Editors against one project. The Python/BAT
verification tools work independently of Claude Code.

Use fresh evidence. Empty scope, missing profiler counters or missing dependencies
must be reported as NOT_ASSESSED/INCOMPLETE. Separate injected Touch inputs from
physical headset acceptance, sampled mesh contact from continuous collision,
and illustrative material parameters from measured textile calibration.

Run `Verify_FashionFit.bat --suite trial` for Ivory/red-hat/input changes or
`--suite full` for a release. Review `summary.json`, detailed evidence and images.
Actual HMD/Touch usability and long-session performance remain manual checks.
