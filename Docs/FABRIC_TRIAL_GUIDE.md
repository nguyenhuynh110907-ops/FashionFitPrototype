# Dùng thử mẫu vải và AI hoa văn trong Unity

Đã tích hợp trực tiếp vào project. Khi vào Play, bảng mở ở **VẢI / AI → DÙNG THỬ · CHI TIẾT VẢI**. Nếu đang ở phiên Play cũ: Stop, chờ compile xong rồi Play lại.

1. Chọn bộ đồ ở **TRANG PHỤC**, hoặc giữ bộ midi mặc định.
2. Vào **VẢI / AI**. Chọn một trong sáu mẫu: caro kem, xanh ngọc, ô vuông trắng, dệt nhỏ xám, xanh lam, đỏ gạch.
3. Bấm **Áp mẫu đã chọn lên trang phục**. Mẫu có sẵn dùng đủ map màu, normal OpenGL và roughness; mesh, UV và solver không thay đổi. Có thể đổi sang bộ khác khi mẫu đang áp.
4. Bấm **So sánh** để bật/tắt mẫu, đối chiếu với vật liệu mặc định. Thanh **Số lần lặp texture** ở dưới giúp điều chỉnh kích thước họa tiết. Với các mẫu vải lặp, thử khoảng 2–4 nếu vân đang quá lớn; với texture thiết kế theo UV riêng của bạn, giữ 1 để kiểm tra trước.
5. Bấm **AI · nhận diện hoa văn đã chọn**. Kết quả hiện ba nhóm đứng đầu sau vài giây; lần đầu có thể lâu hơn khi nạp mô hình. Có thể hủy hoặc chọn ảnh khác. Nhận diện chạy trên máy qua môi trường Python hiện có, không cần API key hay tải ảnh lên mạng.
6. Muốn thử texture của bạn: **Chọn ảnh texture của bạn**, dùng PNG/JPEG tối đa 20 MB và 4096×4096. Nút áp ảnh này giữ nguyên tọa độ UV hiện tại; ảnh chụp cả người mặc đồ không phải texture atlas phù hợp.
7. Có thể bấm **BẮT ĐẦU → Trình diễn chậm** để xem mẫu vải khi mannequin bước. Khi thử VR, chọn vải trước trên desktop rồi vào kính.

## Phạm vi của bản thử

- AI hiện nhận sáu nhóm hoa văn, không tự tạo thêm chi tiết, đổi phom hay sửa cổ áo. Vải trơn không nằm trong sáu lớp; điểm số không phải độ tin cậy đã hiệu chuẩn. Không dùng tên AI dự đoán để tự thay đổi texture gốc.
- Đây là thử bề mặt trên năm trang phục hiện có. Chưa phải pipeline nhập đầy đủ quần áo GLB/FBX sinh viên hay retarget hoa văn giữa hai UV khác nhau. Cổ/tay và solver hiện có giữ nguyên.
- Phần nhận diện AI dùng môi trường cục bộ ở thư mục project. Bản build hoặc Android không kèm Python/checkpoint; sáu mẫu vật liệu đóng gói vẫn có thể dùng trên desktop. Chưa xác nhận streaming path Android hoặc headset.
- Nguồn sáu mẫu: ambientCG, CC0. Map gốc không sửa byte; ghi nguồn trong `Assets/StreamingAssets/FashionFit/FabricTrial/LICENSE_SOURCES.txt`. Bản đóng gói khoảng 23,8 MB, không kéo toàn bộ dữ liệu train vào Unity.

## Kiểm chứng đã chạy

[Kết quả Unity](../Verification/FabricTrial/verification.json): sáu mẫu áp đủ ba map, mesh/UV không đổi, bật/tắt khôi phục hình ảnh gốc, thay ảnh lỗi không phá vật liệu đang dùng. Đã chạy **Unity → Python → kết quả JSON → Unity**, hủy worker và đổi lựa chọn khi đang chạy; không dùng kết quả cũ cho ảnh mới.

Ảnh mới: [vật liệu gốc](../Verification/FabricTrial/Original.png), [mẫu caro](../Verification/FabricTrial/Sample0.png), [mẫu trên bộ Ivory](../Verification/FabricTrial/IvorySample.png). [Log](../Verification/FabricTrial/unity_trial.log), [điểm AI thực tế](../Verification/FabricTrial/unity_ai_prediction.json).

Bản thử bề mặt này không giải quyết các hạn chế hiệu năng/settle/va chạm đã ghi trong [báo cáo trước](../Verification/TrialRun_20261028/REPORT.md). Cần tiếp tục kiểm chứng riêng trước thực nghiệm VR.
