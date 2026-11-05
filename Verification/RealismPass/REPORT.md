Bản mới nhất: [hoàn thiện vùng nách](../UnderarmFix/REPORT.md), [chất liệu, chuyển động phụ và kết quả kiểm tra runtime](../MaterialMotion/REPORT.md). Các ảnh/số đo dưới đây mô tả giai đoạn trước những thay đổi đó.

# Chỉnh phom và bề mặt trang phục — 28/10/2026

Đã sửa trực tiếp mẫu 06 sau phản hồi “nhìn chưa thật”. Bản trước còn nách gấp vụn, cổ nổi khỏi vai, thân áo thụt vào so với eo váy và bề mặt quá đều. Bản này cải thiện những chỗ đó; vẫn là mannequin kỹ thuật và áo dựng tham chiếu, chưa đạt mức chân thực như ảnh người mặc.

## So sánh ảnh cùng góc máy

| Góc | Trước | Sau |
|---|---|---|
| Trước, vải sáng | [Ảnh trước](Before/Polo_LightFront.png) | [Ảnh mới](Polo_LightFront.png) |
| Bên, vải sáng | [Ảnh trước](Before/Polo_LightSide.png) | [Ảnh mới](Polo_LightSide.png) |
| Trước, vải đen | [Ảnh trước](Before/Polo_BlackFront.png) | [Ảnh mới](Polo_BlackFront.png) |
| Bên, vải đen | [Ảnh trước](Before/Polo_BlackSide.png) | [Ảnh mới](Polo_BlackSide.png) |

## Thay đổi

- Sửa khoảng đệm khi tạo lưới vùng nách. Khoảng dư của thân áo từng đẩy mặt vải sang mặt cơ thể bên cạnh trong khe hẹp giữa tay và thân, tạo tam giác gập nhọn. Giờ vùng này theo đúng bề mặt nguồn với khoảng đệm nhỏ hơn, rồi kiểm tra cả đỉnh và tâm tam giác. Các đỉnh trùng vị trí được xử lý chung để tránh nứt đường nối.
- Căn phần dưới thân áo theo vòng eo thực tế của chân váy đang mô phỏng, có đoạn chồng theo chiều dọc; giảm khe hở và gờ áo/váy.
- Đặt mép ngoài cổ theo vai áo, giữ phần chân cổ/cuộn cổ; thu nhỏ tiết diện chỉ và giảm tương phản đường chỉ.
- Thêm normal và roughness vải dệt Fabric061 từ bộ ambientCG CC0 có sẵn trong dự án. Không phủ màu caro của texture lên mẫu đen. Hai map nhập tuyến tính, có mipmap; chi tiết sợi giảm theo kích thước trên màn hình để hạn chế moiré. [Nguồn và hash](../../Assets/Resources/Fitting/WovenDetail/SOURCE.txt).
- Cân lại ba đèn theo ánh sáng studio trung tính hơn để đọc được phom và màu vải. Không thêm đèn hoặc tăng điểm mô phỏng.

Body, rig và số đo giữ nguyên bản BodySilhouette. Lưới áo có 7.583 đỉnh thay vì 7.582 do gộp đỉnh trùng sau đổi offset; vẫn 13.174 tam giác. Cage chân váy vẫn 672 điểm. Căn/làm mượt lưới được tính offline, không chạy lại mỗi frame. Shader thêm lấy mẫu texture; chưa đo hiệu năng trên kính VR. [Thông tin hình học](tailoring.json).

## Kiểm tra và bằng chứng

- [Kiểm tra sáu bộ](all-look-verification.json): 3.240 bước chậm, không vi phạm collider đại diện. Sau sửa lần cuối vùng nách, [chạy lại mẫu 06](verification.json): 540 bước, không vi phạm collider đại diện; độ giãn lớn nhất khoảng 1,281.
- [Kiểm tra chất liệu](material_verification.json): shader được hỗ trợ, ba vật liệu có map vải dệt, hai map nhập tuyến tính và có mipmap.
- [Đo tiếp xúc mẫu 06](PoloContact/body_contact_broadphase.json): đỉnh và tâm tam giác ở tư thế đứng cùng hai tư thế đi, so với body đầy đủ, không ẩn da. [Tổng hợp 18 tư thế](body_contact_broadphase.json) ghi hash từng mesh và số mẫu.
- [Đối chiếu nguồn](source_snapshot.json) ghi hash code, body, cache và texture giữa dự án chính với dự án Unity kiểm thử riêng.

Kết quả sau cùng: **286.125 mẫu trên 18 tư thế, không có mẫu xuyên quá 1 mm; độ xuyên lấy mẫu lớn nhất khoảng 0,0101 mm**. Mẫu 06 được xuất/đo lại ở ba tư thế sau lần sửa cuối (131.943 mẫu). Mười lăm tư thế của các bộ 01–05 không đổi; hash được đối chiếu trước khi tổng hợp. Chưa kiểm tra mọi thao tác VR hoặc mọi thời điểm giữa hai tư thế.

Đợt thử đầu trông gọn hơn nhưng vẫn xuyên đến 5,7 mm ở một số tâm tam giác; bản đó bị loại, không được áp vào dự án chính. [Kết quả thất bại được giữ lại](candidate1_contact_failed.json). Kết quả sau cùng nằm ở các liên kết kiểm tra phía trên. Đây là đo ở các tư thế lấy mẫu, chưa phải kiểm tra va chạm tam giác liên tục hoặc tự va chạm của toàn bộ vải.

## Xem trong Unity

Mở mẫu **06** trong **TRANG PHỤC**, bấm **Nhìn gần cổ áo và nẹp nút**, rồi **Xem phom · vải sáng**. Đổi về đen bằng **Khôi phục màu đen theo ảnh**. Dùng A/D để xem vòng quanh. Mẫu 06 vẫn giữ thân trên ổn định khi trình diễn chậm.

## Phần còn thiếu để đạt mức như ảnh thật

Áo hiện phát sinh từ lưới body, chưa có bộ mảnh rập riêng cho thân trước/sau, vòng nách và đầu tay. Chưa mô phỏng lực căng vải dệt/co giãn trên thân áo; các nếp nhỏ hiện là chi tiết dựng sẵn. Mannequin cũng chưa có vật liệu da và chi tiết người thật.

Bước phát triển cần thiết là thay thân áo nguồn bằng lưới may sạch theo các mảnh rập, UV theo kích thước vải và dữ liệu vật liệu, rồi căn/skin/mô phỏng trên mannequin. Với file 3D sinh viên cung cấp, cần giữ đường may, UV, normal và texture gốc khi căn sang body. Tăng số điểm hoặc chỉ phân loại ảnh hoa văn sẽ không tự tạo cấu trúc may và chuyển động đó. Không có AI mới được huấn luyện trong đợt chỉnh này.
