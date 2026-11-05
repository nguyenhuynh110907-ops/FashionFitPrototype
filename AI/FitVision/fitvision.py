"""Local transfer learning for garment category and coarse appearance family.

No body dimensions, sewing patterns, material parameters, or motion supervision
are present in this dataset. The frozen encoder is NOT trained by this script.
"""
from __future__ import annotations

import argparse
import contextlib
import copy
import csv
import hashlib
import json
import os
import platform
import random
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parent.parent
RUNTIME = PROJECT / ".ml-runtime"
os.environ.setdefault("HF_HOME", str(RUNTIME / "huggingface"))
os.environ.setdefault("HF_HUB_DISABLE_TELEMETRY", "1")
os.environ.setdefault("HF_HUB_DISABLE_XET", "1")
os.environ.setdefault("HF_HUB_DISABLE_IMPLICIT_TOKEN", "1")
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")
os.environ.setdefault("OMP_NUM_THREADS", "4")
os.environ.setdefault("MPLCONFIGDIR", str(RUNTIME / "matplotlib"))
import numpy as np
import torch
from PIL import Image, ImageOps
from safetensors.torch import load_file, save_file
from sklearn.metrics import accuracy_score, balanced_accuracy_score, confusion_matrix, f1_score

SEED = 27102026
ENCODER_ID = "facebook/dinov2-small"
ENCODER_DIR = ROOT / "Encoder"
ARTIFACTS = ROOT / "Artifacts"
MANIFEST = PROJECT / "References/ModelWear100/manifest.json"
PREPROCESS = "exif-transpose; RGB; bicubic letterbox 224x224 gray127; DINO normalization; CLS"
STYLE_MAP = {
    "fitted": "close", "column": "close",
    "flared": "volume", "voluminous": "volume", "pleated": "volume",
    "straight": "straight_relaxed", "loose": "straight_relaxed",
    "oversized": "straight_relaxed", "wide": "straight_relaxed",
    "wrap": "draped_layered", "draped": "draped_layered", "layered": "draped_layered",
    "tailored": "tailored",
}
# Known repeated outfits in the manual image review. Conservative grouping also
# includes C098 with the other black short-sleeved runway dress views.
OUTFIT_GROUPS = [
    ["005", "006", "007"], ["017", "045"], ["018", "037"],
    ["024", "036"], ["027", "041"], ["038", "042"],
    ["064", "094", "098"], ["068", "076"], ["078", "081"],
    ["079", "086"], ["166", "193"],
]


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def write_json(path, data):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + ".tmp")
    temp.write_text(json.dumps(data, ensure_ascii=False, indent=2, allow_nan=False), encoding="utf-8")
    temp.replace(path)


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def setup():
    random.seed(SEED)
    np.random.seed(SEED)
    torch.manual_seed(SEED)
    torch.set_num_threads(4)
    torch.use_deterministic_algorithms(True)
    ARTIFACTS.mkdir(parents=True, exist_ok=True)


def load_encoder(download=False):
    from transformers import AutoImageProcessor, AutoModel
    if not (ENCODER_DIR / "config.json").exists():
        if not download:
            raise RuntimeError("Encoder not cached. Run training once with --download-encoder.")
        from huggingface_hub import model_info
        revision = model_info(ENCODER_ID).sha
        print(f"Downloading encoder {ENCODER_ID}@{revision}; no dataset upload", flush=True)
        processor = AutoImageProcessor.from_pretrained(ENCODER_ID, revision=revision, backend="pil", trust_remote_code=False)
        encoder = AutoModel.from_pretrained(ENCODER_ID, revision=revision, use_safetensors=True, trust_remote_code=False)
        ENCODER_DIR.mkdir(parents=True, exist_ok=True)
        encoder.save_pretrained(ENCODER_DIR, safe_serialization=True)
        processor.save_pretrained(ENCODER_DIR)
        write_json(ROOT / "encoder_source.json", {"model": ENCODER_ID, "revision": revision,
                   "source": f"https://huggingface.co/{ENCODER_ID}", "license": "Apache-2.0",
                   "preprocess": PREPROCESS, "frozen": True})
    processor = AutoImageProcessor.from_pretrained(ENCODER_DIR, local_files_only=True, backend="pil", trust_remote_code=False)
    encoder = AutoModel.from_pretrained(ENCODER_DIR, local_files_only=True, use_safetensors=True, trust_remote_code=False)
    encoder.eval().requires_grad_(False)
    return processor, encoder


