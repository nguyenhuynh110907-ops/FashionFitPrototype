"""Read audited material references without treating predictions as measurements."""
import io
import json
from pathlib import Path
import zipfile
from PIL import Image

DEFAULT_DATA=Path(__file__).resolve().parents[1]/'ClothData/Data/MaterialLab'
class MaterialReferences:
    def __init__(self,root=DEFAULT_DATA):
        self.root=Path(root)
        self.samples=[json.loads(line) for line in (self.root/'Prepared/material_samples.jsonl').read_text(encoding='utf-8').splitlines() if line.strip()]
        self.by_id={sample['id']:sample for sample in self.samples}
        if len(self.by_id)!=len(self.samples):raise ValueError('Duplicate material identity')

    def records(self,split):
        if split not in {'train','test'}:raise ValueError('Use the official train or test split')
        return [sample for sample in self.samples if sample['official_split']==split]

    def images(self,sample_id):
        """Return front/back PIL RGB images; open in the calling worker, no full extraction."""
        sample=self.by_id[sample_id];images={}
        for side,reference in sample['images'].items():
            path=(self.root/reference['archive']).resolve()
            if not path.is_relative_to(self.root.resolve()):raise ValueError('Archive outside data root')
            with zipfile.ZipFile(path) as archive:
                with Image.open(io.BytesIO(archive.read(reference['member']))) as image:images[side]=image.convert('RGB')
        return images

    def measured_training_records(self):
        """Only genuine approved train labels; never fall back to teacher predictions or test GT."""
        rows=[sample for sample in self.records('train') if sample['approved_supervised_training'] and sample['simulation_reference']]
        if not rows:raise ValueError('No approved measured training labels in this release. MechMet/MechIm are predictions; test GT stays held out. Collect measured train pairs before supervised training.')
        return rows
