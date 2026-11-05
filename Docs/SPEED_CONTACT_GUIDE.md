[Nhịp mới 0,50× / 0,75× / 1,00× và kết quả đo cập nhật](../Verification/Bimanual/REPORT.md).

# Kiểm tra vải trước, rồi tăng nhịp trình diễn

Đã áp dụng vào project Desktop: `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`.

## Dùng trên màn hình

1. Stop → chờ Unity compile → Play. Người mẫu mặc đồ vẫn khởi động đứng yên.
2. Chọn trang phục trong **TRANG PHỤC**, quan sát trước/sau và chờ vải ổn định.
3. Vào **BẮT ĐẦU**:
   - **Trình diễn chậm · 0,18×**: mức cũ.
   - **Nhanh hơn · 0,35×**: khoảng 1,94 lần nhịp cũ.
   - **Thử nhanh · 0,50×**: khoảng 2,78 lần nhịp cũ, giới hạn mới.
4. Bấm **Dừng motion** để hoàn thành bước rồi ổn định vải. **Tư thế fitting** đưa cơ thể/vải về tư thế fit ban đầu.

Slider **Nhịp người mẫu ×** chỉ cho tới 0,50×. Tốc độ được đổi dần, không nhảy pha hoặc tăng trọng lực. Biên độ bước vẫn giữ nguyên.

## Dùng trong kính

**Y trái → THỬ ĐỒ → Nhịp trình diễn**. Dùng tia và Trigger phải chọn đứng yên, 0,18×, 0,35×, 0,50× hoặc dừng. Grip phải vẫn dùng để tương tác đồ/khớp như trước.

Đây là nhịp đi tại chỗ của mannequin đang mặc đồ. **CÀI ĐẶT → Tốc độ đi** điều chỉnh tốc độ người chơi trong phòng, là điều khiển khác.

Hai mannequin nam/nữ trong POSE STUDIO hiện chưa mặc các bộ này và vẫn giữ tư thế Grip. Bài đo xuyên vải ở đây áp dụng sáu bộ trên mannequin nữ thử đồ hiện tại.

## Đã kiểm tra tới đâu

Đã kiểm tra đứng yên trước, sau đó 0,35×, 0,50× và 0,50× với nhịp cập nhật 30 lần/giây. Các ca mặc định đạt ngưỡng **không có điểm lấy mẫu xuyên cơ thể quá 1 mm**; độ sâu lấy mẫu lớn nhất của bộ đo mới khoảng **0,798 mm**. Đây không phải cam kết không xuyên tuyệt đối ở mọi khung hình hoặc khi kéo vải bằng VR.

Trong game vẫn giữ bộ bảo vệ tiếp xúc: nếu collider đại diện vi phạm liên tiếp, người mẫu giữ tư thế để vải ổn định. Khi thấy bất thường, dùng Dừng rồi quay về tư thế fitting. Chưa kiểm thử mọi thao tác Grip, gió, độ rộng áo, model nhập ngoài hoặc kính Meta thật ở các tốc độ mới.

[Báo cáo, ảnh và số đo](../Verification/SpeedContact/REPORT.md) · [Kết nối Quest Link](KET_NOI_QUEST_LINK.md).
