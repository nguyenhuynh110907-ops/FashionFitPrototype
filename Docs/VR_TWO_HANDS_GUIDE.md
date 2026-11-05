# Hai tay, đi bộ nhanh hơn và bệ mannequin pose

Mở lại `Open_FashionFit_Model.bat` để dùng EXE mới ở `C:\Users\HUYNH\Downloads\FashionFit_Run`. Với Unity Editor, mở `Open_Quest_Link_Trial.bat`. Chỉ chạy một phiên XR: EXE hoặc Editor.

## Điều khiển hai tay Touch

Trong kính bật Quest Link, vào FashionFit → **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**. Thả Grip/Trigger một lần khi vào hoặc khôi phục tracking. Cần trái đi trong phòng, cần phải quay, **Y** mở/ẩn menu, **X** đưa menu về trước. Trigger trái/phải chọn nút.

Grip vẫn chỉ nhận gần tay: vùng 16 cm hoặc đoạn trỏ ngắn phía trước tối đa 25 cm tới collider. Tia menu dài hơn không dùng để lấy vật từ xa.

### Hai tay cùng giữ một nón hoặc mẫu đồ

1. Đưa tay đầu đến vật rồi giữ Grip.
2. Đưa tay còn lại tới một phần khác của cùng vật rồi giữ Grip.
3. Di chuyển hai tay để mang vật, xoay hướng hai tay để xoay vật. Không thay đổi kích thước vật bằng cách kéo giãn hai tay.
4. Thả một tay: tay còn lại tiếp tục giữ, không nhảy về vị trí mới. Thả tay cuối: vật rơi theo trọng lực, hoặc được đội/mặc nếu đang sát điểm mặc tương ứng.

Hai tay cũng có thể cầm hai món độc lập. Mỗi vật nhận tối đa hai tay. Khi hai tay quá sát nhau hoặc hướng cầm không xác định được trục quay, giữ góc quay gần nhất để tránh giật. Mất tracking một tay không tự buông tay kia hoặc thêm lực ném.

### Kéo vải bằng hai tay

Nắm hai vùng khác nhau trên váy/áo bằng Grip. Hai tay điều khiển hai mảng lưới riêng; thả một tay thì mảng của tay còn lại vẫn được giữ. Đặt hai tay quá sát cùng mảng lưới sẽ không nhận lần nắm thứ hai.

Khoảng kéo của mỗi mảng hiện giới hạn **12 cm từ điểm bắt đầu nắm**, có giới hạn căng tương đối giữa hai mảng và vẫn xử lý va chạm cơ thể. Muốn kéo vùng khác, thả rồi nắm lại. Đây là giới hạn ổn định tương tác, không phải phép đo độ bền thật của vải. Vải tiếp tục ổn định theo trọng lực sau khi thả; không đổi trọng lực theo tốc độ trình diễn.

Hai điểm kéo hỗ trợ các váy procedural và áo Ivory. Các bản chuyển 3D của sinh viên vẫn giữ vật liệu gốc và chưa mở kéo vải/motion chưa kiểm định.

## Di chuyển mannequin pose bằng bệ

Hai mẫu nam/nữ ở **POSE STUDIO** có 17 điểm xanh mỗi mẫu. Có thể đến gần bằng cần trái hoặc **Y → CÀI ĐẶT → Mẫu nam/nữ → nhìn gần**.

- **Giữ Grip ở bệ, tay nắm tròn phía trước hoặc vùng thân ngoài điểm xanh** để di chuyển cả mannequin cùng bệ. Kéo tay để đưa đi, nâng nhẹ để nhấc bệ, xoay cổ tay để quay. Bệ giữ thẳng đứng; không lật hoặc đổi kích thước cơ thể.
- Tay thứ hai có thể cùng nắm bệ. Thả một tay vẫn giữ bằng tay còn lại. Thả tay cuối để bệ hạ xuống sàn theo trọng lực; không ném mannequin.
- Bệ bị chặn bởi vật chắn, bị giới hạn trong phòng và tránh kéo sát vị trí đầu người dùng. Có thể kéo ngang sát sàn mà không cần nhấc cao.
- Tư thế các khớp giữ nguyên khi bệ di chuyển. Trong lúc giữ bệ, tạm khóa chỉnh khớp để tránh giành điều khiển. Sau khi thả, Grip điểm xanh để chỉnh khớp như trước.
- **Đặt lại mẫu nữ/nam** phục hồi cả tư thế và vị trí bệ ban đầu.

Các mannequin pose vẫn là hình nộm giữ tư thế, chưa có ragdoll hoặc tự cân bằng. Rigidbody bệ dùng collider xấp xỉ, không phải va chạm chi tiết từng ngón tay/chân.

## Nhịp đi bộ mới

Ứng dụng vẫn mở ở tư thế đứng yên. Nút đi bộ chính chọn **0,50×** thay cho 0,18×. Trong **Y → THỬ ĐỒ → Nhịp trình diễn** có **0,18× / 0,75× / 1,00×**, cùng nút đứng yên và dừng. Desktop có thêm nút 0,50×.

Đây là hệ số nhịp bước của mannequin trình diễn, không phải tốc độ di chuyển của người đeo kính hay FPS. Nhịp tăng dần tối đa 0,30× mỗi giây, có giai đoạn ổn định vải và bộ giữ tư thế khi phát hiện vi phạm collider đại diện. Chọn **Dừng** để kết thúc bước; chọn **Đứng yên** để căn lại trang phục.

## Texture

**Y → CÀI ĐẶT → Bề mặt vải** áp 6 mẫu lên trang phục mẫu. Web ngoại tuyến mở bằng `Open_Texture_Web.bat`, có 12 mẫu để xem màu/normal/roughness. Web và game chưa điều khiển trực tiếp lẫn nhau. Texture chỉ đổi bề mặt, không suy ra sức bền đứt/rách.

Xem [báo cáo cập nhật](../Verification/Bimanual/REPORT.md). Kiểm thử phần mềm và mesh không thay thế việc thử lại bằng kính Meta thật.
