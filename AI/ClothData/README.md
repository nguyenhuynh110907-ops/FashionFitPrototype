Dữ liệu texture và cơ học vải mới: [nguồn tải, kiểm tra nhãn và cách dùng](../MaterialLab/README.md). Giữ ngân sách dữ liệu dưới 30 GB.

Chuyển trang phục 3D có sẵn (FBX/GLB/OBJ): [công cụ, body mẫu và kết quả kiểm tra](../../Docs/GARMENT_3D_TRANSFER_GUIDE.md). Baseline hình học đã chạy; chưa phải mạng AI mới huấn luyện.

Bổ sung vật lý vải: [30 loại vải MIT/52 phép đo và tài liệu 10 chất liệu Berkeley](PHYSICS_COLLECTION_REPORT.md). Đã thử ba cấu hình trong Unity: [cách sử dụng](../../Docs/MATERIAL_PHYSICS_GUIDE.md), [kết quả và giới hạn](../../Verification/MaterialMotion/REPORT.md). Khối lượng lấy từ số đo; phần lớn tham số động lực học còn là hiệu chỉnh thử nghiệm, chưa train model mới.

Bản mới: [sửa nách/cổ/eo và bề mặt vải dệt](../../Verification/RealismPass/REPORT.md). Đây là chỉnh hình học/chất liệu trực tiếp, không phải huấn luyện AI mới.

Bản mới: [sửa mannequin và căn lại áo theo body mới](../../Verification/BodySilhouette/REPORT.md). Đây là xử lý hình học trực tiếp; không phải mô hình AI vừa học từ ảnh.

Sửa trực tiếp mẫu 06: [phom ngực áo và chi tiết bề mặt](../../Verification/PoloRoundness/REPORT.md). Thay đổi hình học offline, không phải huấn luyện model mới.

Bản thử chi tiết mới: [Gucci 864361 từ ảnh tham chiếu](../../Docs/GUCCI_DETAIL_TRIAL.md). Đây là dựng hình học và căn tiếp xúc, không phải model AI đã được train dựng đầm.

# ClothData · dữ liệu cho mô hình trang phục 3D và tiếp xúc

Đã tích hợp dùng thử vào Unity: [hướng dẫn sáu mẫu vải và AI hoa văn](../../Docs/FABRIC_TRIAL_GUIDE.md).

Chạy thử thực tế 28/10/2026: [Unity, 15 pose/body gốc, AI suy luận lại và các mục chưa đạt](../../Verification/TrialRun_20261028/REPORT.md).

Kiểm tra và hướng phát triển 28/10/2026: [phát hiện trong importer, hạn chế tập test và ma trận nghiệm thu](../../Verification/DETAIL_DEVELOPMENT_REVIEW.md).

Đợt bổ sung 28/10/2026: [90 bộ PBR, 1.002 ảnh hoa văn cho thử nghiệm, 200 mẫu 3D và kết quả hai mô hình](../GarmentDetail/DETAIL_TRAINING_REPORT.md). Ưu tiên giữ UV/vật liệu của file 3D sinh viên; mô hình 3D hiện chưa đủ chất lượng tự sửa cổ áo.


Chuẩn bị học chi tiết đầm: [pack nhãn cấu tạo, curve và đường ráp 3D](../GarmentDetail/SURFACE_DETAIL_DATA.md), [QA](../GarmentDetail/surface_detail_verification.json). Đã có 20 đầm xác nhận trong pilot; nút, khóa và thông số đường chỉ chưa có ground truth. Chạy `Prepare_Surface_Detail_Data.bat` ở thư mục project để tạo/kiểm tra lại từ dữ liệu cục bộ.

Đợt đầu đã thu thập dữ liệu trong mức **30 GB** được chọn. Dữ liệu nằm ở `AI/ClothData/Data/`, ngoài `Assets`; Unity không import hoặc chạy bộ dữ liệu này mỗi frame. Kết quả và số lượng đã kiểm tra: [COLLECTION_REPORT.md](COLLECTION_REPORT.md), [verification.json](verification.json).

