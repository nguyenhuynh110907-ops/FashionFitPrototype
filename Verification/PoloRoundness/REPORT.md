# Sửa phom ngực áo — 28/10/2026

Đã sửa trực tiếp mẫu đầm 06. Phom cũ giữ quá nhiều độ gồ của cơ thể và chuyển tiếp hẹp quanh ngực, làm thân áo có chóp khi nhìn chéo/ngang. Các đỉnh trùng vị trí nhưng tách UV/trọng số còn tạo mảng ánh sáng gãy.

## Thay đổi

- Căn một bề mặt thân trước tròn, có độ dư liền mạch qua ngực và chuyển tiếp mềm theo chiều dọc. Giữ đỉnh ngoài của áo, thêm độ dư xung quanh thay vì ép vải vào cơ thể. Cổ, lưng và mép tay không bị thay phom theo phép này.
- Tính normal mượt theo vị trí trùng nhau và theo bề mặt vải phía ngoài vùng ngực. Normal của vùng này lấy từ trường chiều cao có lọc 6 mm, tránh lấy hướng mặt trong các nếp của lưới cơ thể làm hướng sáng của áo. Giữ UV, trọng số xương và topology. Đây là xử lý offline và một lần khi tạo áo, không chạy thêm solver mỗi frame.
- Chiếu lại nẹp, bốn nút và đường chỉ lên đúng bề mặt áo mới. Không để chi tiết ở vị trí của phom cũ.
- Chỉnh cổ/nẹp gần màu thân và nhám hơn. Thân áo và các chi tiết tiếp tục đổ bóng; không dùng tắt bóng để che lỗi normal.
- Giữ 7.586 đỉnh áo, 5.264 đỉnh chi tiết; cage vật lý váy vẫn 672 điểm. Mannequin và các trang phục khác không bị thay hình dạng.

Ảnh trước: [bên](Before/Side.png), [cận](Before/CollarClose.png). Ảnh sau: [bên](Side.png), [cận](CollarClose.png), [toàn thân](Look6_Standing.png).

## Kiểm tra hình dáng

[Đường cắt thực của mesh](bust_profiles.png) so sánh body, áo trước và áo sau. Tại hai vị trí X = ±0,09 m, Y = 1,26 m, độ gồ cục bộ trên khoảng đo ±15 mm giảm từ khoảng 0,52 mm xuống 0,26 mm. Đây là chỉ số hình học ở các điểm cụ thể, không phải thước đo tổng thể về tính thẩm mỹ hay chứng nhận rập may đúng.

[profile_check.json](profile_check.json) xác nhận số đỉnh và topology giữ nguyên, X/Y giữ nguyên, phần sửa chỉ đẩy thân trước ra ngoài. [roundness.json](roundness.json) ghi thông số căn phom; [source_snapshot.json](source_snapshot.json) ghi hash code/cache trùng với dự án kiểm thử.

## Chuyển động và tiếp xúc

Unity đã chạy 1.080 bước chậm cho mẫu 02 và 06, đạt 45.025 kiểm tra topology/trọng số/cache/normal và không ghi nhận điểm trong collider đại diện. Kết quả ở [verification.json](verification.json). [normal_check.json](normal_check.json) xác nhận 7.586 normal hữu hạn, độ dài chuẩn hóa sai lệch tối đa khoảng 4,2×10⁻⁸. Body đầy đủ được kiểm tra riêng trong [source_body_contact.json](source_body_contact.json).

Kiểm tra body gốc đủ sáu tư thế, 164.016 mẫu đỉnh/tâm tam giác: **0 mẫu xuyên quá 1 mm**, lớn nhất khoảng **0,212 mm**. Ba phép đo của mẫu 02 được dùng lại theo hash nhị phân khớp hoàn toàn; ba tư thế của mẫu 06 với phom mới đã được đo lại. Các thay đổi normal/vật liệu sau đó giữ nguyên vị trí và topology; chỉ dùng lại kết quả khi hash nhị phân lưới xuất trùng hoàn toàn.

Mẫu 06 tiếp tục giữ tay/thân trên ổn định khi đi chậm; chân/hông/váy vẫn chuyển động. Chưa có solver vải động riêng cho nách áo, tự va chạm vải hoặc kiểm chứng mọi thao tác kéo VR. Đây vẫn là bản dựng tham chiếu ảnh, chưa phải cấu trúc rập/lót/đường ráp sản xuất của Gucci.
