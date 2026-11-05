# Hotfix v0.5.1 — 2026-10-25

Confirmed and fixed: `CommonUsages` in RunwayPlayer was ambiguous between UnityEngine.InputSystem.CommonUsages and UnityEngine.XR.CommonUsages. The exact Input System 1.7.0 source types from Unity's package registry reproduce eight CS0104 errors before the fix. The file now uses an explicit XRCommonUsages alias at all eight use sites.

The earlier limited compile-check substitute did not expose InputSystem.CommonUsages, so it missed this namespace collision. The check now includes the actual CommonUsages.cs and InternedString.cs files from Input System 1.7.0. Both Windows and Android runtime reference checks pass after the fix. This remains a reference compilation, not a Unity Editor run or complete package/Editor/shader build.

The screenshot shows the generic compiler-error banner, not the individual Console messages. This hotfix resolves a reproduced source error, but additional errors on the user's machine cannot be ruled out without Console details.

Import FashionFit_Compile_Fix_v0.5.1.unitypackage through Assets > Import Package > Custom Package in the existing FashionFitPrototype project; it replaces only Assets/FashionFit/Runtime/RunwayPlayer.cs with the same GUID. No scene, model, package setting or user garment is replaced.

No remote access to the user's Unity Editor was available. No claim of having clicked or tested the user's Windows application is made.