Đây là dữ liệu cho các bài học mới. **Không chạy `Train_FitVision.bat` để học vật lý vải**: FitVision hiện chỉ nhận diện ảnh; bộ đọc 3D trong thư mục này dành cho mô hình hình học/graph/contact riêng.

Bước tiếp theo đã triển khai ở [GarmentDetail](../GarmentDetail/README.md): căn theo eo body, giữ panel/đường may/UV, chuyển đổi 24 graph và huấn luyện pilot 8 epoch. [Báo cáo](../GarmentDetail/TRAINING_PREPARATION_REPORT.md) nêu sai lệch còn lớn và phần tiếp xúc chưa tích hợp. Tổng dữ liệu hiện khoảng 27,77 GB; trọng số chưa thay bộ giải trong game.

## Ba phần dữ liệu

1. **Pattern → mesh mặc trên body:** GarmentCodeData v2, gồm pattern JSON, thiết kế và số đo YAML, mesh trước/sau mô phỏng, nhãn panel và điểm neo. Giữ nguyên Parquet để tránh nhân đôi hơn 10 GB dữ liệu khi giải nén.
2. **Chuỗi chuyển động vải:** MeshGraphNets `sphere_simple`, giữ nguyên train/validation/test. Đã kiểm tra cấu trúc và checksum. Phần này tạm loại khỏi loader huấn luyện mặc định vì README nguồn chưa xác định riêng điều khoản dataset; không lấy giấy phép code thay cho giấy phép dữ liệu.
3. **Mẫu khoảng cách/đẩy điểm khỏi body:** sinh từ một body 3D kín của GarmentCodeData. Có vị trí, signed clearance, pháp tuyến, vector hiệu chỉnh và khoảng cách sau hiệu chỉnh. Đây là bài tiếp xúc điểm trên một body tĩnh, chưa phải vải có tam giác, self-collision hay chuyển động người.

## Dùng bộ đọc

Chạy Python từ thư mục gốc project; thêm đường dẫn module:

```python
import sys
sys.path.insert(0, "AI/ClothData")
from datasets import GarmentPairs, ContactPoints, MotionArchiveReader

train = GarmentPairs("train")
sample = train[0]
# rest_positions_m, draped_positions_m, faces, pattern,
# body_measurements_source, panel_id_per_vertex, semantic_vertex_id_map

# Chỉ các mẫu có mesh body đi kèm, khi cần kiểm tra tiếp xúc:
with_body = GarmentPairs("train", require_body_mesh=True)

contacts = ContactPoints("train")
point = contacts[0]  # query_m, clearance_m, normal, correction_m, split...

# Chỉ xem dữ liệu chuyển động khi điều khoản dataset đang chờ xác nhận:
inspection = MotionArchiveReader("test", for_inspection=True)
trajectory = inspection[0]
world_positions = trajectory["arrays"]["world_pos"]
previous, current, target = world_positions[:-2], world_positions[1:-1], world_positions[2:]
```

Mesh có số đỉnh khác nhau, cần batching theo graph hoặc xử lý từng mesh; không `stack` tùy ý các mesh khác topology. Không trộn các frame của cùng trajectory sang hai tập. `mesh_pos` của dữ liệu chuyển động là tọa độ vật liệu 2D, khác `world_pos` 3D; giữ nguyên schema nguồn.

Pattern/mesh gốc khai báo `units_in_meter=100`; loader đổi tọa độ mesh thành mét. Số đo YAML còn nguyên đơn vị nguồn: các độ dài là centimet, nhưng trường góc và định danh body không phải độ dài, không nhân đồng loạt mọi giá trị với 0,01. Body tham chiếu OBJ đã ở đơn vị mét.

Nhãn panel dùng ID đỉnh nguyên thủy. Reader xử lý các đỉnh UV trùng liên tiếp đúng theo hướng dẫn upstream; chỉ cung cấp nhãn từng đỉnh khi mapping đã qua kiểm tra. Không tự weld mesh làm đổi ID rồi áp nhãn cũ.

## Tải tiếp hoặc chạy lại kiểm tra

