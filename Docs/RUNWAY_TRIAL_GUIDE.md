# Chạy thử Runway Trial 0.8.1

Các thay đổi đã áp dụng: lưới physics váy 672 điểm, áo 384 điểm; mesh hiển thị chia nhỏ riêng; fit phom nghỉ trước khi tạo constraint; chuyển động hai pha trụ/vung chân với IK; khởi động/dừng chuyển tiếp; quán tính khi di chuyển mannequin; gravity theo thời gian thực; một substep thường và hai substep khi kéo/di chuyển nhanh (collider 0→0.5 và 0.5→1); kiểm tra đường đi điểm vải với proxy; self-contact điểm–điểm; nghỉ theo tốc độ vải. Áo Ivory có lỗ tay thực trong topology thay vì một ống vải bọc qua cánh tay.

## Mở đúng bản

1. Dừng Play nếu Unity đang mở.
2. Chờ compile. Nếu Unity đang đóng, mở `Open_FashionFit.bat`.
3. Bấm Play hoặc `Fashion Fit > Open and Run Prototype`.
4. Dưới FPS phải có dòng `RUNWAY TRIAL 0.8.1` và thời gian xử lý vải tính bằng ms.

## Thử trong 3 phút

1. Chọn Atelier Midi; mannequin mặc định đứng yên. Bấm **Trình diễn chậm · bước ngắn 0,18×**, quan sát chân trụ và độ trễ gấu váy.
2. Thử tốc độ 0,10 rồi 0,25 trong khoảng giới hạn mới 0,05–0,35×. Biên độ bước giảm còn 35%; gravity vẫn theo thời gian thực. Xem [hướng dẫn chống xuyên mới](SLOW_PRESENTATION_GUIDE.md).
3. Bấm dừng. Người mẫu hoàn tất bước cuối rồi chuyển về đứng; vải tiếp tục chuyển động và giảm dần.
4. Lần lượt chuyển sang Rose, Sapphire, Emerald và Ivory; quan sát trước, sau và hai bên.
5. Nếu đã có kính: Grip kéo vải ra rồi thử ép vào cơ thể, thả. Kéo/xoay mannequin để quan sát quán tính; tốc độ thao tác mannequin được giới hạn để tránh teleport.

FPS và số ms vải là số đo khi chạy trên máy, không phải cam kết hiệu năng. Nút `QUALITY` giảm chất lượng render; không loại bỏ va chạm để lấy FPS.

## Đọc kết quả kiểm tra

- `Verification/Runway_Trial_Checks.json`: đường cong gait, năm cage váy, tính hữu hạn, khoảng cách điểm/proxy và chặn kéo xuyên proxy trong bộ kiểm tra số ngoài engine.
- `Verification/Unity_Runway_Trial.json`: kiểm tra rig thật, năm mesh hiển thị, áo Ivory và quán tính transform trong Unity không có cửa sổ render. Menu chạy lại: `Fashion Fit > Verify Runway Trial` sau khi dừng Play.
- `Verification/Unity-Runway-Trial.log`: kết quả từng bộ; chỉ dòng `UNITY_RUNWAY_TRIAL_OK` mới xác nhận toàn bộ điều kiện của bài Unity đã qua.

Chưa có chứng nhận toàn bộ tam giác body/vải không giao cắt hoặc FPS/headset thực. Proxy và tự tiếp xúc điểm–điểm vẫn là xấp xỉ; đây chưa phải triển khai C-IPC/OGC. Dữ liệu Unity headless không đo vẻ đẹp của ánh sáng, shader hoặc chuyển động nhìn qua kính.
