# V090 — phản hồi nhanh hơn và thư mục gọn hơn

Thoát game cũ rồi mở **Desktop → FashionFit VR → 01 - VAO GAME VR**. Launcher chọn V090; V089 được giữ để quay lại.

Trong kính: **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**; thả Grip/Trigger một lần để bắt đầu. **VẢI** kéo áo/nón; **POSE** chỉnh các chấm khớp. Khi đang giữ áo/khớp, trang phục cập nhật mỗi frame thay vì chờ nhịp 30 Hz. Tốc độ thực tế vẫn phụ thuộc thời gian render của máy.

Kéo/nén hai tay, đổi tay, đội rồi Grip thường nhấc nón ra; thả tay cuối để vật rơi. Muốn chỉnh nón tại chỗ trên đầu: Trigger + Grip. Bộ thông số và định luật hồi giữ nguyên; các preset vẫn chưa hiệu chuẩn.

Đợt này đã xóa 1403 tệp trong 8 bản build trùng, khoảng 1.08 GiB. Chỉ xóa bản sao được so SHA256 với bản giữ; không xóa dữ liệu huấn luyện, model/texture/source chính hoặc checkpoint. Bản cũ đã cài vẫn còn.

- `README.md`: điểm bắt đầu ngắn, không còn nhiều banner “bản mới” chồng nhau.
- `Docs/HISTORY_README_V089.md`: toàn bộ README/liên kết nghiên cứu cũ.
- `Docs/INDEX.md`: mục lục tài liệu.
- Các thư mục `*_Work` ở Downloads là phần kỹ thuật; không cần mở để chơi.

Lượt desktop so sánh: p95 frame khoảng **27.3 → 21.7 ms**. Chưa đạt 72 Hz ổn định, chưa kiểm chứng bằng kính thật.

Muốn ghi phiên thật: mở **04 - GHI HIEU NANG VR** sau khi thoát game cũ; vào Link và VR như trên. Kết quả ở `%LOCALAPPDATA%\FashionFit\Performance`. [Cách đọc CSV/JSON và chạy kiểm tra](STUDIO_WORKFLOW.md).

[Báo cáo, bằng chứng đối chiếu và danh sách tệp đã dọn](../Verification/ResponseOptimization/REPORT.md).


Hoàn tất dọn: **1,931 tệp build trùng/tạm**, tương đương **1.53 GiB** sau khi giữ archive nhỏ của phần riêng các build thử. Project/dữ liệu huấn luyện/model/texture gốc và các bản đã cài không bị xóa. Lượt kiểm tra lại EXE đã cài có p95 desktop 23.7 ms; kết quả vẫn dao động theo tải máy.
