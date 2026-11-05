"""Check saved training artifacts and independent image inference, offline."""
import contextlib
import csv
import json
import os
from pathlib import Path
import sys
import tempfile

os.environ["HF_HUB_OFFLINE"] = "1"
sys.path.insert(0, str(Path(__file__).resolve().parent))
import fitvision as fv
import numpy as np
import torch
from safetensors.torch import load_file

fv.setup()
config = fv.read_json(fv.ARTIFACTS / "model.json")
metrics = fv.read_json(fv.ARTIFACTS / "metrics.json")
splits = fv.read_json(fv.ARTIFACTS / "splits.json")
manifest = fv.read_json(fv.MANIFEST)
assert fv.sha256(fv.MANIFEST) == config["dataset_manifest_sha256"]
assert fv.sha256(fv.ARTIFACTS/"heads.safetensors") == config["checkpoint_sha256"]
assert config["deployment_status"] == "research_only"
assert config["always_require_review"] is True
assert len(splits) == len(manifest) == 100
group_sets = {name: {x["group"] for x in splits if x["split"] == name} for name in ("train", "validation", "test")}
assert not group_sets["train"] & group_sets["test"]
assert not group_sets["train"] & group_sets["validation"]
assert not group_sets["validation"] & group_sets["test"]
state = load_file(fv.ARTIFACTS/"heads.safetensors")
assert sum(value.numel() for value in state.values()) == metrics["trainable_head_parameters"] == 4620
assert all(torch.isfinite(value).all() for value in state.values())
assert sum(int(torch.count_nonzero(value)) for value in state.values()) > 4500
assert metrics["best_epoch"] >= 1 and metrics["trained_encoder_parameters"] == 0
for row in manifest:
    assert fv.sha256(fv.MANIFEST.parent/row["file"]) == row["download_sha256"].lower()
with (fv.ARTIFACTS/"predictions.csv").open(encoding="utf-8-sig",newline="") as stream:
    predictions = list(csv.DictReader(stream))
prediction_by_id = {row["id"]:row for row in predictions}
for task, head in metrics["heads"].items():
    actual = [row for row in predictions if row["split"] == "test" and row[task+"_true"] != "unlabeled"]
    correct = sum(row[task+"_true"] == row[task+"_predicted"] for row in actual)
    assert abs(correct/len(actual)-head["splits"]["test"]["accuracy"]) < 1e-12
checked, max_error = [], 0.
# Deterministic first three held-out images, including failures; no cherry-pick.
for split in [item for item in splits if item["split"] == "test"][:3]:
    row = next(item for item in manifest if item["id"] == split["id"])
    target = fv.ARTIFACTS/("verification_"+row["id"]+".json")
    with contextlib.redirect_stdout(sys.stderr):
        fv.predict(fv.MANIFEST.parent/row["file"],target)
    result = fv.read_json(target)
    for prediction in result["predictions"]:
        task = prediction["task"]
        expected = prediction_by_id[row["id"]]
        assert prediction["label"] == expected[task+"_predicted"]
        difference = abs(prediction["probability"]-float(expected[task+"_probability"]))
        max_error = max(max_error,difference)
        assert difference < 1e-4
        assert all(0<=item["probability"]<=1 for item in prediction["top3"])
    checked.append(row["id"])
old_artifacts = fv.ARTIFACTS
try:
    with tempfile.TemporaryDirectory(dir=fv.ROOT,prefix="verify_temp_") as temp:
        fv.ARTIFACTS = Path(temp)
        bad = dict(config); bad["checkpoint_sha256"] = "0"*64
        fv.write_json(fv.ARTIFACTS/"model.json",bad)
        (fv.ARTIFACTS/"heads.safetensors").write_bytes((old_artifacts/"heads.safetensors").read_bytes())
        try:
            fv.predict(fv.MANIFEST.parent/manifest[0]["file"],fv.ARTIFACTS/"unexpected.json")
            raise AssertionError("Mismatched checkpoint accepted")
        except ValueError as error:
            assert "hash mismatch" in str(error)
finally:
    fv.ARTIFACTS = old_artifacts
report={"status":"passed","dataset_images_verified":100,"group_leakage":False,
        "trainable_parameters":4620,"independent_inference_examples":checked,
        "max_probability_difference":max_error,"tampered_checkpoint_rejected":True,
        "offline_inference":True,"checkpoint_sha256":config["checkpoint_sha256"],
        "repeat_training_same_checkpoint_as_first_run":config["checkpoint_sha256"]=="195f9f5b7e5ebaf8f30451037b44e6ef2a97262c0d81a18e4415e6b41fa630c1"}
fv.write_json(fv.ARTIFACTS/"verification.json",report)
print(json.dumps(report,ensure_ascii=False))