Nhấp đúp `Collect_Cloth_Training_Data.bat` ở gốc project. Công cụ chỉ chạy lại **kế hoạch đợt đầu**, tiếp tục tệp `.part` nếu cần, giữ file đã xác minh và tạo chỉ mục còn thiếu. Không tự mở rộng quá hạn mức đã chọn.

Các lệnh riêng:

```powershell
.\.ml-runtime\python\python.exe AI\ClothData\collect_data.py
.\.ml-runtime\python\python.exe AI\ClothData\prepare_data.py
.\.ml-runtime\python\python.exe AI\ClothData\build_contact_data.py --points 200000
.\.ml-runtime\python\python.exe AI\ClothData\verify_data.py
# Sau khi di chuyển dữ liệu, có thể đọc và băm lại toàn bộ archive:
.\.ml-runtime\python\python.exe AI\ClothData\verify_data.py --full-raw
```

Máy mới cần Python cục bộ đã dùng ở `AI/FitVision/Setup-Environment.ps1`, rồi cài thư viện dữ liệu:

```powershell
.\.ml-runtime\python\python.exe -m pip install --only-binary=:all: -r AI\ClothData\requirements.txt
```

`preview_data.py` cần matplotlib, đã có trong môi trường FitVision. Mẫu SDF mặc định giới hạn khoảng 180 giây xử lý và ghi đúng số điểm thực tế tạo được, không tự nhận đã đủ số yêu cầu nếu máy dừng sớm.

## Tệp và khả năng tái lập

| Tệp | Vai trò |
| --- | --- |
| `source_inventory.json` | Revision cố định, kích thước và hash các file của bản repack |
| `download_plan.json` | 17 file được chọn, dung lượng và nguồn |
| `download_manifest.json` | Tệp tải xong, hash, thời gian và trạng thái xác minh |
| `collection_status.json` | Trạng thái tải gần nhất |
| `Data/Processed/garments.sqlite3` | Chỉ mục từng biến thể, nhóm thiết kế, split, pointer vào Parquet, QC và hash thành phần |
| `Data/Processed/motion_sequences.jsonl` | Offset từng trajectory để đọc lười từ TFRecord |
| `Data/Processed/Contact/neutral_body_contact.npz` | Nhãn tiếp xúc điểm tổng hợp |
| `Data/Previews/static_drapes.png` | Ảnh đối chiếu mesh nguồn trước/bên |
| `sources.json` | Tác giả, nguồn, điều khoản và các phần chưa lấy được |

Ảnh 2D cũ vẫn ở `References/ModelWear100`. Chưa có ground truth ảnh chụp người thật ghép chính xác với các mesh 3D mới; không tự ghép ảnh và mesh chỉ vì trông giống nhau.

## Trước khi huấn luyện mô hình mới

Có thể bắt đầu bài **pattern/số đo → hình học drape** và **point/body → clearance/vector hiệu chỉnh** bằng các phần đã chuẩn bị. Cần thêm body có nhiều hình dạng/tư thế và clip người mặc đồng bộ để học chuyển động trang phục trên người. Bộ `random_body` hiện có số đo và mesh trang phục nhưng bản repack không chứa mesh từng body ngẫu nhiên; chỉ các mẫu body mặc định có collider body đầy đủ trong đợt này.

QA cấu trúc không chứng minh toàn bộ mặt vải không xuyên body. Cần bổ sung kiểm tra body–triangle, cloth–cloth và chuyển động liên tục trước khi dùng các mẫu làm chuẩn không xuyên hoặc đưa mô hình vào game.





## Bổ sung nghiên cứu áo dài - 30/10/2026

Đã bổ sung gói tham chiếu ~104.82 MB: 6 ảnh gốc, 4 PDF (80 trang) và trích 6 mẫu rập tổng quát từ dữ liệu đã có. Tổng thư mục Data hiện 29.86015 GB / 30 GB. Chưa có mesh/rập số áo dài mới hoặc mô hình áo dài đã train. Xem [báo cáo áo dài](../../Verification/AoDaiResearch/REPORT.md), [hồ sơ dữ liệu](../AoDaiResearch/README.md) và [trang ảnh đã tải](Data/AoDaiResearch/BROWSE.html).