def image_tensor(path, processor):
    with Image.open(path) as source:
        image = ImageOps.exif_transpose(source).convert("RGB")
        image = ImageOps.pad(image, (224, 224), method=Image.Resampling.BICUBIC, color=(127, 127, 127))
        return processor(images=image, do_resize=False, do_center_crop=False, return_tensors="pt")["pixel_values"][0]


def extract(rows, download):
    processor, encoder = load_encoder(download)
    signature = hashlib.sha256((sha256(MANIFEST) + json.dumps(read_json(ROOT / "encoder_source.json"), sort_keys=True)).encode()).hexdigest()
    cache = ARTIFACTS / "features.npz"
    if cache.exists():
        with np.load(cache, allow_pickle=False) as old:
            if str(old["signature"].item()) == signature:
                print("Using verified cached image features", flush=True)
                return old["features"].copy()
    features = []
    with torch.inference_mode():
        for start in range(0, len(rows), 4):
            pixels = torch.stack([image_tensor(MANIFEST.parent / row["file"], processor) for row in rows[start:start+4]])
            hidden = encoder(pixel_values=pixels).last_hidden_state[:, 0]
            features.append(torch.nn.functional.normalize(hidden, dim=1).numpy())
            print(f"Encoded {min(start+4,len(rows))}/{len(rows)}", flush=True)
    result = np.concatenate(features).astype(np.float32)
    np.savez_compressed(cache, features=result, signature=np.array(signature))
    return result


def make_groups(rows, features):
    parent = list(range(len(rows)))
    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x
    def union(a, b):
        parent[find(b)] = find(a)
    ids = {row["candidate_id"]: i for i, row in enumerate(rows)}
    for group in OUTFIT_GROUPS:
        indices = [ids[x] for x in group if x in ids]
        for i in indices[1:]:
            union(indices[0], i)
    # Deduplication is label-independent and conservative: nearly identical
    # embeddings are kept together, not used as extra training samples.
    similarities = features @ features.T
    pairs = []
    for a in range(len(rows)):
        for b in range(a + 1, len(rows)):
            if similarities[a, b] >= .985:
                union(a, b)
                pairs.append({"a": rows[a]["id"], "b": rows[b]["id"], "cosine": float(similarities[a,b])})
    roots = [find(i) for i in range(len(rows))]
    ordered = {root: f"G{j+1:03d}" for j, root in enumerate(sorted(set(roots)))}
    return np.array([ordered[x] for x in roots]), pairs


def split_groups(groups, labels):
    rng = np.random.default_rng(SEED)
    unique = np.unique(groups)
    best, best_score = None, float("inf")
    # Uses label counts only, never validation/test image performance.
    for _ in range(20000):
        order = rng.permutation(unique)
        a, b = round(len(order)*.6), round(len(order)*.8)
        parts = [np.where(np.isin(groups, part))[0] for part in (order[:a], order[a:b], order[b:])]
        score = sum(abs(len(part)/len(groups)-fraction) for part, fraction in zip(parts, (.6,.2,.2)))
        for target in labels.values():
            valid = target >= 0
            total = np.bincount(target[valid])
            for part, fraction in zip(parts, (.6,.2,.2)):
                values = target[part]; counts = np.bincount(values[values>=0], minlength=len(total))
                score += 100 * np.count_nonzero(counts == 0)
                score += np.mean(np.abs(counts / total - fraction))
        if score < best_score:
            best_score, best = score, parts
    for target in labels.values():
        expected = len(np.unique(target[target>=0]))
        for part in best:
            assert len(np.unique(target[part][target[part]>=0])) == expected, "Insufficient class groups for all splits"
    assert not set(groups[best[0]]) & set(groups[best[1]])
    assert not set(groups[best[0]]) & set(groups[best[2]])
    assert not set(groups[best[1]]) & set(groups[best[2]])
    return dict(zip(("train", "validation", "test"), best))


def metrics(truth, prediction, nclasses):
    return {"n": len(truth), "accuracy": float(accuracy_score(truth, prediction)),
            "balanced_accuracy": float(balanced_accuracy_score(truth, prediction)),
            "macro_f1": float(f1_score(truth, prediction, labels=list(range(nclasses)), average="macro", zero_division=0)),
            "confusion_matrix": confusion_matrix(truth, prediction, labels=list(range(nclasses))).tolist()}


