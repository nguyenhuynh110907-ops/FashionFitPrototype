# Interaction fix v0.5.2 — 2026-10-25

User screenshot of v0.5.1 confirms the room, female stand-in and skirt are visible in Unity 2022.3.62f1, with FITTING_READY and zero red Console errors. The user reports no movement/interaction. This is user-side visual evidence only; no remote control or direct Unity run was available here.

Confirmed configuration incompatibility: OnGUI runtime UI with activeInputHandler=1 (new-only). The exact Input System 1.7 documentation states OnGUI does not receive input events with that setting. Changed the shipped ProjectSettings value to 2 (Both).

Other changes: desktop key/mouse polling uses legacy API only when ENABLE_LEGACY_INPUT_MANAGER is compiled, with a new-input fallback for other builds; observer movement uses unscaled time; click controls orbit or dolly the camera; the sample model starts walking automatically after dressing; the Editor focuses Game on entering Play.

Confirmed false-error risk: Unity serializes public strings and can restore an unset string as empty; the previous FatalError==null guard allowed an empty error overlay. FatalError is now a nonserialized property and the overlay tests IsNullOrEmpty. Actual exception reporting remains.

Validation: runtime reference compilation passes for Windows new-input, Android new-input, and Windows with ENABLE_LEGACY_INPUT_MANAGER. The check includes the real CommonUsages and InternedString source types from Input System 1.7, while device/XR entry-point substitutes remain. C# syntax is checked separately for all project scripts. This is not a Unity 2022.3 Editor/package/shader build or input event integration test. Cloth code is unchanged; previous numerical results remain applicable only to that numerical core.

Required user-side check after fully restarting/opening the corrected project: UI mouse clicks work, model walks automatically, pause/resume freezes only the model/cloth, four camera buttons move the view, WASD/right-mouse work with Game focused, no false fatal overlay, and no red Console errors.

The screenshot reports AMD Radeon 860M Graphics. The reported memory value is not a validated dedicated-VRAM measurement or a VR performance result.
