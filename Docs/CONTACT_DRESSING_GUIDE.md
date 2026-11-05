# V091 — Grip khớp và thử mặc/cởi vật lý

Bản cập nhật nằm trong project Unity chính và game V091. Chế độ mặc/cởi vật lý được bật riêng để thử. Kiểm tra cơ học của chế độ này còn **INCOMPLETE** do giãn quá mức và xuyên ở một số tình huống.

## Vào VR

1. Vào Quest Link bằng cáp và kiểm tra hai tay cầm Touch trong môi trường Link.
2. Mở **Desktop → FashionFit VR → 01 - VAO GAME VR**.
3. Trong game chọn **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**.
4. Thả Grip và Trigger một lần. **Y** mở bảng, **X** đưa bảng về trước.
5. Cần trái di chuyển, cần phải quay. Đi tới gần vật hoặc điểm khớp để Grip.

[Hướng dẫn kết nối chi tiết](VR_REENTRY_GUIDE.md).

## Chỉnh mannequin pose

- Nhấn nút **KÉO VẢI / CHỈNH KHỚP** ở đầu bảng Y để hiện các chấm khớp.
- Đưa tay tới chấm và giữ Grip. Kéo cổ tay/cổ chân để dùng IK, xoay tay cầm để chỉnh hướng khớp.
- Thả Grip để giữ tư thế. Khuỷu và gối có chiều gập cùng giới hạn góc.
- Hai tay có thể giữ hai khớp độc lập. Grip tay nắm của bệ để di chuyển cả mannequin.
- Chọn **Pose nữ** hoặc **Pose nam** ở phần đích mặc trước khi chọn trang phục hoặc nón.

Bài kiểm tra đã chọn và điều khiển đủ 34 điểm khớp bằng dữ liệu Touch giả lập. Trải nghiệm bằng kính thật cần được kiểm tra trên máy.

## Thử mặc/cởi bằng Grip

1. Chọn **Pose nữ**, dùng áo Ivory đang mặc sẵn cho bài thử đầu.
2. Mở **Y → ĐÍCH MẶC / VẬT LIỆU → MẶC/CỞI BẰNG GRIP**.
3. Đưa tay sát áo, giữ Grip và kéo. Có thể dùng hai tay giữ hai vùng.
4. Khi thả, lưới mô phỏng tiếp tục chịu trọng lực, ma sát và va chạm. Khoảng cách nghỉ của các cạnh mô tả phom vải.
5. Muốn mannequin khác giữ áo, đưa tay mannequin đến gần vải bằng chế độ chỉnh khớp, đổi lại sang VẢI, rồi nhấn Trigger gần bàn tay đó. Nhấn Trigger lần nữa để thả. Bảng thông số hiển thị số tay mannequin đang giữ.
6. **Về phom ban đầu / kết thúc bản thử** đặt lại các trang phục đang thử và trở về chế độ kéo đàn hồi.

Desktop có nút **Mặc/cởi bằng Grip · thử vật lý** ở trang Trang phục.

## Đọc kết quả hiện tại

- Kiểm tra thông thường và 34 khớp đã qua các bài tự động.
- Ba áo nam tay dài đã dùng lưới thân–tay liên tục. Phom, cổ và chi tiết hiển thị vẫn còn thô.
- Bản thử mặc/cởi đã nhận hai Grip và giữ/thả bằng tay mannequin.
- Bản thử còn giãn vượt preset, xuyên sau khi thả và xuyên với cơ thể mannequin khác trong bài kiểm tra chéo.
- Va chạm dùng proxy capsule/sphere. Độ cứng và ma sát là tham số thử. Lực và sức bền vật liệu chưa được hiệu chuẩn.
- Mật độ lưới cao chưa bảo đảm hình học may, đường nối, cổ và cửa tay đúng.
- Đo desktop hiện chưa đạt mục tiêu 72 Hz. Phiên Quest Link/Touch thật và độ trễ toàn chuỗi còn chưa được đánh giá.

[Báo cáo kiểm tra V091](../Verification/ContactDressing/REPORT.md).