def bootstrap_accuracy(truth, prediction, groups):
    rng = np.random.default_rng(SEED)
    unique = np.unique(groups)
    estimates = []
    for _ in range(2000):
        chosen = np.concatenate([np.where(groups == g)[0] for g in rng.choice(unique, size=len(unique), replace=True)])
        estimates.append(float(np.mean(truth[chosen] == prediction[chosen])))
    return [float(x) for x in np.quantile(estimates, [.025, .975])]


def train(download=False):
    setup()
    started = time.perf_counter()
    rows = read_json(MANIFEST)
    assert len(rows) == 100
    assert len({r["download_sha256"] for r in rows}) == len(rows)
    for row in rows:
        if sha256(MANIFEST.parent / row["file"]) != row["download_sha256"].lower():
            raise ValueError("Dataset file hash mismatch: " + row["id"])
    classes = {"category": sorted({r["category"] for r in rows}), "appearance": sorted(set(STYLE_MAP.values()))}
    labels = {
        "category": np.array([classes["category"].index(r["category"]) for r in rows]),
        "appearance": np.array([classes["appearance"].index(STYLE_MAP[r["silhouette"]]) if r["silhouette"] in STYLE_MAP else -1 for r in rows]),
    }
    features = extract(rows, download)
    groups, duplicate_pairs = make_groups(rows, features)
    splits = split_groups(groups, labels)
    train_idx, val_idx, test_idx = (splits[x] for x in ("train", "validation", "test"))
    mean, std = features[train_idx].mean(0), np.maximum(features[train_idx].std(0), .01)
    x = torch.tensor((features-mean)/std)
    y = {name: torch.tensor(target, dtype=torch.long) for name, target in labels.items()}
    heads = torch.nn.ModuleDict({name: torch.nn.Linear(features.shape[1], len(names)) for name, names in classes.items()})
    for parameter in heads.parameters():
        torch.nn.init.zeros_(parameter)
    weights = {}
    for name in classes:
        values = labels[name][train_idx]; counts = np.bincount(values[values>=0], minlength=len(classes[name]))
        w = 1/np.sqrt(counts.astype(np.float32)); w /= w.mean()
        weights[name] = torch.tensor(w)
    def losses(indices, weighted):
        result = []
        for name, head in heads.items():
            valid = indices[labels[name][indices]>=0]
            result.append(torch.nn.functional.cross_entropy(head(x[valid]), y[name][valid], weight=weights[name] if weighted else None))
        return torch.stack(result).mean()
    optimizer = torch.optim.AdamW(heads.parameters(), lr=.01, weight_decay=.1)
    history, best_loss, best_state, best_epoch = [], float("inf"), None, 0
    initial_loss = float(losses(train_idx, True).detach())
    for epoch in range(1, 401):
        heads.train(); optimizer.zero_grad()
        loss = losses(train_idx, True); loss.backward(); optimizer.step()
        heads.eval()
        with torch.no_grad():
            validation_loss = float(losses(val_idx, False))
        history.append({"epoch": epoch, "train_weighted_ce": float(loss.detach()), "validation_ce": validation_loss})
        if validation_loss < best_loss - .0001:
            best_loss, best_state, best_epoch = validation_loss, copy.deepcopy(heads.state_dict()), epoch
        if epoch % 20 == 0:
            print(f"Epoch {epoch}: train={loss.item():.4f} validation={validation_loss:.4f}; best={best_epoch}", flush=True)
        if epoch - best_epoch >= 50:
            break
    heads.load_state_dict(best_state); heads.eval()
    temperatures = {}
    with torch.inference_mode():
        logits = {name: head(x) for name, head in heads.items()}
        for name in classes:
            valid = val_idx[labels[name][val_idx]>=0]
            candidates = np.geomspace(.5, 5., 60)
            temperatures[name] = float(min(candidates, key=lambda t: float(torch.nn.functional.cross_entropy(logits[name][valid]/float(t), y[name][valid]))))
        probabilities = {name: torch.softmax(value/temperatures[name], dim=1).numpy() for name, value in logits.items()}
    save_file({name: value.contiguous() for name, value in heads.state_dict().items()}, ARTIFACTS / "heads.safetensors")
    config = {"schema_version": 1, "encoder": read_json(ROOT/"encoder_source.json"), "dimension": features.shape[1],
              "classes": classes, "appearance_label_map": STYLE_MAP, "mean": mean.tolist(), "std": std.tolist(),
              "temperatures": temperatures, "seed": SEED, "best_epoch": best_epoch,
              "checkpoint_sha256": sha256(ARTIFACTS/"heads.safetensors"),
              "dataset_manifest_sha256": sha256(MANIFEST), "deployment_status": "research_only",
              "always_require_review": True}
    write_json(ARTIFACTS / "model.json", config)
    summary = {"seed": SEED, "encoder_frozen": True, "trained_encoder_parameters": 0,
               "trainable_head_parameters": sum(p.numel() for p in heads.parameters()),
               "initial_train_loss": initial_loss, "best_epoch": best_epoch, "epochs_run": len(history),
               "unique_outfit_groups": len(np.unique(groups)), "group_overlap_between_splits": 0,
               "split_counts": {name:len(part) for name, part in splits.items()},
               "appearance_unlabeled_ids": [rows[i]["id"] for i in range(len(rows)) if labels["appearance"][i]<0],
               "near_duplicate_pairs": duplicate_pairs, "heads": {}, "deployment_status": "research_only",
               "limitations": ["100 images; small holdout", "Garment/outfit grouping is conservative, not a verified person-identity split",
                  "Photographer/show backgrounds may confound predictions", "No segmentation/crop annotations",
                  "Appearance families combine existing visual tags, not measured tightness", "No pattern, 3D, motion or collision training"],
               "environment": {"python":sys.version, "torch":torch.__version__, "cpu_threads":torch.get_num_threads(), "platform":platform.platform()},
               "checkpoint_sha256": config["checkpoint_sha256"], "dataset_manifest_sha256":config["dataset_manifest_sha256"]}
    predictions = []
    split_lookup = {i:name for name, part in splits.items() for i in part}
    for name in classes:
        detail = {"classes": classes[name], "splits": {}, "class_counts": {}}
        majority = np.bincount(labels[name][train_idx][labels[name][train_idx]>=0]).argmax()
        for split, indices in splits.items():
            valid = indices[labels[name][indices]>=0]
            truth, predicted = labels[name][valid], probabilities[name][valid].argmax(1)
            detail["splits"][split] = metrics(truth, predicted, len(classes[name]))
            detail["class_counts"][split] = dict(zip(classes[name], np.bincount(truth, minlength=len(classes[name])).tolist()))
            if split == "test":
                detail["majority_baseline_test"] = metrics(truth, np.full_like(truth, majority), len(classes[name]))
                detail["test_accuracy_group_bootstrap_95pct"] = bootstrap_accuracy(truth, predicted, groups[valid])
        summary["heads"][name] = detail
    for i, row in enumerate(rows):
        entry = {"id":row["id"], "candidate_id":row["candidate_id"], "group":groups[i], "split":split_lookup[i]}
        for name in classes:
            entry[name+"_true"] = classes[name][labels[name][i]] if labels[name][i]>=0 else "unlabeled"
            predicted = int(probabilities[name][i].argmax())
            entry[name+"_predicted"] = classes[name][predicted]
            entry[name+"_probability"] = float(probabilities[name][i,predicted])
        predictions.append(entry)
    summary["elapsed_seconds"] = time.perf_counter()-started
    write_json(ARTIFACTS/"metrics.json", summary)
    write_json(ARTIFACTS/"editor_summary.json", {"dataset_size":len(rows), "train_count":len(train_idx),
        "validation_count":len(val_idx), "test_count":len(test_idx), "best_epoch":best_epoch,
        "tasks":[{"name":name, "accuracy":detail["splits"]["test"]["accuracy"],
                  "macro_f1":detail["splits"]["test"]["macro_f1"], "test_n":detail["splits"]["test"]["n"]}
                 for name,detail in summary["heads"].items()]})
    write_json(ARTIFACTS/"training_history.json", history)
    write_json(ARTIFACTS/"splits.json", [{"id":row["id"],"candidate_id":row["candidate_id"],"group":groups[i],"split":split_lookup[i]} for i,row in enumerate(rows)])
    with (ARTIFACTS/"predictions.csv").open("w",encoding="utf-8-sig",newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(predictions[0])); writer.writeheader(); writer.writerows(predictions)
    plot(summary, history)
    print(json.dumps({"split_counts":summary["split_counts"], "best_epoch":best_epoch,
          "test":{name:detail["splits"]["test"] for name,detail in summary["heads"].items()},
          "elapsed_seconds":summary["elapsed_seconds"]}, ensure_ascii=False), flush=True)


