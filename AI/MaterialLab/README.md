Thư viện texture tương tác: [12 mẫu màu/normal/roughness](FabricViewer.html). Sáu mẫu dùng chung file với menu VR.

# Dữ liệu bề mặt và phản ứng cơ học của vải

Đã thu thập và kiểm tra dữ liệu trong `AI/ClothData/Data/MaterialLab/`, ngoài thư mục Unity `Assets`. Mở [trang xem texture và đường lực](index.html) hoặc [báo cáo chi tiết](../../Verification/MaterialReferences/REPORT.md).

Texture mô tả bề mặt nhìn thấy. Độ cứng kéo/uốn, độ hồi phục và độ bền đứt/rách là những đại lượng khác; không suy ra số đo sức bền thật chỉ bằng tên vải, màu hoặc normal map.

## Dữ liệu hiện có

| Nguồn | Nội dung đã xác minh | Cách dùng |
|---|---|---|
| ambientCG | 6 bộ PBR 1K-PNG, 40 map, CC0 | Màu/normal/roughness/height và các map thực có trong gói; chưa ghép với mẫu đo cơ học |
| Eurographics 2024 | 1.275 dòng train + 194 dòng test, 2.938 ảnh trước/sau hợp lệ | Học cấu trúc/metadata; tách dự đoán khỏi nhãn GT; giữ nguyên official split |
| Poincloux và cộng sự | 78.256 dòng lực–kích thước theo thời gian, 2 điều kiện biên của một hệ vải dệt kim, CC BY 4.0 | Đối chiếu phản ứng kéo phi tuyến và vòng tải–thả, không phải 78.256 chất liệu |
| MIT đã thu thập trước | 30 loại vải, khối lượng bề mặt/độ dày/uốn | Dùng lại dữ liệu có sẵn, không tải trùng video nhiều GB |

Giữ ảnh Eurographics trong ZIP 673.630.805 byte và đọc theo từng mẫu bằng `material_reference.py`; không giải nén thêm toàn bộ. Tổng dữ liệu sau chuẩn bị khoảng **29,76 GB / 30 GB**, còn khoảng 244 MB theo đơn vị thập phân. Không mở rộng đợt thu thập này vượt ngân sách đã chọn.

## Tránh huấn luyện nhầm nhãn

Trong CSV Eurographics, các trường `*_MechMet`, `*_MechIm`, `pred_*` là đầu ra mô hình; `nmae_*` là sai số đánh giá. Chúng không tự trở thành số đo của vải. ZIP có 195 cảnh `GT`, trong đó 194 tương ứng bộ test và một ID ngoài hai CSV; bộ train không có GT tương ứng trong bản phát hành này.

`Prepared/material_samples.jsonl` giữ ảnh, metadata, prediction và GT thành các trường riêng; `approved_supervised_training` hiện là `false`. Hàm `measured_training_records()` sẽ dừng khi thiếu nhãn train được xác nhận. Không chuyển GT từ test sang train. Nếu chọn distillation về sau, phải ghi rõ teacher/pseudo-label; không tuyên bố độ chính xác vật lý tốt hơn nhãn nguồn.

Số 1.565 là mô tả tập nghiên cứu trong bài báo; CSV trong ZIP đã tải có **1.469 bản ghi**, không tự bổ sung cho đủ số. Không thấy ảnh trùng byte giữa hai split trong các cặp được tham chiếu; điều đó không chứng nhận không có hai swatch gần giống nhau.

## Đơn vị và sức bền

Hai file `Prepared/*_force_SI.csv` có thời gian giây, **tổng chiều cao mẫu** mét và lực Newton. Header nguồn ghi `extension`, nhưng README của tác giả nói đó là tổng kích thước theo chiều dọc; vì vậy không diễn giải nó thành độ giãn hay strain. Giữ các lực âm nhỏ của nguồn, không xóa offset để làm đẹp đường đo.

Chưa có nhãn lực đứt, độ giãn tại đứt, lực xé, độ bền đường may hoặc chu kỳ mài mòn ghép đúng mẫu. Các trường đó để `null`. Lực cực đại trong một cửa sổ thử không mặc nhiên là lực phá hủy. Tham số StVK/hinge trong cảnh GT và hệ số solver chưa được xác nhận tương đương với compliance XPBD trong Unity; không chép trực tiếp các số đó vào profile game.

## Hướng phát triển tiếp

1. Với từng vải thật của sinh viên, ghi mã mẫu, cấu trúc dệt/đan, thành phần, khối lượng bề mặt, độ dày, hướng dọc/ngang/chéo và kích thước mẫu thử. Chụp texture đúng mẫu đó, có thước và điều kiện chiếu sáng được ghi lại.
2. Thu đường lực–độ giãn tải/thả và thử uốn, cùng điều kiện kẹp, tốc độ, tiền tải và lặp lại. Muốn mô phỏng hư hỏng cần thêm thí nghiệm đến đứt/xé và độ bền mối may.
3. Dựng swatch ảo cùng kích thước, khối lượng, hướng sợi và điều kiện biên. Hiệu chỉnh tham số solver cho đường lực và drape; kiểm tra khi đổi bước thời gian và mật độ lưới. Chỉ chuyển sang profile runtime khi có đối chiếu này.
4. Ghép profile vật lý với đúng asset 3D và UV của bộ đồ; giữ texture độc lập với tham số cơ học. Kiểm tra đứng yên, kéo/thả, va chạm rồi mới tăng tốc trình diễn.

Chưa huấn luyện model mới và chưa sửa profile vật lý runtime trong đợt này. [Báo cáo](../../Verification/MaterialReferences/REPORT.md) có checksum, phép kiểm tra và giới hạn.

## Chạy lại

Trong thư mục dự án, dùng môi trường Python đã có:

```powershell
& .\.ml-runtime\python\python.exe AI\MaterialLab\collect_material_data.py --project .
& .\.ml-runtime\python\python.exe AI\MaterialLab\prepare_material_data.py --project .
```

Collector kiểm tra MD5 khi nguồn cung cấp, kích thước, SHA-256 và ngân sách. Script chuẩn bị đọc nhãn/ảnh, tạo manifest, CSV SI, đồ thị và audit; không train hoặc ghi `cloth-materials.json`. Dùng numpy, Pillow, matplotlib đã cài trong `.ml-runtime`; không cần đưa Python vào Unity.

## Nguồn và phạm vi sử dụng

- [ambientCG và giấy phép CC0](https://docs.ambientcg.com/license/): texture có thể dùng trong game. Bốn gói dùng phương pháp multi-angle, hai gói procedural theo metadata nguồn; không gọi cả sáu là mẫu vải vật lý đã đo.
- [Practical Methods to Estimate Fabric Mechanics from Metadata](https://henardoel.github.io/henardoel/index_eg2024.html), Domínguez-Elvira và cộng sự, Eurographics 2024: dữ liệu dành cho nghiên cứu học thuật theo bài báo. Giữ raw ngoài Assets và khỏi bản phát hành/public repository; chưa coi là texture CC0 hay nhãn thương mại.
- [Force and position dataset, record 17075127](https://zenodo.org/records/17075127), Poincloux, CC BY 4.0; dẫn [Poincloux, Adda-Bedia, Lechenault, PRX 2018](https://doi.org/10.1103/PhysRevX.8.021075) khi sử dụng.
- [XPBD, Macklin và cộng sự](https://matthias-research.github.io/pages/publications/XPBD.pdf): tham khảo quan hệ compliance, bước thời gian và ước lượng lực; cần kiểm chứng cách ánh xạ cho solver thực tế của dự án.
