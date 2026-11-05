# Sửa xuyên vải và trình diễn chậm · 27/10/2026

Đã chỉnh trực tiếp runtime: khởi động đứng yên, bước ngắn ở 0,18×, thêm bao tiếp xúc thân người, sửa va chạm điểm neo và hạ đường nách ba thân đầm. Các trọng số AI không tham gia bản sửa này.

## Lỗi tìm được

1. Các điểm cố định trước đây không bị kiểm tra/đẩy ra khỏi proxy. Điểm neo sai có thể giữ vải xuyên body dù các điểm tự do được sửa.
2. Chỉ hai ellipsoid eo/hông không phủ được toàn bộ đường bao đo từ mesh body. Lớp mới dùng bao hướng kính theo tiết diện body kỹ thuật, biến đổi theo xương hỗ trợ, kết hợp các proxy tay/chân hiện có.
3. Thân đầm phía trên dùng skinning, không qua solver chân váy. Mép bên quá cao đi vào nách thật. Kiểm tra độc lập body gốc phát hiện độ xuyên lớn nhất khoảng **27,68 mm** trước khi sửa mép nách, dù số điểm xuyên proxy bằng 0. Đã sửa hình học mép bên thành đường cong thấp hơn; không chỉ che phần da lỗi.

## Chế độ mới

- Mặc định đứng yên, cho vải ổn định. Chạy motion bằng nút **Trình diễn chậm · bước ngắn 0,18×** hoặc Space/`MOTION`.
- Chờ 0,8 giây trước khi bước, tăng nhịp và biên độ dần. Biên độ bằng 35% cũ, tốc độ cho phép 0,05–0,35×; gravity vẫn theo giây thật.
- Dừng kết thúc bước từ từ; dừng ngay trong thời gian chờ không khởi động thêm một bước. Nếu xuyên proxy liên tiếp ba bước mô phỏng, giữ nguyên pose cho vải ổn định.
- Giới hạn kéo/quay mannequin và tốc độ hạ về sàn để giảm biến đổi collider đột ngột.
- Chỉ mesh render body được bỏ các tam giác nằm hoàn toàn dưới vùng đồ kín. Mesh gốc giữ nguyên, tay/cổ/gấu/xẻ vẫn chừa. Có thể tắt tùy chọn này; kéo VR sẽ hiện body đầy đủ tới lần fitting tiếp theo.

## Kiểm tra Unity

Luồng khởi tạo và mô phỏng được gọi trong Unity 2022.3.62f1 bằng CLI, dùng rig và mesh thật, không dùng lớp math giả. Đây không phải chạy thử bằng headset hoặc toàn bộ GUI trong Play Mode.

- Năm bộ, mỗi bộ **540 bước cập nhật được theo dõi khi trình diễn**, đi hơn một chu kỳ; tổng 2.700 bước đi được theo dõi, ngoài các bước đứng, chờ và dừng.
- **0 điểm xuyên proxy** trong các bước đi được đo; điểm neo cũng được tính. Bài riêng đặt toàn bộ điểm neo bên trong capsule đã kiểm tra khả năng đẩy chúng ra ngoài.
- Cả năm bộ khởi động/dừng hoàn tất, hiện lại body khi kéo VR và trả đúng mesh gốc khi bỏ đồ.
- Max stretch: Atelier **1,239**, Emerald **1,126**, Rose **1,181**, Sapphire **1,211**, Ivory **1,431** lần chiều dài cạnh nghỉ.
- Đã render và quan sát ảnh trước/bên của các tư thế đứng và đi. Nguồn: [results.json](SlowPresentation/results.json), [ảnh](SlowPresentation), log `SlowPresentation-Unity.log`.

## Đo độc lập với body gốc

Đã xuất body đầy đủ tại từng pose, kể cả khi đang bật che da trong render. Đo signed distance bằng `trimesh` với body kín, winding nhất quán, **không dùng proxy runtime hoặc body đã ẩn da**.

Mỗi bộ có ba pose: đứng và hai thời điểm đi. Lấy toàn bộ đỉnh thật sự được tham chiếu bởi mặt render và một tâm cho mỗi tam giác. Những điểm cage không dùng tại lỗ tay không được coi là bề mặt vải.

**15/15 pose qua kiểm tra**: không ghi nhận mẫu nằm bên trong body; độ xuyên lấy mẫu lớn nhất **0 mm**. Kết quả chi tiết: [source_body_contact.json](SlowPresentation/source_body_contact.json). Bản trước sửa nách được giữ riêng để đối chiếu; bản cũ còn tính cả một số điểm không được mặt render sử dụng tại lỗ tay.

## Hiệu năng và giới hạn

Thời gian CPU cloth trong bài CLI khoảng **14,9–17,7 ms/bước** cho bốn váy, **26,5 ms/bước** cho bộ áo + váy, có frame chậm hơn. Đây không phải số FPS toàn game hoặc kính VR. Giảm tốc gait không làm giảm số vòng vật lý; không tuyên bố đã hết lag. Khi ổn định, solver được nghỉ; Sapphire và Ivory vẫn đang settle ở mốc sáu giây sau yêu cầu dừng trong bài này.

Kiểm tra bề mặt tại 15 pose chưa chứng minh mọi điểm trên tam giác đều không giao cắt giữa các frame. Chưa kiểm thử full triangle/edge CCD, vải–vải nhiều lớp, nhân vật khác, OBJ ngoài hoặc headset. Phần che da là biện pháp render bổ sung, không thay cho va chạm. Mục tiêu của bản này là giảm rõ lỗi ở mannequin và năm bộ hiện có trong chế độ đứng yên/bước rất chậm.

Cách thử ngay: [SLOW_PRESENTATION_GUIDE.md](../Docs/SLOW_PRESENTATION_GUIDE.md).