def plot(summary, history):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    fig, axes = plt.subplots(1, 2, figsize=(13, 5), layout="constrained")
    for ax, (name, result) in zip(axes, summary["heads"].items()):
        matrix = np.array(result["splits"]["test"]["confusion_matrix"])
        ax.imshow(matrix, cmap="Blues", vmin=0)
        ax.set_xticks(range(len(result["classes"])), result["classes"], rotation=45, ha="right", fontsize=8)
        ax.set_yticks(range(len(result["classes"])), result["classes"], fontsize=8)
        ax.set(xlabel="Predicted", ylabel="Reference label", title=f"{name} · held-out n={matrix.sum()}")
        for (r,c), value in np.ndenumerate(matrix): ax.text(c,r,str(value),ha="center",va="center",color="white" if value>matrix.max()/2 else "black")
    fig.savefig(ARTIFACTS/"test_confusion.png", dpi=160); plt.close(fig)
    fig, ax = plt.subplots(figsize=(8,4), layout="constrained")
    ax.plot([x["epoch"] for x in history],[x["train_weighted_ce"] for x in history],label="Train (weighted CE)")
    ax.plot([x["epoch"] for x in history],[x["validation_ce"] for x in history],label="Validation (unweighted CE)")
    ax.axvline(summary["best_epoch"],ls="--",color="gray",label="Selected epoch")
    ax.set(xlabel="Epoch",ylabel="Mean head cross entropy",title="FitVision · frozen DINOv2, trained linear heads")
    ax.legend(); fig.savefig(ARTIFACTS/"learning_curve.png",dpi=160); plt.close(fig)


