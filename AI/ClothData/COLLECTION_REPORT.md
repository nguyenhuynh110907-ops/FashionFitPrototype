# Kết quả thu thập dữ liệu 3D và tiếp xúc · đợt 1

Hoàn thành và kiểm tra ngày **27/10/2026**, theo mức tối đa **30 GB** đã chọn. Dữ liệu được tải trực tiếp vào project, có chỉ mục và bộ đọc cục bộ. **Đợt này chuẩn bị dữ liệu; chưa huấn luyện mô hình dựng đồ 3D hoặc mô hình chuyển động mới.**

Cập nhật sau đợt thu thập: [GarmentDetail](../GarmentDetail/TRAINING_PREPARATION_REPORT.md) đã chạy thử mô hình graph drape tĩnh trên 16 mẫu train và 4 validation, giữ cấu trúc rập/đường may/UV. Đây là pilot riêng, chưa phải mô hình dựng đồ từ ảnh hoặc mô hình chống xuyên vải; tổng dữ liệu kể cả cache mới khoảng 27,77 GB.

## Số lượng thực tế đã có trên máy

| Phần | Kết quả đã đọc và kiểm tra |
| --- | --- |
| Dung lượng dữ liệu thô | **27.655.459.557 byte**, khoảng **27,66 GB** |
| Tổng dữ liệu gồm chỉ mục, mẫu tiếp xúc và preview | Khoảng **27,71 GB**, dưới mức 30 GB |
| File tải thành công | **17/17**, có receipt và checksum |
| Biến thể trang phục có mesh/pattern | **10.594** |
| Thiết kế khác nhau trong các biến thể đó | **7.641** |
| Thành phần pattern/mesh/YAML/nhãn đã kiểm tra hash | **84.752** thành phần, được giữ trong Parquet |
| Biến thể vào bộ đọc huấn luyện mặc định | **9.256** |
| Chuỗi chuyển động vải/vật cản | **1.200**, gồm **601.200 frame** |
| Cặp bước thời gian có lịch sử trước–hiện tại–sau | **598.800**, chưa bật cho training mặc định |
| Mẫu point/body contact được chấp nhận | **196.558** |

Một thiết kế có thể xuất hiện ở body mặc định và body ngẫu nhiên. Một chuỗi có nhiều frame liên tiếp tương quan với nhau. Các con số biến thể, thiết kế, frame và điểm tiếp xúc là các đơn vị khác nhau, không cộng thành một số “bộ đồ” chung.

## 1. Pattern và hình học mặc trên body

