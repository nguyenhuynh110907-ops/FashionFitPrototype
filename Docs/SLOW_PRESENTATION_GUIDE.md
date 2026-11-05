Đã kiểm tra đứng yên rồi tăng nhịp 0,35× / 0,50×: [cách thử và phạm vi đo](SPEED_CONTACT_GUIDE.md).

# Đứng yên và trình diễn rất chậm

Bản sửa ưu tiên hạn chế xuyên cơ thể ở năm bộ đồ có sẵn trên mannequin kỹ thuật. Mở lại Play Mode để áp dụng mặc định mới; các file đã được sửa trực tiếp trong dự án này.

Cập nhật góc nhìn gần: đã sửa bounds khiến thân đầm biến mất và **tắt che da mặc định**. Xem [báo cáo mới](../Verification/CLOSE_VIEW_FIX_REPORT.md). Phần chỉnh áo giữ cổ tại chỗ khi đổi rộng/dài.

## Sử dụng

1. Bấm Stop rồi Play trong Unity. Mannequin **đứng yên** để mặc đồ và cho vải ổn định.
2. Trong **BẮT ĐẦU**, bấm **Trình diễn chậm · bước ngắn 0,18×**. Hệ thống chờ 0,8 giây trước khi bước, rồi tăng nhịp/biên độ dần. Một chu kỳ đủ hai chân ở tốc độ ổn định kéo dài khoảng **6,7 giây**.
3. Thanh tốc độ giới hạn **0,05–0,35×**; bước chân và nâng gối chỉ dùng **35% biên độ cũ**. Trọng lực vẫn chạy theo thời gian thực.
4. Bấm **Dừng motion** để hoàn tất bước và về đứng yên từ từ. Bấm **Tư thế fitting — đứng yên và settle** khi cần căn lại ngay; thao tác này đặt lại vải ở tư thế nghỉ.
5. Trong VR, `MOTION` chạy/dừng cùng chế độ chậm; `FIT` đưa về đứng yên. Kéo mannequin giới hạn 0,25 m/s, quay 25°/s; khi thả trên không, mannequin hạ xuống tối đa 0,25 m/s.

Nếu các điểm còn xuyên proxy trong ba bước mô phỏng liên tiếp khi đang trình diễn, hệ thống giữ nguyên tư thế hiện tại cho vải ổn định. Nó không tự giật chân về tư thế nghỉ hoặc tự chạy lại.

## Những sửa đổi chống xuyên

- Bổ sung bao tiếp xúc eo–hông và thân áo theo tiết diện đã đo từ body kỹ thuật. Các capsule tay/chân và kiểm tra đường đi điểm vẫn hoạt động cùng bao này.
- Điểm neo cũng phải được đẩy ra ngoài va chạm. Trước đây chúng bị bỏ qua khi kiểm tra và có thể giữ vải bên trong body.
- Mép nách ba thân đầm được hạ và bo cong. Phép đo với mesh body gốc phát hiện mép cũ đi vào nách tới khoảng 28 mm dù mannequin đứng yên; giảm tốc không giải quyết được lỗi hình học này.
- Các đỉnh hiển thị cũng được kiểm tra tiếp xúc sau nội suy; không tăng số điểm physics để tránh nhân chi phí mô phỏng.
- Có tùy chọn **Ẩn da bên dưới vùng trang phục che kín**. Nó chỉ bỏ những tam giác da nằm hoàn toàn trong vùng đồ kín khỏi bản mesh render; không sửa mesh body gốc. Cổ, tay, mép gấu và vùng xẻ được chừa lại. Khi kéo vải bằng VR, body đầy đủ được hiện và tiếp tục hiện sau khi thả; chọn tư thế fitting để bật lại phần che. Bỏ chọn tùy chọn này để quan sát trực tiếp toàn bộ da.

Ẩn da là biện pháp dựng hình bổ sung. Các phép đo tiếp xúc độc lập trong đợt này dùng **body gốc đầy đủ**, không dùng mesh đã ẩn da để làm kết quả đẹp hơn.

## Kiểm chứng và giới hạn

Kết quả Unity nằm ở [SlowPresentation/results.json](../Verification/SlowPresentation/results.json), ảnh đứng/đi ở thư mục [SlowPresentation](../Verification/SlowPresentation). Phép đo độc lập với body gốc nằm ở [source_body_contact.json](../Verification/SlowPresentation/source_body_contact.json). Xem [báo cáo sửa lỗi](../Verification/SLOW_CONTACT_REPORT.md) để biết số lượng và thời gian đo.

Đây vẫn là bộ giải điểm/proxy với kiểm tra mesh tại các tư thế lấy mẫu, chưa phải chứng nhận mọi tam giác không giao nhau trong mọi chuyển động liên tục. Các rập từ GarmentDetail và trọng số AI chưa được áp vào tủ đồ này. Nhân vật khác, OBJ nhập ngoài và thao tác VR cực đoan cần kiểm tra riêng. Chế độ chậm giảm chuyển động cơ thể, không tự bảo đảm FPS của kính VR.
