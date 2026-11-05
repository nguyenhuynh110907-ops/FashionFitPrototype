Bản mới V088: [sửa mép áo/nón/chân tủ và xem rõ thông số kéo giãn](SURFACE_READOUT_VR_GUIDE.md).

# Hollister nam — 5 bộ tham chiếu V087

Source được sửa trực tiếp trong `FashionFitPrototype`. Mã log của game mới: **FITTING_READY_V087_MENS_REFERENCE**. Bản này bao gồm sửa POSE/VẢI, nhấc nón và giới hạn kéo của V086.

## Mở và chọn bộ đồ

1. Thoát game FashionFit cũ nếu đang mở. Mở **FashionFit VR → 01 - VAO GAME VR** trên Desktop như trước; vào Quest Link trước khi bắt đầu VR.
2. Trong kính bấm **Y**, chọn **HOLLISTER NAM / 5** ở cuối bảng. Chọn một trong năm bộ để mặc cho mannequin **NAM**. Mẫu nữ và mẫu đi bộ giữ nguyên trang phục.
3. Bấm Y để cất bảng. Đổi **VẢI → POSE** ở đầu bảng nếu muốn chỉnh khớp; đổi về **VẢI** để kéo áo/quần. Grip cần sát bề mặt, khoảng dưới 11 cm. Hai tay giữ được hai vị trí và nhả riêng.
4. Có thêm thanh treo **HOLLISTER / 5 MALE STUDIES** sát tường bên phải phòng. Grip mẫu rồi đưa gần mannequin nam và thả để mặc. Các mẫu này không tự đổi sang đồ nữ nếu thả nhầm nhân vật.
5. Desktop: tab **TRANG PHỤC**, phần **HOLLISTER NAM · 5 BẢN THAM CHIẾU**.

| Bộ | Áo | Quần phối |
|---|---|---|
| 01 | Boxy Crew Tee, cổ tròn bo, tay ngắn | Denim xanh nhạt |
| 02 | Pique Polo xanh navy, cổ gập và nẹp nút | Chino khaki |
| 03 | Rugby dài tay, sọc đỏ/than, cổ tương phản | Denim xanh đậm |
| 04 | Rib Henley, cổ tròn và nẹp nút, tay dài | Chino than |
| 05 | Cotton Layering Tee dài tay | Chino olive |

Đây là **bản dựng tham chiếu**, chưa phải mesh/rập chính thức Hollister hoặc bản sao chính xác theo size hãng. Phối màu được tác giả chọn; logo/thêu đồ họa của sản phẩm chưa tái tạo. Các nẹp/nút ở bộ mới hiện là chi tiết hình học, chưa mở nút/cởi riêng như blouse thử.

## Dữ liệu và vật liệu

Mở [trang dữ liệu cục bộ](../AI/HollisterMens/Reference/BROWSE.html) để xem 11 ảnh, 7 hồ sơ sản phẩm và nguồn. Có thành phần vải, phom, cổ/tay, và thông số được công bố. Ví dụ áo Boxy Tee có 235 gsm; không suy độ cứng hay sức kéo đứt từ ảnh hoặc phần trăm cotton. Chưa huấn luyện AI hoặc hiệu chuẩn cơ học bằng dữ liệu này.

Texture trong game được tạo bằng code: weave nhỏ, rib và sọc rugby. Ảnh hãng được giữ ở thư mục tham chiếu, không nhúng làm texture hoặc coi là dữ liệu CC0. Giới hạn Cotton 5 cm / Stretch 10 cm vẫn là preset thử có sẵn của V086.

## Bản game

Bản mới nằm tại `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V087_Mens\FashionFittingRoom.exe`. Lối mở trên Desktop đã được trỏ qua launcher tới bản này. Các tệp game cũ được giữ vì tiến trình cũ đang sử dụng; không xóa dữ liệu của bạn.

[Báo cáo kiểm tra](../Verification/HollisterMens/REPORT.md) · [Pose, nón và vật liệu](POSE_BALANCE_VR_GUIDE.md)
