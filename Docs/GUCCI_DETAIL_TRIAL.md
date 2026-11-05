# Thử chi tiết đầm từ ảnh 2D

Mới nhất: [hình thể mannequin và phom áo ôm vừa](../Verification/BodySilhouette/REPORT.md). Dùng **Xem phom · vải sáng** và **Bỏ đồ · xem phom mannequin** trong mẫu 06 để so sánh. Body, số đo và cache đã được cập nhật; các báo cáo PoloStudy/PoloRoundness bên dưới thuộc các đợt trước.

Cập nhật mới: [phom ngực áo tròn hơn, normal mượt và chi tiết bề mặt](../Verification/PoloRoundness/REPORT.md). Ảnh và kiểm tra tiếp xúc của phom mới nằm trong `Verification/PoloRoundness`; `PoloStudy` lưu đợt dựng trước đó.

Mở scene hiện tại và bấm **Play → TRANG PHỤC → 06 · Gucci 864361**. Mẫu 06 được chọn sẵn khi khởi động. Bấm **Nhìn gần cổ áo và nẹp nút** để xem chi tiết, **Xem ảnh tham chiếu 2D** để so với ảnh gốc; dùng tab **VR / CAMERA** để trở lại góc toàn thân. Mặc định mannequin đứng yên; tab **BẮT ĐẦU** có nút trình diễn chậm. Riêng mẫu 06 giữ tay và thân trên ổn định trong khi chân/váy trình diễn, vì phần nách áo chưa có solver vải động riêng.

Mẫu tham chiếu là [Gucci Crêpe wool dress with Double G button, 864361 ZACKN 1000](https://www.gucci.com/it/it/pr/women/ready-to-wear-for-women/dresses-jumpsuits-for-women/crepe-wool-dress-with-double-g-button-p-864361ZACKN1000). Ảnh đã dùng và thông tin nguồn ở [References/GucciDressStudy](../References/GucciDressStudy/source.json). Đặc điểm dựng lại gồm cổ gập có chân cổ và độ dày, tay ngắn, nẹp trước, bốn nút kim loại mô phỏng Double G, mép chỉ và hai đường may thân trước. Chân váy kín, dáng ngắn rộng dần về gấu.

Đây là **bản dựng hình học có tham khảo ảnh**, không phải mesh chính thức của Gucci, không phải kết quả của mô hình AI đã được huấn luyện chuyển ảnh thành 3D. Phần lưng, đường ráp bên trong, lớp lót và kích thước chi tiết chưa quan sát được đều là ước lượng. Ảnh tham chiếu không được đưa vào tập huấn luyện.

## Những thay đổi trực tiếp trong game

- Đóng khe xẻ giữa của Emerald ở cả tam giác hiển thị, ràng buộc vật lý và vùng kiểm tra tiếp xúc.
- Thêm mẫu 06; cổ, nẹp, nút và đường chỉ là mesh 3D riêng, có nút ẩn/hiện để so sánh.
- Làm mượt phom áo, tính normal của áo và căn tiếp xúc trước khi chạy. UV và trọng số xương được giữ; không tăng mật độ hạt vật lý mỗi frame. Cache chỉ hợp lệ với đúng hai mã băm của lưới nguồn.
- Căn nẹp, nút và đường chỉ theo bề mặt áo đã fit, tránh chi tiết nổi lơ lửng khi nhìn ngang.
- Áo trên của mẫu 06 chạy theo xương, cổ/nút không có mô phỏng vải độc lập. Kéo vải VR hiện tác động vào phần váy; mẫu 05 vẫn có áo tương tác VR riêng. Không coi đây là áo mô phỏng đầy đủ như CLO/Marvelous Designer.

## Kiểm tra và tái tạo

[Verification/PoloStudy](../Verification/PoloStudy) chứa kết quả Unity, ảnh trước/sau/góc bên/cận cổ và kiểm tra đối chiếu với **body gốc không ẩn da**. `verification.json` là kiểm tra topology/proxy; cần đọc thêm `source_body_contact.json` để biết lỗi trên cơ thể gốc. Các tư thế lấy mẫu không chứng minh không xuyên vải ở mọi góc hoặc mọi chuyển động VR.

[Báo cáo đợt dựng đầu](../Verification/PoloStudy/REPORT.md): 1.080 bước mô phỏng; sáu tư thế với 164.016 mẫu, không có mẫu xuyên quá 1 mm. Mức lớn nhất khoảng 0,212 mm. Kiểm tra lại phom mới ở [PoloRoundness](../Verification/PoloRoundness/REPORT.md). Mẫu 06 dùng chế độ giữ thân trên/tay ổn định nêu trên.

Mã dựng chi tiết: `Assets/FashionFit/Runtime/TailoredPoloStudy.cs`. Bộ căn tiếp xúc offline: `Verification/bake_polo_contact.py`; dữ liệu Unity: `Assets/Resources/Fitting/polo-contact-fit.json`. Đây là phép căn hình học, không phải trọng số AI mới. Muốn đổi cấu trúc áo, cần xuất lại rest mesh trong project kiểm thử, chạy bake và kiểm tra các tư thế lại.

Để đưa trang phục 3D sinh viên vào bản chính, vẫn ưu tiên mesh, UV, vật liệu PBR và chi tiết gốc từ file 3D. Không tự suy đoán hoặc vẽ đè hoa văn mới lên thiết kế gốc.
