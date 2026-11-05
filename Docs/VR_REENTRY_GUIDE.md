# Đưa kính Meta vào lại — bản Desktop mới nhất

Project: `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`.

## Mở sẵn phiên thử

Nhấp đúp **Open_Quest_Link_Trial.bat** trong thư mục project. File này mở Meta Horizon Link và Unity, Play rồi đưa tới tab **VR / CAMERA** với phiên thử đã chuẩn bị. Nếu Unity đã mở, chọn menu trên cùng **Fashion Fit → Prepare Quest Link Trial**.

Preset thử ban đầu: mannequin mặc đồ đứng yên, tắt gió, ẩn điểm khớp, đồ họa nhẹ, tốc độ đi người chơi 0,80 m/s và quay từng nấc 45°. Preset không tự khởi động phiên XR. Trong Game cũng có nút **Chuẩn bị phiên VR · đứng yên / đồ họa nhẹ**.

## Ba bước bạn làm với kính

1. **Trên PC:** bật kính và cắm cáp có truyền dữ liệu vào PC, mở **Meta Horizon Link → Devices**, xác nhận kính được nhận. Chạy **USB Test** nếu có. Runtime trên PC đã được kiểm tra đang trỏ về Meta; chưa cần đổi lại nếu ứng dụng vẫn báo Meta active.
2. **Trong kính:** mở **Settings / Quick Settings → Quest Link / Link**, chọn PC → **Launch**. Phải vào được môi trường Link trước khi mở VR của game. Tên mục có thể khác theo phiên bản giao diện.
3. **Trong Unity:** ở tab **VR / CAMERA**, bấm **Vào VR · OpenXR / Quest Link**. Đeo lại kính và thả cả hai cần/nút về trạng thái nghỉ rồi bắt đầu thao tác.

Quy trình runtime và Link dựa trên [tài liệu Meta](https://developers.meta.com/vr/documentation/unity/unity-link/). Đây là PCVR; chưa cần xuất APK.

## Thử theo thứ tự

| Thử | Điều khiển / kết quả cần thấy |
|---|---|
| Nhìn quanh | Quay đầu, camera phải theo đầu; chiều cao nhìn phù hợp |
| Menu | Y trái mở/ẩn; X trái đưa menu về trước |
| Bấm nút | Tia tay phải + Trigger; nút đổi màu khi trỏ trúng |
| Đi/quay | Cần trái đi; cần phải quay 45°; thả về giữa trước lần gạt tiếp |
| Nón / đồ | Grip phải giữ vật, thả để đặt; thả gần đầu/thân mannequin để đội/mặc |
| Khớp hai mẫu | CÀI ĐẶT → Mẫu nam/nữ → nhìn gần; Grip điểm xanh, thả để giữ tư thế |
| Nhịp trình diễn | Kiểm tra đồ khi đứng yên trước, rồi THỬ ĐỒ → Nhịp trình diễn → 0,18× / 0,35× / 0,50× |
| Thoát | CÀI ĐẶT → Thoát VR; nếu không điều khiển được thì Stop Play trên PC |

**Trigger** là nút bấm phía trước; **Grip** là nút bóp bên hông. Hai mẫu POSE STUDIO hiện chưa mặc các bộ đầm. Nhịp trình diễn là của mannequin đang mặc đồ; tốc độ đi người chơi là điều khiển riêng.

## Nếu chưa vào được

- **Devices chưa nhận kính:** kiểm tra cáp dữ liệu/cổng USB và mở Link trong kính trước. Unity chưa thể nhận HMD khi Link chưa hoạt động.
- **Game báo kính chưa sẵn sàng:** kiểm tra môi trường Link, runtime Meta rồi thử Vào VR lại; Stop/Play nếu vừa đổi runtime hoặc code.
- **Không có tia / không bấm được:** kiểm tra tay phải đang tracking; thả Grip/Trigger rồi bóp lại. Khi mất tracking đầu hoặc tay, game nhả vật và yêu cầu thả nút trước khi tương tác lại.
- **Menu ở ngoài tầm nhìn:** nhìn về khoảng trống rồi bấm X trái. Menu chỉ được đặt sau khi có pose đầu, không đặt sẵn ở gốc sàn.
- **Giật:** giữ preset nhẹ và mannequin đứng yên để kiểm tra kết nối trước. Chưa có số đo FPS qua kính.

Meta Horizon Link đã hiển thị thông báo PC chưa đáp ứng cấu hình tối thiểu cập nhật. Việc cấu hình project đúng không bảo đảm Link hoạt động/mượt trên máy này. Nếu Link từ chối PC hoặc không vào được môi trường PCVR, cần giải quyết phần PC/Link trước. [Yêu cầu PC chính thức](https://www.meta.com/help/quest/140991407990979/).

[Báo cáo chuẩn bị](../Verification/VRReady/REPORT.md) · [Tủ/menu](ATELIER_VR_GUIDE.md) · [Khớp nam/nữ](POSE_STUDIO_GUIDE.md) · [Kiểm tra vải/tốc độ](SPEED_CONTACT_GUIDE.md).