Nguồn gốc là [GarmentCodeData v2, Korosteleva và cộng sự](https://igl.ethz.ch/projects/GarmentCodeData/), [DOI](https://doi.org/10.3929/ethz-b-000690432). Lần này tải từ [bản đóng gói Parquet công khai của chibifire](https://huggingface.co/datasets/chibifire/datasource-garmentcodedata-v2-parquet), revision cố định `0e900868c0f7728b0522a13f917203082e8395d4`. Đây là một phần của bản repack, không phải toàn bộ archive gốc ETH. Điều khoản dữ liệu được ghi là **CC BY 4.0**; giữ nguồn, tác giả và citation trong metadata tải về.

Ba shard đã lấy:

- `default_body/batch-00.parquet` — 3.523.351.314 byte.
- `random_body/batch-00.parquet` — 3.367.650.361 byte.
- `default_body/batch-14.parquet` — 4.033.138.880 byte.

Mỗi biến thể có 8 thành phần: thiết kế YAML, số đo YAML, pattern JSON, pattern SVG, mesh ban đầu PLY, mesh drape PLY, nhãn panel và nhãn điểm neo. Đã đối chiếu SHA256 từng thành phần với cột hash của bản repack. Không giải nén toàn bộ thành hàng chục nghìn file để tránh nhân đôi dung lượng.

Trong mẫu đã lấy có 7.287 biến thể trên body mặc định và 3.307 biến thể trên body ngẫu nhiên. Các chương trình thiết kế được ghi trong dữ liệu gồm áo vừa/rộng, quần, váy bút chì, váy tròn, nhiều tầng, nhiều panel, godet và dạng bất đối xứng. Hai mức bending được bao phủ là `b=1` và `b=500` theo thông số upstream; không tự diễn giải chúng thành tên chất liệu hoặc đơn vị SI đã hiệu chuẩn.

### Chia tập và dữ liệu được dùng

Giữ cách chia chính thức theo **thiết kế**, kế thừa cùng split cho biến thể trên body ngẫu nhiên nếu nguồn liệt kê đường dẫn body mặc định. Không chia riêng từng mesh/frame ngẫu nhiên.

| Split của bộ đọc mặc định | Biến thể đạt điều kiện |
| --- | ---: |
| Train | 8.225 |
| Validation | 525 |
| Test | 506 |
| **Tổng** | **9.256** |

Không phát hiện thiết kế xuất hiện ở nhiều split. Có **1.337** biến thể không nằm trong danh sách thiết kế của split chính thức, được lưu trong chỉ mục với `unassigned`, không tự đưa vào train. Một mẫu có tam giác suy biến ở mesh ban đầu bị loại: `gcd_b00_random_rand_EUQVO5PTST` (1 mặt suy biến ở box mesh, 0 ở mesh drape). Vì vậy tổng mẫu ngoài bộ đọc mặc định là 1.338.

Trong 9.256 mẫu mặc định có 6.304 biến thể body chuẩn và 2.952 biến thể body ngẫu nhiên. **Bản repack không có mesh của 5.000 body ngẫu nhiên**; các biến thể đó có số đo và mesh trang phục, nhưng chưa đủ để kiểm tra collider body tương ứng. Loader có tùy chọn `require_body_mesh=True` để chỉ lấy mẫu có body mesh đi kèm.

### Các kiểm tra hình học đã chạy

- Tọa độ hữu hạn, chỉ số tam giác hợp lệ, topology khớp giữa mesh ban đầu và mesh drape.
- Đọc `units_in_meter=100` từ pattern; đổi tọa độ trang phục về mét. Body OBJ tham chiếu đã ở mét.
- Phát hiện tam giác suy biến bằng diện tích; lưu riêng trạng thái QC và loại khỏi loader training.
- Xác minh mapping đỉnh trùng do UV bằng dữ liệu nhãn. Cả 10.594 biến thể cần và vượt qua phép map đỉnh trùng liên tiếp; không weld tùy ý rồi gắn nhãn cũ.
- Giữ nguyên số đo, thông số mô phỏng, nguồn và hash; số đo chứa cả trường góc/định danh, không đổi mọi giá trị bằng một hệ số chung.

**Chưa chạy kiểm tra giao cắt body–tam giác hoặc cloth–cloth đầy đủ cho toàn bộ mẫu.** `pattern_drape_training_eligible` nghĩa là đủ điều kiện cấu trúc, nguồn và split cho bài pattern/drape, không phải chứng nhận không xuyên.

![Ví dụ mesh drape và body chuẩn ở góc trước/bên](Data/Previews/static_drapes.png)

Ảnh trên được dựng từ mesh nguồn của GarmentCodeData, có ghi công ở mục nguồn; dùng kiểm tra trực quan, không phải ảnh chụp người thật hoặc chứng minh va chạm đúng.

## 2. Chuỗi chuyển động vải/vật cản

Lấy `sphere_simple` từ [kho dữ liệu và script tải chính thức MeshGraphNets](https://github.com/google-deepmind/deepmind-research/tree/master/meshgraphnets), của Pfaff và cộng sự/DeepMind. Đây là dữ liệu vải tương tác vật cản, **không phải clip người mẫu mặc đầm đi catwalk**.

| Split upstream | Chuỗi | Frame |
| --- | ---: | ---: |
| Train | 1.000 | 501.000 |
| Validation | 100 | 50.100 |
| Test | 100 | 50.100 |

Mỗi chuỗi đã đọc có 501 trạng thái, 1.731 đỉnh và 3.328 mặt; các trường gồm `cells`, `mesh_pos`, `node_type`, `world_pos`; metadata có `dt=0.01`. Có thể dựng mục tiêu vị trí bước tiếp theo bằng cách lấy lịch sử trước–hiện tại–sau trong cùng chuỗi. Không lấy frame ở một chuỗi sang các split khác.

Đã kiểm tra CRC32C của file theo metadata GCS, CRC header và payload của **mọi TFRecord**, schema, shape, tọa độ hữu hạn và chỉ số mặt. Bộ đọc không cần cài TensorFlow để đọc dữ liệu này.

**Trạng thái sử dụng:** code repository là Apache-2.0, nhưng README đã đọc chưa quy định riêng rõ ràng cho dataset nằm trên bucket. Phần raw và chỉ mục được giữ để nghiên cứu cấu trúc; loader huấn luyện mặc định từ chối phần này, chỉ cho đọc với `for_inspection=True`. Chưa dùng nó để huấn luyện hoặc gộp vào tập training sẵn sàng. Cần xác nhận điều khoản dataset trước bước đó.

## 3. Nhãn khoảng cách và đẩy điểm ra khỏi body

Dùng body chuẩn GarmentCodeData, 23.752 đỉnh/47.500 mặt, đã kiểm tra kín, hướng mặt nhất quán và thể tích dương. SHA256: `4f87d26bba81778c7ed492de9145483750a2dbb64b9fa71fda6de1fac8ef51a8`.

Lấy 200.000 điểm quanh bề mặt, lệch khoảng −4 đến +6 cm theo pháp tuyến, rồi tính khoảng cách có dấu và vị trí hiệu chỉnh ra ngoài với margin 3 mm. **3.442 điểm bị loại** khi không qua kiểm tra sau hiệu chỉnh; còn **196.558** điểm:

| Split theo ô bề mặt 15 cm | Điểm |
| --- | ---: |
| Train | 157.483 |
| Validation | 22.911 |
| Test | 16.164 |

Các ô bề mặt không giao nhau giữa ba tập. Có 75.731 điểm đầu vào nằm trong body trong số mẫu được chấp nhận, nên có cả tình huống cần hiệu chỉnh và điểm đã ở ngoài.

Đã tính lại khoảng cách sau hiệu chỉnh cho mọi điểm được chấp nhận; giá trị nhỏ nhất ghi nhận khoảng 2,990 mm với dung sai 10 micromet. Bộ xác minh độc lập lấy lại 512 điểm, tính từ tọa độ/vector float32 đã lưu, vẫn qua điều kiện ngoài body. Đây là nhãn hình học được tạo từ mesh, không phải kết quả dự đoán của AI.

**Giới hạn:** chỉ một body tĩnh, chưa có body mới để kiểm tra khả năng tổng quát hóa; không có vải dạng tam giác, vật liệu, ma sát, self-collision hoặc CCD. Phép đẩy điểm không bảo đảm cạnh/tam giác nối các điểm cũng ở ngoài body.

## Khả năng dùng ngay và phần còn thiếu

Đã chuẩn bị được hai bài training có nhãn: **pattern/số đo → mesh drape** và **point/body → clearance/vector hiệu chỉnh**. Bộ đọc lazy ở `datasets.py` đã được thử trên train/validation/test; dữ liệu được nạp theo mesh hoặc chuỗi, không đưa hàng chục GB vào RAM cùng lúc.

Để học chuyển động quần áo trên người, vẫn cần chuỗi pose/body/garment đồng bộ, nhiều body và thông số vật liệu đáng tin. [4D-DRESS](https://4d-dress.ait.ethz.ch/) yêu cầu đăng ký bằng email tổ chức và điều khoản học thuật phi thương mại; CLOTH3D cũng cần truy cập dataset. Không tạo tài khoản, chấp nhận thỏa thuận hay tính những nguồn chưa tải vào kết quả đợt này. Các mục chờ được ghi trong `sources.json`.

Không ghép tùy ý 100 ảnh runway cũ với mesh mới để tạo cặp ảnh–3D giả. Bộ dữ liệu hiện cũng chưa biến mô phỏng Unity thành hệ thống bảo đảm không xuyên.

## Tệp bàn giao và xác minh

- [README.md](README.md): dùng loader, tiếp tục tải và thiết lập trên máy mới.
- [verification.json](verification.json): xác minh hoàn tất, giới hạn dung lượng, split, reader và contact.
- [dataset_summary.json](dataset_summary.json): thống kê chương trình thiết kế, vật liệu và trường hợp bị loại.
- [download_manifest.json](download_manifest.json): 17 tệp đã tải, checksum, nguồn và thời gian.
- [sources.json](sources.json): tác giả, provenance, điều khoản và phần còn thiếu.
- `Data/Processed/garments.sqlite3`, `motion_sequences.jsonl`, `Contact/neutral_body_contact.npz`: chỉ mục và nhãn để dùng tiếp.

Checksum archive đã được xác minh khi tải, SHA256 từng thành phần được kiểm tra khi tạo chỉ mục. Kiểm tra cuối không đọc/băm lại toàn bộ 27,66 GB lần thứ hai; có thể yêu cầu bằng `verify_data.py --full-raw` khi chuyển dữ liệu hoặc nghi ngờ file bị thay đổi.


## Bổ sung nghiên cứu áo dài - 30/10/2026

Đã bổ sung gói tham chiếu ~104.82 MB: 6 ảnh gốc, 4 PDF (80 trang) và trích 6 mẫu rập tổng quát từ dữ liệu đã có. Tổng thư mục Data hiện 29.86015 GB / 30 GB. Chưa có mesh/rập số áo dài mới hoặc mô hình áo dài đã train. Xem [báo cáo áo dài](../../Verification/AoDaiResearch/REPORT.md), [hồ sơ dữ liệu](../AoDaiResearch/README.md) và [trang ảnh đã tải](Data/AoDaiResearch/BROWSE.html).
