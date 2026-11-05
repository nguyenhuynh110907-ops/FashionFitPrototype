Cập nhật V086: [Pose, nhấc nón bằng Grip thường, kéo/nén hai tay](POSE_BALANCE_VR_GUIDE.md).

Cập nhật thao tác mới: xem [Áo dài và Grip nón/áo đang mặc](LAYERED_AODAI_VR_GUIDE.md). Grip thường nhấc nón khỏi đầu; Trigger + Grip chỉnh nón tại chỗ.

# Mặc đồ mannequin pose và thử độ đàn hồi trong VR

Cập nhật ngày 01/11/2026. Source được sửa trực tiếp tại `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`; bản game ở `C:\Users\HUYNH\Downloads\FashionFit_Run\FashionFittingRoom.exe`.

## Bắt đầu

1. Cắm kính Meta vào PC, mở Meta Horizon Link và vào Quest Link trong kính.
2. Trên Desktop mở **FashionFit VR → 01 - VAO GAME VR**.
3. Trong game chọn **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**. Di chuyển bằng cần trái, quay bằng cần phải; dùng hai tay cầm Touch.
4. Đến **POSE STUDIO**. Hai mannequin đã mặc đồ mặc định: nữ bộ Ivory, nam bộ cổ polo. Các chấm xanh vẫn dùng để chỉnh khớp; Grip tay nắm trên bệ để di chuyển cả mannequin, trang phục và nón.

## Mặc đồ và đội nón đúng nhân vật

Nhấn **Y** để mở bảng VR. Chọn phần đầu bảng bên phải **ĐÍCH MẶC / VẬT LIỆU**, rồi chọn **Mẫu đi bộ**, **Pose nữ** hoặc **Pose nam**. Sáu nút trang phục sau đó áp cho nhân vật đã chọn. Có thể chọn bỏ trang phục riêng của từng nhân vật.

Ở tủ đồ, Grip một mẫu trang phục, đưa gần thân rồi thả. Game chọn thân người gần nhất trong ba nhân vật. Nón cũng dùng cách đó: đưa gần đầu rồi thả để đội; đội nón mới chỉ thay nón trên cùng nhân vật. Cả ba có thể đội nón cùng lúc.

Muốn kéo vải thay vì chỉnh khớp, vào trang Pose Studio và **ẩn điểm khớp**. Khi hiện điểm xanh, khớp có ưu tiên tương tác. Đưa tay đến sát bề mặt trước khi Grip; bề mặt đồ pose nhận tay ở khoảng gần 8,5 cm. Không kéo từ xa bằng tia.

## Hai tay kéo giãn, nén và thả

- **Nón:** Grip nón trên bàn để cầm. Nón đang đội: Grip thường để tháo; Trigger + Grip để chỉnh tại chỗ. Tay còn lại Grip vào một vị trí khác trên nón. Tách hai tay để kéo giãn, đưa lại gần để nén. Có thể di chuyển/xoay cả nón khi giữ hai tay. Thả một tay vẫn giữ được bằng tay còn lại; thả tay cuối ở xa đầu thì nón rơi theo trọng lực.
- **Váy/đầm trên mannequin pose:** Grip hai vị trí khác nhau gần mặt vải rồi kéo hoặc đẩy. Thả từng tay độc lập. Mặt vải hồi dần về trạng thái mặc vừa, có ràng buộc tiếp xúc cơ thể.
- **Váy của mẫu đi bộ:** vẫn dùng bộ giải hạt PBD/XPBD và trọng lực hiện có; mô hình vật liệu mới được bật khi chọn vật liệu trong bảng.

Khi tháo nón, độ giãn còn lại được giữ tại thời điểm tháo rồi giảm theo thời gian, không nhảy ngay về mesh gốc. Nón đang đội tiếp tục chịu tiếp xúc đầu nên có thể vẫn giãn: nó hồi về cân bằng đang chịu tải, không thu nhỏ xuyên vào đầu.

## Xem độ căng và chọn vật liệu

Trong **ĐÍCH MẶC / VẬT LIỆU → Căng / nén vật liệu**:

1. Chọn đối tượng cần đo: **TRANG PHỤC** hoặc **NÓN**. Nón đang cầm được ưu tiên tự động.
2. **Đổi vật liệu** chuyển qua Cotton, Stretch, Felt và Leather. Chỉ đối tượng đang chọn đổi vật liệu; các mannequin khác giữ thiết lập riêng.
3. **Cỡ nón** chuyển 88% / 100% / 110%. Nón nhỏ hơn vùng đầu bị căng; tăng cỡ có thể giảm độ căng. Đây là chỉnh cỡ ban đầu, khác với biến dạng đàn hồi do Grip.
4. Xem độ giãn/nén theo phần trăm và lực màng ước lượng **N/m**. Ngoài miền đàn hồi giả định sẽ hiện cảnh báo; mô hình chưa làm rách vật liệu.
5. **Đặt lại biến dạng** đặt lại đối tượng đang chọn. Nón đang đội vẫn phải vừa vùng đầu.

## Công thức đang dùng

Với cạnh vải có chiều dài nghỉ `L0` và chiều dài hiện tại `L`:

`ε = (L − L0) / L0`

`S = E × h` (N/m), `T ≈ S × ε` (N/m).

`E` là mô đun đàn hồi theo hướng sợi, `h` là chiều dày. Nhánh nén có độ cứng nhỏ hơn để tránh làm vải cư xử như vật rắn; đây là mô hình rút gọn, chưa giải các nếp nhăn vật lý đầy đủ.

Hồi về cân bằng được tính bằng:

`u(t) = u_cân_bằng + [u(0) − u_cân_bằng] × exp(−t / τ)`.

Sau `τ` còn khoảng 36,8% sai lệch; sau `3τ` còn khoảng 5%. Các preset có thời gian hồi 0,8–1,8 giây. Tải trọng và tiếp xúc có thể làm trạng thái cân bằng khác mesh gốc. Công thức mũ được kiểm tra ở 30/60/90 Hz.

## Phạm vi hiện tại

Các tham số **E, chiều dày, tải kéo ảo và giới hạn đàn hồi đều là minh họa chưa hiệu chuẩn**. Texture không cung cấp số đo sức bền. Số N/m là ước lượng của mô hình màng, không phải lực đo từ tay cầm hoặc chứng nhận bền kéo/rách. Tay cầm không tạo được phản lực như vải thật.

Pose dùng biến dạng đàn hồi nhớt rút gọn, skin theo khớp, trường khoảng cách cơ thể và proxy va chạm cho chi di chuyển; không phải FEM toàn trang phục. Sáu bộ procedural được giữ UV, vật liệu con và chi tiết gốc khi chuyển sang hai mannequin. Đồ FBX/GLB/OBJ của sinh viên vẫn cần nhập/căn riêng. Cởi đồ bằng nút hiện áp cho blouse thử riêng trên mẫu chính, chưa áp cho các bộ pose này.

Các kiểm tra tự động không thay thế buổi thử kính thật, không chứng minh mọi tư thế và mọi thời điểm đều không xuyên vải. Xem [báo cáo và số đo](../Verification/PoseMaterial/REPORT.md).
