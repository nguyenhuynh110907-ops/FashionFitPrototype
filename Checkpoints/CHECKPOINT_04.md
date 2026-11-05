# Checkpoint 04 — Reference garment, 2026-10-25

Input: `Reference/Dress_Source.png`, the user's front product image. Input was preserved unchanged. Checkerboard backdrop is not mapped to the garment.

Completed:
- Independent long A-line dress, fitted upper band, two wide straps, six metallic buttons, lining, rolled hem, parallel seam geometry.
- Runtime binary mesh with UV coordinates, four materials and bone weights; portable OBJ and rebuild script included.
- Integration into the desktop/VR look cycle; startup selects the dress.
- Procedural anti-aliased micro-weave shader, matte fabric, separate metallic hardware.
- Two skirt sway blend shapes. These are authored preview deformation, not simulated cloth.
- Neutral dark underlayer on the existing Ronaldo source body; original hand texture protected by vertex-color mask.
- Offline Blender front, detail and inferred-back renders; first pass exposed floating straps, subsequently conformed to the actual shoulder surface. The source-body texture path was repaired after moving the Blender scene.
- C# syntax check; runtime compile-check with real Unity 2021.3 engine references and small Input System/XR Management substitutes, Windows and Android branches. This does not establish a Unity package or native build.

Assumptions:
- Back construction, garment dimensions, weave, physical properties and body fit are inferred from one image. The back render is explicitly labeled inferred.
- Material color is an approximation under selected lighting, not calibrated color measurement.
- The existing source avatar has not been replaced or upgraded to photoreal quality. A dark underlayer remains visible at shoulders/arms.

Still to verify:
- Actual Unity 2022.3 import, shader compilation, Play mode and Quest performance.
- Skirt/leg collision during all steps and turns; strap deformation under animation.
- Clothing movement consistency and close-range detail on headset.

Next work:
1. Test inside Unity and record actual screenshots/FPS.
2. Correct shoulder/arm fit and cloth penetration based on moving poses.
3. Introduce a clean body model and improve fabric microdetail/fit against additional product views.
4. Add physically based cloth simulation or validated pose deformation only after performance measurement.

Usage constraint: the assistant cannot inspect account usage percentage. This checkpoint is saved at the geometry/integration milestone instead of claiming to detect 5% remaining.

No Nano Banana service was invoked. This is geometry-first reconstruction from the user-provided visual reference, with explicit estimates. No native executable has been built.
