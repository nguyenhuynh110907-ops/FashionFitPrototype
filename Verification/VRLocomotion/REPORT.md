# Di chuyển tự do trong kính Meta — 28/10/2026

Đã sửa trực tiếp đường điều khiển VR. [Cách dùng](../../Docs/VR_FREE_MOVEMENT.md).

## Hành vi runtime

- Cần trái đi trên mặt sàn theo yaw của headset, gồm tiến/lùi/ngang, không gọi camera nhìn mannequin. Giữ cách đi desktop đang có.
- Quay bằng cần phải quanh vị trí đầu hiện tại, bảo toàn offset tracking khi người chơi đứng lệch tâm rig. Quay mượt mặc định 75°/s hoặc quay 45° từng nấc có chốt chống lặp khi giữ cần.
- Tốc độ mặc định 1,25 m/s; thanh chỉnh trước VR 0,5–2,2 m/s; nút WALK trong kính đổi 0,80/1,25/1,80 m/s. Nút TURN đổi kiểu quay. Bảng VR có 12 nút.
- Vùng chết cần trái 0,18, quay mượt 0,20; chuẩn hóa đi chéo. Khi mất tracking đầu, dừng di chuyển do cần và yêu cầu đưa cả hai cần về giữa trước khi tiếp tục.
- Di chuyển bằng cần chia thành bước kiểm tra nhỏ tối đa 5 cm, có vùng chặn bàn/tủ, mép phòng ±3,6 m và vùng sát mannequin 0,45 m. Cho trượt dọc biên thay vì quay quanh mannequin.
- Đổi điểm quan sát không đưa đầu vào bàn; điểm bên trái được chuyển sang khoảng trống giữa bàn và tủ. Khi quay/đi, không sửa pose local được tracking của camera.

Không khóa chuyển động cơ thể thật vào collider: room-scale vẫn theo vị trí HMD. Vùng chặn nói trên chỉ áp dụng dịch chuyển bằng cần/nút. Không có bay hoặc thay đổi độ cao giả lập bằng joystick.

## Kiểm tra

[verification.json](verification.json), [log Unity](unity.log), [hash nguồn](source_snapshot.json).

Đã chạy 21 kiểm tra, 2.044 bước mô phỏng qua chính hàm runtime `StepVRMovement`, với pose và stick được đưa vào bằng công cụ Editor. Không khởi tạo headset giả hoặc coi đây là kiểm tra Meta Link thực tế.

| Trường hợp | Kết quả |
|---|---|
| Tiến theo đầu quay 90°, đi ngang | Đạt |
| Bảo toàn pose local và không tự nhìn mannequin | Đạt |
| Đi chéo không tăng tốc | Đạt |
| Cần trong vùng chết không trôi/quay | Đạt |
| Quay mượt đúng 75° sau 1 giây | Đạt; sai số góc đo được 0° |
| Quay khi đầu lệch tâm rig | Đầu dịch ngoài ý muốn khoảng 0,0019 mm do số thực |
| Giữ snap turn không quay lặp | Đạt |
| Thay đổi vị trí local HMD khi không bấm cần | Rig giữ nguyên; camera đi theo pose được đưa vào |
| Bàn, tủ, tường, mannequin | Chặn đúng trong các đường thử |
| Đi chéo dọc mép bàn | Trượt được theo mép |
| Mất tracking / phục hồi khi cần còn bị đẩy | Dừng; phải đưa cần về giữa |
| Cùng đường đi ở 30/60/90 Hz | Sai khác vị trí lớn nhất khoảng 0,0043 mm |
| Bốn điểm VIEW | Không nằm trong vùng chặn |
| TURN, WALK và bảng 12 nút | Đạt |

Chưa kiểm tra bằng Quest/controller thật, chưa đo độ trễ, mức thoải mái, mất tracking thực tế hoặc FPS trong kính. Các bài thử không chứng minh mọi tình huống va chạm liên tục. Không bổ sung solver vải hoặc đổi hình học nón trong đợt này.
