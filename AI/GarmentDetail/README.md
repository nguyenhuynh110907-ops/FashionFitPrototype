Chuyển trang phục 3D có sẵn (FBX/GLB/OBJ): [công cụ, body mẫu và kết quả kiểm tra](../../Docs/GARMENT_3D_TRANSFER_GUIDE.md). Baseline hình học đã chạy; chưa phải mạng AI mới huấn luyện.

# Học chi tiết trang phục theo rập và đường may

Cập nhật 28/10/2026: [dữ liệu và kết quả huấn luyện vùng cổ áo/hoa văn](DETAIL_TRAINING_REPORT.md), cùng công cụ kiểm tra OBJ/MTL/GLB của sinh viên. Hai mô hình chạy offline; chưa thay runtime Unity.


Pack nhãn cấu tạo mới: [SURFACE_DETAIL_DATA.md](SURFACE_DETAIL_DATA.md). Đã chuẩn bị 24 graph, gồm 20 đầm may nối thân trên–váy, cạnh rập cong và đường ráp 3D; có bộ đọc và loss bỏ qua nhãn chưa biết. Các chi tiết nút/khóa/gấu/đường chỉ chưa có nhãn đầy đủ, chưa áp vào game.

Phần này chuẩn bị bài học **mesh trước drape + cấu trúc may + số đo → mesh mặc trên body tham chiếu**. Mỗi bộ đồ giữ các mảnh thân trước/sau, tay, cổ, cạp… thực sự có trong nguồn, cùng đường may và UV. Đã chạy một mô hình graph thử nhỏ; chưa thay trang phục trong Game hoặc đưa trọng số vào bộ giải VR.

Xem [báo cáo kết quả và giới hạn](TRAINING_PREPARATION_REPORT.md), [kiểm tra máy đọc được](verification.json) và [các giai đoạn tiếp theo](training_plan.json).

## Căn giữa nhưng giữ đúng phom

1. Đổi trang phục từ cm sang mét; body gốc đã dùng mét.
2. Lấy tâm tiết diện eo của **body**, xác định hướng lên +Y và trước +Z của nguồn. Đưa cả mesh trước drape và mesh tham chiếu vào cùng hệ này.
3. Chuẩn hóa bằng một tỉ lệ chiều cao đồng đều. Không co riêng từng trục; không kéo áo về tâm hộp bao hoặc ép váy bất đối xứng thành đối xứng. Tâm diện tích bộ đồ chỉ được ghi để quan sát.
4. Giữ nguyên vị trí tương ứng của từng đỉnh giữa hai trạng thái. Học sai lệch đỉnh và hình học cạnh, đường may, pháp tuyến; các nhãn cổ/eo có thêm trọng số khớp mẫu. Đây là giám sát theo mẫu, không phải ghim mọi đỉnh đó vào cơ thể.
5. Dùng các mảnh rập, nhãn và UV để đi vào chi tiết. Các đỉnh trùng ở đường UV được liên kết ở graph hình học; bản dựng hiển thị vẫn giữ UV riêng, không gộp mất ranh giới texture.

Nguồn đã mặc lên body mặc định tương ứng. Đưa bộ đồ sang mannequin khác còn cần đối chiếu cổ, vai, eo, hông và cỡ đồ, kiểm tra tiếp xúc rồi mới chấp nhận. Phép dịch tâm/scale đơn thuần chưa giải quyết được bước này.

## Xem trong Unity

Mở dự án hiện tại, chờ Unity compile, chọn **Fashion Fit > AI > Inspect Detailed Garment**. Không cần Play Mode.

- Chuyển **Mặc theo mẫu 3D / Mesh trước drape** để đối chiếu hai trạng thái cùng topology.
- Chọn một panel để tách riêng thân áo, tay hoặc mảnh váy; khung nhìn tự phóng vừa mảnh. Tắt **Body nguồn** nếu mảnh phía sau bị che. Nút **Phóng vừa mảnh / toàn bộ đồ** căn lại camera khi cần.
- Bật màu panel, đường may hoặc nhãn neo để kiểm tra cấu trúc. Nhãn neo hiển thị tối đa 256 dấu để dễ nhìn; dữ liệu huấn luyện giữ đầy đủ.
- Kéo chuột trái để xoay, lăn chuột để zoom. Dịch/quay/tỉ lệ chỉ thay bản xem; nút căn theo eo trả về phép căn gốc.

