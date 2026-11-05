# Sources and attribution

- Frozen visual encoder: [`facebook/dinov2-small`](https://huggingface.co/facebook/dinov2-small), AI at Meta / Hugging Face Transformers conversion. Model card identifies Apache-2.0. Exact model revision is recorded in `encoder_source.json`; pretrained weights are cached in `Encoder/`. [Upstream DINOv2](https://github.com/facebookresearch/dinov2), [Apache-2.0 license](https://github.com/facebookresearch/dinov2/blob/main/LICENSE). No remote Python model code is trusted or executed by the loader.
- Training method: frozen feature extractor with trained linear classification heads, consistent with the DINOv2 model card's downstream-classifier example and [PyTorch transfer-learning documentation](https://docs.pytorch.org/tutorials/beginner/transfer_learning_tutorial.html).
- Training image sources: individual photographer/credit/license records in [`../../References/ModelWear100/manifest.csv`](../../References/ModelWear100/manifest.csv). Images remain unchanged in their reference directory. Labels are local visual annotations, not labels supplied by the photographers or evidence of endorsement.
- Python runtime: CPython 3.12.10 Windows embeddable distribution from python.org. Runtime license files are retained in `.ml-runtime/python/`.
- Dependencies and installed versions are recorded in `requirements.lock.txt`. Their bundled licenses remain in `.ml-runtime/python/Lib/site-packages/`.

Original images, third-party encoder weights and package code retain their respective licenses. No images are sent to a training or inference service; model downloads are inbound only.