def predict(image, output):
    setup()
    config = read_json(ARTIFACTS/"model.json")
    if sha256(ARTIFACTS/"heads.safetensors") != config["checkpoint_sha256"]:
        raise ValueError("Checkpoint hash mismatch")
    processor, encoder = load_encoder(False)
    with torch.inference_mode():
        hidden = encoder(pixel_values=image_tensor(image, processor).unsqueeze(0)).last_hidden_state[:,0]
        feature = torch.nn.functional.normalize(hidden, dim=1)
        x = (feature-torch.tensor(config["mean"]))/torch.tensor(config["std"])
        state = load_file(ARTIFACTS/"heads.safetensors")
        result = {"schema_version":1,"image":str(Path(image).resolve()),"deployment_status":"research_only",
                  "requires_review":True,"checkpoint_sha256":config["checkpoint_sha256"],"predictions":[]}
        for name, names in config["classes"].items():
            logits = torch.nn.functional.linear(x, state[name+".weight"], state[name+".bias"])
            probability = torch.softmax(logits/config["temperatures"][name],dim=1)[0]
            ranked = torch.topk(probability,min(3,len(names)))
            result["predictions"].append({"task":name,"label":names[int(ranked.indices[0])],
                "probability":float(ranked.values[0]),"top3":[{"label":names[int(i)],"probability":float(p)} for p,i in zip(ranked.values,ranked.indices)]})
        appearance = result["predictions"][1]["label"]
        guidance = {"close":"Fit theo tung vung nguc-eo-hong; giu do du va khoang tiep xuc rieng.",
                    "volume":"Dung the tich tung/panel va diem giu eo; de gau co do tu do.",
                    "straight_relaxed":"Giu do du theo thiet ke; khong ep vai sat moi diem cua body.",
                    "draped_layered":"Can panel quan/xep ru va khoang ho giua cac lop.",
                    "tailored":"Can cau truc vai, ve, cap va duong may phu hop; anh khong cho thong so vat lieu."}
        result["guidance"] = guidance[appearance]
        result["warning"] = "Research classifier trained on 100 photos. Verify manually; no 3D, cloth physics, or nonpenetration guarantee."
    write_json(output,result)
    print(json.dumps(result,ensure_ascii=False),flush=True)


if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    sub=parser.add_subparsers(dest="command",required=True)
    training=sub.add_parser("train"); training.add_argument("--download-encoder",action="store_true"); training.add_argument("--log-file",type=Path)
    inference=sub.add_parser("predict"); inference.add_argument("--image",type=Path,required=True); inference.add_argument("--output",type=Path,default=ARTIFACTS/"latest_prediction.json")
    args=parser.parse_args()
    if args.command=="train":
        if args.log_file:
            args.log_file.parent.mkdir(parents=True,exist_ok=True)
            print(f"Training locally. Progress log: {args.log_file}",flush=True)
            with args.log_file.open("w",encoding="utf-8") as log, contextlib.redirect_stdout(log), contextlib.redirect_stderr(log):
                train(args.download_encoder)
            print("TRAINING_OK: weights, metrics and plots saved in AI/FitVision/Artifacts",flush=True)
        else: train(args.download_encoder)
    else: predict(args.image,args.output)
