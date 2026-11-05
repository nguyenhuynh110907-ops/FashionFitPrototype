# GarmentCode CPU trial

See [Outputs/REPORT.md](Outputs/REPORT.md) for the three completed trials, contact counts, timings and limitations. The GLB files include the source body and a blue garment. Source OBJ/rendered images retain panel colors. This is a separate desktop web experiment; it has not been integrated into Unity/VR.

## Windows setup from a fresh clone

Requires standard **Python 3.12 x64**, **Visual Studio C++ Build Tools** and a Windows SDK. The repository excludes the embedded Python environment used on the original workstation. From this directory:

```powershell
py -3.12 -m venv .venv
.venv\Scripts\python.exe -m pip install -r requirements.txt
.venv\Scripts\python.exe -m pip install --no-deps pyrender==0.1.45
.venv\Scripts\python.exe setup_sources.py
.\Open_Repo_Trials.bat
```

The setup script downloads source archives at the commits in `SourceReceipt.json`, checks their SHA256, applies the one warning-message fix described in the report, builds the original Warp fork for CPU and obtains only its matching Clang compiler DLL from the NVIDIA release. Dependencies remain in this `.venv`. The compatibility override uses PyOpenGL 3.1.10 with pyrender 0.1.45; that differs from pyrender's old exact dependency pin and is recorded here because the original renders succeeded with this combination.

The original workstation successfully ran the solver with the package versions in `RuntimePackages.txt`. The fresh-clone setup wrapper is provided for reproduction, but has not been run end to end on a second clean workstation.

## Use

Open <http://127.0.0.1:8765>. Select the tee, skirt or custom neckline result. Drag to orbit, wheel to zoom, or use front/back/neck camera buttons. Changing neckline width/front depth/back depth and running the custom simulation takes roughly 1–3 minutes on the tested CPU. These input numbers are upstream design ratios, not centimetres.

The original English GarmentCode configurator is at <http://127.0.0.1:8766>. More complex designs may take longer. Launchers reuse servers only if their health endpoint reports the expected identity.

To reproduce the measured samples directly:

```powershell
.venv\Scripts\python.exe run_garment_trial.py tee
.venv\Scripts\python.exe run_garment_trial.py skirt
.venv\Scripts\python.exe run_garment_trial.py custom --width 0.1 --front 0.3 --back 0.08
```

Assistive Gym was downloaded and assessed but its full dressing environment did not run on the tested Python runtime. This folder does not claim an Assistive Gym simulation result. See the report for compatibility details.

Licenses remain under `THIRD_PARTY/`. The pinned old NVIDIA Warp license limits its use to research/evaluation. Review each upstream license for your intended use.
