# FashionFit development studio — focused adaptation

This is an EXISTING Unity project, not a blank game template.
Read Docs/STUDIO_WORKFLOW.md and Tools/Studio/studio.json first.

## Fixed project context
- Unity **2022.3.62f1**, Windows x64, Meta Quest Link PCVR.
- Input System 1.7.0, OpenXR 1.11.0, XR Management 4.4.0, glTFast 6.12.1.
- Runtime: Assets/FashionFit/Runtime. Editor: Assets/FashionFit/Editor.
- Runtime resources: Assets/Resources. User reports: Verification/.
- The current production solver includes body fields/native fitted wearables,
  elastic hats and layered áo dài. Understand these before changing them.
- Keep .meta GUIDs. Generate mesh/scene/settings through the Unity Editor API.

## Work style
Proceed with reversible implementation and verification inside the user's
requested scope. Do not repeatedly ask to edit every file. Describe meaningful
findings and the next action. Ask only for genuinely missing requirements or
actions outside the authorized scope. Never commit, push, publish or send
messages without authorization for that action.

Six role definitions are available as review lenses. Work through them
sequentially by default. Do not automatically fan out a studio, invoke nested
agents or run multiple Unity Editors against one project. Delegate only when
the user or applicable higher-priority instructions explicitly request it.

## Definition of done
1. Reproduce with a named garment, avatar, pose and controller sequence.
2. Make the smallest useful fix; preserve existing grip release/gravity rules.
3. Run Verify_FashionFit.bat with the relevant suite; retain fresh evidence.
4. Separate automated injected input from physical HMD/Touch acceptance.
5. Compare rendered images when changing appearance. Topology alone is not art QA.
6. Report changed behavior, evidence, remaining limitations and exact player path.
7. Build a new version alongside the old one; never replace a running game.

## Measurement integrity
- No profiler output -> NOT_ASSESSED. No exports -> NOT_ASSESSED/failed, never pass.
- Zero recorded GPU/CPU samples means unavailable, not zero milliseconds.
- A sample's sampled contact pass does not prove continuous/self collision.
- Command travel (cm), local strain (%), membrane tension (N/m), and virtual
  hand command force (N) are different quantities.
- Current material presets are illustrative and UNCALIBRATED. Textures do not
  determine elastic modulus or breaking strength. Do not claim training/calibration.
- 72 Hz / 13.89 ms is a proposed app budget, not a measured Link/compositor result.
- Never hide body triangles or disable gravity to make a check appear green.
- Do not replace Resources, render pipeline, input stack, engine version or
  material model solely because a generic studio guide prefers another pattern.

## Available commands in Claude Code
/fashionfit-verify — execute the real existing checks and the rendered trial.
/fashionfit-performance — inspect captures or prepare a real user-driven recording.
/fashionfit-review — review evidence quality and uncovered behavior.

The Python/BAT tools also run without Claude Code. This is a curated adaptation,
not an installation of the upstream 49-role studio. See THIRD_PARTY/CCGS/NOTICE.md.
