import json
from pathlib import Path
import urllib.request

root=Path(__file__).resolve().parent
result=[]
for domain in ('sphere_simple','sphere_dynamic','flag_simple'):
    for file in ('meta.json','train.tfrecord','valid.tfrecord','test.tfrecord'):
        url=f'https://storage.googleapis.com/dm-meshgraphnets/{domain}/{file}'
        try:
            with urllib.request.urlopen(urllib.request.Request(url,method='HEAD'),timeout=30) as response:
                item=dict(domain=domain,file=file,url=url,size=int(response.headers.get('Content-Length',0)),checksum=response.headers.get('x-goog-hash'),etag=response.headers.get('ETag'))
            if file=='meta.json':
                with urllib.request.urlopen(url,timeout=30) as response:
                    item['metadata']=json.load(response)
            result.append(item)
            print(json.dumps({k:v for k,v in item.items() if k!='metadata'}),flush=True)
        except Exception as error:
            result.append(dict(domain=domain,file=file,url=url,error=str(error)))
(root/'meshgraph_inventory.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
