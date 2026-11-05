# Hướng dẫn đưa Fashion Unity vào VR

**Bản cập nhật bàn nón/tủ đồ:** bắt đầu bằng [VR_SHOWROOM_QUICK_START.md](VR_SHOWROOM_QUICK_START.md). Tài liệu mới có thao tác Grip, sáu bộ chọn từ tủ, kiểm tra GPU Radeon 860M và menu Link hiện hành. Các số lượng/nút trong tài liệu cũ dưới đây có thể chưa phản ánh bản mới.

## Mục tiêu đầu tiên: PCVR bằng Meta Quest Link

Đây là đường thử ngắn và ít thay đổi project nhất. Unity chạy game trên PC, kính cung cấp hình ảnh, tracking đầu và controller. Chưa cần build APK.

### 1. Chuẩn bị kính và PC

- Meta Quest 2, 3, 3S hoặc Pro.
- Cáp USB-C tốt; nên kiểm tra tốc độ trong Meta Horizon Link.
- Cài và đăng nhập ứng dụng **Meta Horizon Link** trên Windows.
- Giữ vùng chơi trống, bật boundary/guardian và đeo dây controller.

### 2. Chọn Meta làm OpenXR runtime

1. Mở Meta Horizon Link trên PC.
2. Vào `Settings > General`.
3. Tại `OpenXR Runtime`, bấm đặt Meta Horizon Link làm runtime đang hoạt động.
4. Nếu Unity đang mở, đóng và mở lại Unity sau thay đổi này.

### 3. Kết nối Quest Link

1. Cắm headset vào PC.
2. Trong headset: `Settings > Quest Link` và bật Quest Link.
3. Chọn `Launch Quest Link`.
4. Trong Meta Horizon Link: `Devices`, chọn headset và chạy `USB Test` nếu dùng cáp.

### 4. Kiểm tra project Unity

Project đã chứa:

- XR Plug-in Management `4.4.0`.
- OpenXR Plugin `1.11.0`.
- OpenXR Loader cho Standalone Windows.
- Oculus Touch Controller Profile.
- Tracking origin dạng Floor khi vào VR.

Trong Unity có thể kiểm tra lại tại:

`Edit > Project Settings > XR Plug-in Management > PC, Mac & Linux Standalone`

OpenXR phải được đánh dấu. Trong phần OpenXR interaction profiles cần có Oculus Touch Controller Profile.

### 5. Chạy lần đầu

1. Stop Play Mode nếu game đang chạy.
2. Chờ Unity compile xong và Console không có lỗi đỏ.
3. Bấm Play.
4. Mở tab `VR / CAMERA` trong giao diện game.
5. Bấm `Kiểm tra cấu hình OpenXR`.
6. Khi Quest Link đã hoạt động, bấm `Vào VR`.

## Điều khiển trong kính

| Thiết bị | Chức năng |
|---|---|
| Cần trái | Đi theo hướng đầu nhìn |
| Cần phải | Quay người/camera |
| Trigger phải | Bấm bảng điều khiển VR |
| Grip phải | Giữ/kéo mannequin, váy hoặc áo Ivory |

Bảng trong kính có hai hàng:

- `MOTION`, `FIT`, `RESET`, `EXIT`.
- `OUTFIT -`, `OUTFIT +`, `VIEW`, `QUALITY`.

## Bài thử đầu tiên — 10 phút

1. Dùng `OUTFIT +` chuyển qua đủ năm bộ.
2. Chạy motion 30–60 giây mỗi bộ; quan sát eo, hông, đùi, đầu gối và gấu.
3. Bấm `FIT`, kéo gấu váy bằng Grip rồi thả; vải phải tiếp tục rơi và giảm dao động.
4. Chọn Ivory, kéo phần ngực/hem áo; kiểm tra áo 1.152 điểm phản ứng với thân và tay.
5. Grip mannequin, kéo/xoay rồi thả; mannequin phải trở lại sàn.
6. Đi và quay 360°, xác nhận camera theo headset.
7. Ghi FPS thấp nhất, loại headset, runtime, GPU và lỗi Console.

## Xử lý lỗi nhanh

### “Không khởi tạo được OpenXR”

- Kiểm tra OpenXR đã bật trong XR Plug-in Management.
- Đặt Meta Horizon Link làm active OpenXR runtime.
- Stop Play, đóng Unity, mở lại project.

### “Kính chưa sẵn sàng”

- Đeo kính và Launch Quest Link trước khi bấm `Vào VR`.
- Kiểm tra headset xuất hiện trong Meta Horizon Link > Devices.
- Thử USB Test hoặc đổi cáp/cổng USB.

### Có hình nhưng controller không hoạt động

- Kiểm tra Oculus Touch Controller Profile trong OpenXR.
- Thoát Play Mode, reconnect controller/headset rồi chạy lại.
- Đảm bảo cửa sổ Game đang focus.

### Giật hoặc FPS thấp

- Bấm `QUALITY` trong kính hoặc chọn `Đồ họa nhẹ` trên desktop.
- Đóng ứng dụng nền, giảm độ phân giải Game view.
- Thử bằng cáp trước Air Link.
- Lưới hiện có 2.112 điểm mỗi váy và 1.152 điểm áo Ivory; máy yếu có thể cần chế độ nhẹ.

## Native Quest APK — giai đoạn sau

Bản hiện tại ưu tiên PCVR/Quest Link. Trước khi build APK standalone cần chuyển platform Android, cài Android Build Support/SDK/NDK, kiểm tra lại phiên bản Unity/OpenXR theo yêu cầu Meta hiện hành, tối ưu shader và đo trực tiếp trên headset. Không nên coi kết quả PCVR là bằng chứng hiệu năng native Quest.

## Tiêu chí đạt prototype

- Không có lỗi đỏ, NaN hoặc mesh biến mất.
- Vải không rung tăng dần và không xuyên proxy rõ rệt trong motion mặc định.
- Sau khi thả Grip, vải chịu trọng lực và settle.
- Tất cả nút VR bấm được bằng Trigger.
- FPS ổn định ở mức phù hợp với headset thử nghiệm.

Tài liệu chính thức:

- Meta Quest Link: https://developers.meta.com/vr/documentation/unity/unity-link/
- Unity/OpenXR trên Meta: https://developers.meta.com/vr/documentation/unity/unity-and-openxr-compatibility/
