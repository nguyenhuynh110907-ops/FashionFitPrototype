# Sửa vùng nách mẫu 06 — 28/10/2026

Đã áp cache fit mới trực tiếp vào `Assets/Resources/Fitting/polo-contact-fit.json`. Phần sau nách được đẩy ra cục bộ, có kiểm tra các điểm bên trong tam giác, sau đó hoàn thiện khoảng hở nhìn thấy ở mức dưới milimét. So với cache trước đợt sửa này, 484 đỉnh đổi vị trí, mức dịch lớn nhất 3,595 mm; không đổi topology, UV, trọng số xương hoặc body. [Thông số hình học](geometry.json).

Lỗi có hai phần: cache mới chưa được cập nhật vào object đang Play và một số điểm tiếp xúc thật ở mặt sau nách. Đã thêm cập nhật cache trong Editor và menu **Fashion Fit → Refresh Active Garment Fit**. Kiểm tra refresh sau khi cố ý làm lệch một đỉnh xác nhận khôi phục fit mà vẫn giữ camera, vật liệu và trạng thái váy.

## Kết quả

- Kiểm tra dày ở các tam giác vùng sửa: 28.418 mẫu qua ba tư thế, không có mẫu xuyên quá 1 mm; độ xuyên lấy mẫu lớn nhất 0,6181 mm. [Kết quả](dense_contact.json). Đây là lần đo cache trước khi thêm chuyển động chất liệu.
- Kiểm tra lại sau khi tích hợp vật liệu: sáu góc nhìn 512 px, cả hai nách từ trước/bên/sau, **0 pixel da lộ trong dải cần che**. Body đầy đủ, không dùng mặt nạ che da. Đối chứng bỏ áo cho 189.108 pixel da để xác nhận phép đo phát hiện được cơ thể. Có thêm 540 bước chậm và kiểm tra refresh cache. [Kết quả cuối](visibility.json).
- Bản runtime cuối được kiểm tra toàn thân ở 18 tư thế trong [báo cáo vật liệu](../MaterialMotion/REPORT.md).

Ảnh kiểm tra cuối: [nách trái phía sau](LeftBack_Fabric.png), [nách phải phía sau](RightBack_Fabric.png), [bên trái](LeftSide_Fabric.png), [bên phải](RightSide_Fabric.png). Các ảnh `*_Visibility.png` tô vùng da mục tiêu; ảnh `*_BodyOnlyControl.png` là đối chứng bỏ áo.

Các phép đo này xác nhận những tư thế/góc đã thử, không chứng minh mọi tam giác luôn không xuyên ở mọi góc nhìn, tốc độ hoặc thao tác kéo VR. Mẫu 06 vẫn giữ tay và thân trên ổn định khi trình diễn chậm.
