# FitVision AI · mô hình thử nghiệm đã huấn luyện

Đã chạy huấn luyện cục bộ trên bộ ảnh của dự án và lưu trọng số thật. Mô hình dùng **DINOv2-small đã có sẵn, giữ nguyên trọng số encoder**, cộng hai lớp phân loại được huấn luyện mới: 7 nhãn loại trang phục và 5 nhóm phom quan sát. Có **4.620 tham số được cập nhật**. Đây chưa phải AI dựng trang phục 3D hoặc AI mô phỏng vải.

Kết quả: **60% loại trang phục (12/20)** và **41,2% nhóm phom (7/17)** trên tập kiểm tra. Độ chính xác còn thấp, chỉ dùng nghiên cứu và kiểm tra thủ công. Xem [báo cáo đầy đủ](TRAINING_REPORT.md).

## Dùng ngay trong Unity

1. Dừng Play nếu đang chạy; đợi Unity compile script mới.
2. Chọn menu **Fashion Fit > AI > Analyze Garment Photo**.
3. Bấm **Chọn ảnh JPG / PNG**, rồi **Phân tích bằng mô hình đã huấn luyện**.
4. Xem ba dự đoán đầu cho từng đầu ra, kèm gợi ý dựng phom. Luôn đối chiếu ảnh trước khi dùng kết quả.

Phân tích chạy bằng một tiến trình Python nền khi bấm nút; không chạy mỗi frame. Đóng cửa sổ hoặc bấm Hủy sẽ dừng tiến trình do cửa sổ đó tạo. Không tải ảnh lên dịch vụ bên ngoài. Các gợi ý dựng phom là quy tắc viết trong chương trình dựa trên nhãn AI; chúng không phải thông số vật lý AI học từ ảnh.

Ngoài Unity: nhấp đúp `Analyze_Garment_AI.bat` ở gốc project, chọn ảnh. Kết quả nằm trong `Artifacts/latest_prediction.json`. Có thể dùng PowerShell:

```powershell
.\.ml-runtime\python\python.exe AI\FitVision\fitvision.py predict --image "C:\duong-dan\anh.jpg"
```

Phải giữ cả `AI/FitVision/Encoder/`, `AI/FitVision/Artifacts/` và `.ml-runtime/` trong project hiện tại. Mô hình và Python nằm ngoài `Assets`, không được import vào game VR.

## Huấn luyện lại

Nhấp đúp **Train_FitVision.bat**. Lệnh chạy lại với dữ liệu, seed và cấu hình hiện có; ghi đè kết quả trong `Artifacts`. Tập test vẫn giữ tách biệt theo nhóm trang phục. Không dùng kết quả test để chọn epoch; chỉ dùng validation. Việc chạy thêm nhiều epoch không tự làm AI tốt hơn: lần này validation tốt nhất ở epoch 2 và xấu đi sau đó.

Nếu chuyển sang máy mới, tại thư mục gốc project:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File AI\FitVision\Setup-Environment.ps1
# Chỉ lần đầu khi chưa có Encoder/ mới cần tải trọng số công khai:
.\.ml-runtime\python\python.exe AI\FitVision\fitvision.py train --download-encoder
```

Python được giải nén riêng vào project; không đổi PATH hoặc cài runtime hệ thống. `Setup-Environment.ps1` cần Internet để tải Python/thư viện từ các nguồn ghi trong script. Khi đã có encoder, huấn luyện và phân tích có thể chạy ngoại tuyến. `requirements.lock.txt` lưu các phiên bản đã dùng.

Kiểm tra lại artifact và đường suy luận độc lập:

```powershell
.\.ml-runtime\python\python.exe AI\FitVision\verify_fitvision.py
```

## Tệp quan trọng

| Tệp | Nội dung |
| --- | --- |
| `fitvision.py` | Kiểm tra ảnh, trích đặc trưng, chia nhóm, huấn luyện, đánh giá và suy luận |
| `Artifacts/heads.safetensors` | Trọng số hai đầu phân loại đã huấn luyện |
| `Artifacts/model.json` | Nhãn, chuẩn hóa từ tập train, nhiệt độ từ validation, hash trọng số |
| `Encoder/model.safetensors` | Encoder pretrained giữ nguyên |
| `encoder_source.json` | Nguồn, revision chính xác và cách tiền xử lý encoder |
| `Artifacts/metrics.json` | Độ chính xác, macro F1, confusion matrix, baseline và khoảng bootstrap |
| `Artifacts/splits.json` | Danh sách train/validation/test và mã nhóm |
| `Artifacts/predictions.csv` | Dự đoán từng ảnh, có cột split để phân biệt kết quả trên ảnh đã học |
| `Artifacts/training_history.json` | Loss theo từng epoch |
| `Artifacts/retrain_verified.log` | Log chạy lại thành công bằng encoder và feature cache cục bộ |
| `Artifacts/verification.json` | Kiểm tra hash, nhóm không chồng, suy luận độc lập, từ chối trọng số sai hash |

## Muốn học cách mặc và chuyển động tốt hơn

Cần sửa dữ liệu trước khi tăng thời gian huấn luyện: đánh dấu đúng vùng món đồ cần học, tách nhãn ôm/xòe khỏi nhãn nhiều lớp/xẻ, bổ sung đủ mẫu cho áo/jumpsuit và nhiều nguồn chụp khác nhau. Bộ hiện tại có ảnh nhóm, ảnh cắt thân và ảnh nhiều món trong một khung, nên nhãn chính dễ mơ hồ.

Để học fit 3D cần cặp body–garment có pattern/mesh, số đo và trạng thái mặc đúng. Để học chuyển động cần chuỗi video/scan hoặc mô phỏng được kiểm chứng, đồng bộ body pose và hình dạng vải. Không thể suy ra các đại lượng đó chỉ bằng huấn luyện trên 100 ảnh tĩnh hiện có.

Nguồn và điều khoản: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
