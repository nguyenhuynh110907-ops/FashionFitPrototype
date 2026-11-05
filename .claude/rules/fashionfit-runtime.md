---
paths:
  - "Assets/FashionFit/Runtime/**/*.cs"
  - "Assets/FashionFit/Editor/**/*.cs"
---
Read the existing controller/grab/solver call chain before editing it. Preserve
two independent hand owners, near-only physical grip and last-hand release.
POSE selects joint/base handles; VẢI selects clothing. Normal grip removes a worn
hat; modifier grip can adjust it in place. Missing tracking releases safely.
Apply gravity on released pickups and free cloth panels; mannequin supports
remain anchored. Do not equate a draping panel's rigid motion with fibre strain.
Keep optional QA/telemetry disabled unless explicitly launched. No per-frame
mesh.vertices allocations for measurement; collect buffers and write files at end.
Use the pinned 2022.3 APIs. Preserve NativeArray ownership/disposal and cache GUIDs.
