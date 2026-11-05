# Dữ liệu học cấu tạo và vị trí chi tiết trên đầm

Đã chuẩn bị một pack riêng từ dữ liệu đang có, ngoài `Assets`. Pack này giúp học vùng cổ/nách, biên và đường ráp trước khi thêm chi tiết nhỏ. Nó **chưa phải bộ nhãn đầy đủ cho nút, khóa, đường chỉ hay thêu** và chưa được đưa vào runtime.

## Dữ liệu đã tạo

| Nội dung | Số lượng / trạng thái |
|---|---|
| Graph được chuyển thành nhãn chi tiết | 24 |
| Đầm xác nhận bằng liên kết may giữa thân trên và váy | 20: train 12, validation 4, test 4 |
| Mẫu khác giữ để đối chiếu | 4 |
| Cạnh rập 2D | 2.414: thẳng 1.848, cubic 110, quadratic 248, cung tròn 208 |
| Cạnh đường ráp trong mesh 3D | 18.142 |
| Ứng viên có thân trên + chương trình váy để mở rộng | 2.310; chưa kiểm tra liên kết may/chuyển đổi tất cả |
| Ảnh có sẵn đưa vào hàng đợi gán nhãn chi tiết | 100; giữ nguồn, tác giả và giấy phép |
| Cặp ảnh–rập–mesh đúng danh tính | 0 |
| Tổng ClothData sau chuẩn bị | 27.785.438.191 byte, khoảng 27,79 GB / mức 30 GB |

Đầm được xác nhận khi có một thành phần liên thông trong đồ thị đường may chứa cả panel thân trên và panel váy. Không chỉ thấy một áo và một váy là tự nhận thành đầm liền. Vai trò panel được suy ra từ tên/nhãn nguồn, chưa phải bộ nhãn thiết kế do thợ may kiểm duyệt.

## Đầu vào và nhãn

`AI/ClothData/Data/DetailTraining/SurfaceDetails/` chứa manifest, cache NPZ, metadata JSON, danh sách ứng viên và hàng đợi ảnh.

- **Đầu vào cho bài học vùng chi tiết:** 11 giá trị/đỉnh, gồm tọa độ/pháp tuyến trước drape và số đo/tham số bending nguồn. Không đưa bit đường may, nhãn cổ/nách, vai trò panel hay vị trí target vào đầu vào.
- **Nhãn đỉnh:** điểm đường ráp, biên hở, neckline từ nhãn collar, armhole, waist, `lower_interface` và mép strapless. Mỗi nhãn có mask hợp lệ riêng. Không có nhãn nguồn nghĩa là chưa biết, không tự coi là âm tính.
- **Nhãn cạnh:** cạnh may, biên hở và may hai cạnh thuộc cùng một mảnh. May cùng mảnh có thể là chiết hoặc nếp; không tự đặt nhãn tất cả thành chiết eo.
- **Vị trí 3D:** mesh tham chiếu và tập cạnh đường ráp theo mét. Tập cạnh có thể có nút nhánh; chưa ép thành một đường liên tục sai cấu trúc.
- **Rập:** giữ endpoint và tham số đường cong gốc, kèm tham chiếu rập đầy đủ có rotation/translation và cờ hướng may. Không xấp xỉ cung tròn thành đoạn thẳng. Endpoint dùng cm nguồn, tham số cong giữ đúng quy ước nguồn.
- **UV:** giữ trong graph gốc được tham chiếu cùng SHA-256; không lấy trung bình qua các đường UV nối khác panel.

`lower_interface` không mặc định là gấu hoặc eo tự nhiên; biên hở cũng có thể là cổ, nách, tay hoặc đường xẻ. Cần nhãn cụ thể trước khi học gấu may.

## Chi tiết nhỏ chưa đủ nhãn

Metadata ghi `value=null, valid=false` cho bước chỉ, bán kính chỉ, seam allowance, bề rộng gấu, độ dày vải, lớp lót, nút 3D, khóa 3D, túi và UV thêu. Các trường này chưa được dùng làm target. Không suy ra kích thước milimet từ ảnh runway thiếu thước đo.

Hàng đợi `photo_detail_annotation_queue.json` giữ ảnh gốc tại `References/ModelWear100`; script không ghi đè các annotation đã có. Mỗi vùng ảnh cần tọa độ chuẩn hóa [0,1], loại chi tiết và trạng thái thấy rõ/bị che/chưa biết. Cần thêm nhóm danh tính bộ đồ, ảnh trước–sau–bên/cận cảnh và mesh đúng bộ đồ trước khi bật huấn luyện hình học từ ảnh. 100 ảnh hiện vẫn bị tắt cho bài ảnh → 3D.

## Chạy lại và đọc dữ liệu

Nhấp **Prepare_Surface_Detail_Data.bat** tại thư mục project. Script dùng dữ liệu cục bộ, không tải thêm, không thay checkpoint drape và không triển khai AI vào game.

```python
import sys
sys.path.insert(0, "AI/GarmentDetail")
from surface_detail_data import SurfaceDetailDataset, masked_region_loss

train = SurfaceDetailDataset("train")  # mặc định chỉ 12 đầm đã xác nhận
sample = train[0]
x = sample["arrays"]["point_features"]
labels = sample["arrays"]["vertex_targets"]
valid = sample["arrays"]["vertex_target_valid"]
```

Graph có số đỉnh khác nhau, nên đọc từng graph hoặc batch có offset chỉ số cạnh; không stack trực tiếp các mesh khác kích thước. Loss mẫu `masked_region_loss` chỉ tính các nhãn hợp lệ.

## Kiểm tra và hướng huấn luyện

[surface_detail_verification.json](surface_detail_verification.json) ghi kiểm tra hash, nguồn nhãn, curve, tọa độ, split và 100 file ảnh. Đã chạy **hai bước optimizer thử trên train** với gradient hữu hạn và xác nhận nhãn chưa biết không sinh gradient. Không lưu checkpoint chi tiết, không chạy optimizer trên test và không công bố độ chính xác từ phép thử này.

Thứ tự tiếp theo: học phân vùng/định vị đường cấu tạo → kiểm tra trên đầm chưa thấy → thêm nhãn 3D/UV cho chi tiết may thực → học phần hình học/vật liệu tương ứng → retarget và kiểm tra tiếp xúc trên mannequin. Cần mở rộng dữ liệu và kiểm duyệt nhãn trước huấn luyện đầy đủ; 20 đầm là pilot.
