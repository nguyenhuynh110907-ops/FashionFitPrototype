# Rà soát và chuẩn bị vào lại Meta Quest Link

## Trạng thái PC và phạm vi

[Dữ liệu runtime PC](pc-runtime.json): OpenXR ActiveRuntime trỏ về manifest của Meta Horizon, manifest tồn tại; OVRService và OVRServer_x64 đang chạy. Đã mở được giao diện Meta Horizon Link. Ứng dụng hiển thị cảnh báo máy chưa đáp ứng cấu hình tối thiểu cập nhật. Người dùng xác nhận **chưa kết nối kính**; chưa có phiên HMD/controller thật, chưa đo FPS/stereo trong kính.

## Đã sửa

1. **Thứ tự tạo menu:** trước đây tạo menu rồi mới đọc pose đầu và quay rig. Menu có thể được đặt từ gốc chưa tracking, sai chiều cao/hướng. Nay đọc pose, quay rig rồi tạo menu; nếu pose chưa tới, ẩn menu và đặt một lần khi pose đầu hợp lệ tới. Sau đó menu vẫn đứng trong không gian, không bám đầu mỗi frame.
2. **Tracking và Grip:** mất tracking đầu hoặc tay phải sẽ tắt tia, kết thúc Grip và xóa hover; các nút Grip/Trigger phải thả trước khi được kích hoạt lại. Nút/cần cũng yêu cầu trạng thái nghỉ lúc vào VR. Mất focus cửa sổ PC nhả vật và latch nút. Các bài này dùng trạng thái tracking giả lập, chưa thay thế sự kiện mất tracking/session focus thật của Quest.
3. **Chuẩn bị phiên thử:** nút mới đặt mannequin đứng yên, nhịp mục tiêu 0,18×, tắt gió, ẩn điểm khớp, đồ họa nhẹ, đi 0,80 m/s và quay 45°. Không tự vào XR. Đưa cụm kết nối/Vào VR lên đầu tab để dễ tìm.
4. **Mở lại một bước:** thêm `Open_Quest_Link_Trial.bat` và menu **Fashion Fit → Prepare Quest Link Trial**, tự Play rồi mở tab VR với preset. Không đổi runtime Windows, không cài SDK mới hoặc sửa cài đặt tài khoản/privacy.

## Kiểm tra

[26 kiểm tra cấu hình/khởi tạo](verification.json), [log cuối](final.log), [hash nguồn](source_snapshot.json).

- OpenXR loader, Touch profile, manual XR startup, D3D11 và Single Pass Instanced đạt kiểm tra.
- Preset đúng và gọi lại không đổi ngược chất lượng; launcher có thể chọn tab VR.
- Menu chờ pose đầu, xuất hiện đúng cao độ: sai số khoảng 0,00000006 m trong bài giả lập. Khoảng cách bảng khoảng 1,3124 m; đặt sau khi quay rig, không đổi vị trí đầu hoặc chạy theo đầu sau đó.
- Grip được giữ khi tracking hợp lệ, được nhả khi mất đầu/tay/focus; latch nút được đặt để yêu cầu nhả nút. Trang khớp và trang nhịp trình diễn vẫn truy cập được.
- Hồi quy: [21 mục di chuyển](movement-verification.json), [34 điểm Grip khớp](pose-verification.json), [nón/tủ đồ](showroom-verification.json), [31 mục menu/tủ](atelier-verification.json).

Các mục đạt ở trên là kiểm tra code/cấu hình và đầu vào tổng hợp. Chưa xác nhận độ cao sàn thực tế, nút vật lý, cáp, hình hai mắt, rung tay, focus runtime hoặc khả năng chạy 72/90 Hz trên máy hiện tại.

Đã mở bản Desktop trong Unity ở tab VR / CAMERA: cụm chuẩn bị/kết nối nằm trên đầu, trạng thái hiển thị đi 0,80 m/s, quay 45°, đồ họa nhẹ và người mẫu đứng yên. [Log phiên chuẩn bị thực tế](live-session.log) ghi `QUEST_LINK_TRIAL_PREPARED`; chưa tự khởi động phiên XR. Kiểm tra log sau lần khởi động này không ghi exception/C# error tại thời điểm rà soát.

## Phần cần người dùng làm

Cắm/bật kính, vào Link trong kính, xác nhận Devices nhận kính rồi bấm Vào VR trong Unity. Chỉ sau đó mới kiểm thử phần cứng. [Hướng dẫn ngắn](../../Docs/VR_REENTRY_GUIDE.md).

[Meta: Link và runtime OpenXR](https://developers.meta.com/vr/documentation/unity/unity-link/) · [Yêu cầu PC](https://www.meta.com/help/quest/140991407990979/).