Mẫu xem là bộ áo + váy mô phỏng nguồn `gcd_b00_default_rand_R2D7JIR0NJ`, có **10 panel, 29.183 đỉnh hiển thị**. Màu dùng để phân biệt mảnh, chưa phải chất liệu thời trang hoàn chỉnh. Đây là **mesh tham chiếu**, không phải kết quả AI dự đoán.

## Chuẩn bị và chạy lại

Tại thư mục `FashionFitPrototype`:

1. `Prepare_Detailed_Garment_Training.bat`: tạo lại manifest ứng viên, 24 graph pilot và mẫu xem Unity từ dữ liệu đã có. Không tải thêm và không chạy huấn luyện toàn bộ.
2. `Train_Detail_Drape.bat`: chạy lại pilot 8 epoch, lưu checkpoint rồi kiểm tra nguồn, cấu trúc, alignment và suy luận validation. Ghi đè checkpoint pilot cùng tên.

Nếu dữ liệu không đổi, có thể bỏ bước 1. Môi trường `.ml-runtime` và ClothData đã được cài trong bản dự án hiện tại; các phiên bản phụ thuộc được ghi trong [requirements.txt](requirements.txt). Giữ `Data` ngoài `Assets` để Unity không import hàng chục GB.

Các lệnh tương ứng trong PowerShell:

```powershell
$env:PYTHONUTF8 = '1'
& .\.ml-runtime\python\python.exe AI\GarmentDetail\prepare_training.py
& .\.ml-runtime\python\python.exe AI\GarmentDetail\train_drape.py --dataset pilot --epochs 8
& .\.ml-runtime\python\python.exe AI\GarmentDetail\verify_preparation.py
```

`--smoke` chỉ kiểm tra hai bước optimizer. Chế độ `--dataset full` đọc graph theo nhu cầu từ 6.304 ứng viên; chưa chạy và chưa xác nhận mọi ứng viên qua chuyển đổi. Huấn luyện đầy đủ sẽ lâu hơn nhiều trên CPU, cần đánh giá mô hình và cách xử lý mẫu lỗi trước khi dùng như kết quả hoàn chỉnh.

## Dữ liệu và giới hạn

Manifest có **5.611 train / 352 validation / 341 test** từ các mẫu body mặc định có mesh cơ thể thật sự đi kèm. Pilot có **16 / 4 / 4**; test chỉ kiểm tra cấu trúc, không dùng chọn trọng số hoặc tính điểm mô hình. Các nhóm thiết kế theo official split được giữ tách biệt.

Không dùng các mẫu random body cho giai đoạn này vì bản repack thiếu mesh từng cơ thể. 100 ảnh đã thu thập chưa có cặp rập–mesh đúng danh tính nên không được giả làm nhãn 3D.

Mô hình hiện học drape **tĩnh**, không học bước đi. Sai lệch còn lớn, chưa có loss tiếp xúc body, self-collision hoặc kiểm tra chuyển động liên tục. Chưa có kết luận chống xuyên vải. Nút áo, khóa kéo, lót, độ dày mép may, họa tiết và vật liệu đo thực cần thêm asset/nhãn thích hợp; nhiều đỉnh không tự tạo ra các chi tiết đó.

Nguồn: [GarmentCodeData](https://igl.ethz.ch/projects/GarmentCodeData/), [GarmentCode](https://github.com/maria-korosteleva/GarmentCode), [bản repack đã thu thập](https://huggingface.co/datasets/chibifire/datasource-garmentcodedata-v2-parquet). Thông tin giấy phép, revision và kiểm chứng nguồn được lưu trong [ClothData](../ClothData/README.md). Viewer dùng API [PreviewRenderUtility của Unity](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/PreviewRenderUtility.cs).
