# Đi tự do trong phòng bằng Meta Quest

Menu đã cập nhật: **Y trái** mở/ẩn, **X trái** đưa về trước; tab **CÀI ĐẶT** thay cho các nút TURN/WALK riêng của bản trước. [Hướng dẫn giao diện mới](ATELIER_VR_GUIDE.md).

Sau khi vào **Quest Link → Unity Play → VR / CAMERA → Vào VR**, bạn có thể tự đi tới bàn nón, tủ đồ và các góc phòng. Không phải bấm VIEW hoặc đi vòng quanh mannequin mới đổi được vị trí.

| Thao tác trong kính | Kết quả |
|---|---|
| Đẩy cần trái lên / xuống | Tiến / lùi theo hướng đầu đang nhìn |
| Gạt cần trái trái / phải | Đi ngang trái / phải |
| Gạt cần phải trái / phải | Quay người trong game |
| Quay đầu thật | Nhìn quanh độc lập; camera không tự quay lại mannequin |
| Bước, nghiêng người hoặc cúi thật | Camera theo tracking đầu trong vùng chơi thực |
| Menu CÀI ĐẶT → nút Quay | Đổi quay mượt ↔ quay từng nấc 45° |
| Menu CÀI ĐẶT → Tốc độ đi | Đổi tốc độ 0,80 → 1,25 → 1,80 m/s |
| Grip phải | Cầm nón/mẫu đồ; có thể vừa cầm vừa di chuyển |

Mặc định tốc độ đi là **1,25 m/s**, quay mượt **75°/s**. Trước khi vào kính, tab **VR / CAMERA → DI CHUYỂN TỰ DO TRONG KÍNH** cho chỉnh tốc độ 0,5–2,2 m/s và cách quay. Trong kính, mở Y → CÀI ĐẶT để đổi. Nếu chưa quen quay mượt, chọn quay 45°; thả cần phải về giữa rồi gạt lại để quay nấc tiếp theo.

Đẩy cần chỉ di chuyển ngang trên sàn. Ngẩng hoặc cúi nhìn không làm bay lên/xuống. Bước thật vẫn theo tracking, không bị thay bằng chuyển động joystick. Bạn có thể đứng tại chỗ trong phòng thật và dùng cần để đi xa trong phòng ảo.

## Thử nhanh

1. Sau khi vào VR, thả hai cần về giữa một lần.
2. Nhìn về một góc trống, đẩy cần trái: phải đi tới góc đó, không vòng quanh mannequin.
3. Nhìn sang hướng khác rồi đi tiếp; thử đi ngang và lùi.
4. Gạt cần phải; quay bằng đầu vẫn hoạt động độc lập.
5. Đi tới bàn/tủ, Grip một vật rồi đi về mannequin để đội/mặc. Có thể đi vòng ra phía sau mannequin.
6. Trong **CÀI ĐẶT**, thử cách quay và tốc độ đi, rồi đi sát bàn/tủ để kiểm tra vùng chặn.

Di chuyển bằng cần có vùng chặn ở bàn/tủ, tường và sát mannequin. Có thể trượt dọc mép bàn để đi vòng qua; đây không phải khóa camera vào mannequin. Các nút **VIEW** và **TABLE / RACK** chỉ là đường tắt đổi vị trí.

Vùng chặn ảo không giữ được bước chân thật. Tiếp tục dùng boundary/vùng chơi của Meta khi đi thật; nếu bước xuyên vào bàn ảo bằng cơ thể thật, game vẫn ưu tiên tracking đầu thay vì kéo đầu bạn về vị trí khác.

## Nếu cần không phản ứng

- Sau khi vào kính hoặc vừa mất tracking: **thả cả hai cần về giữa**, rồi thử lại. Game chờ thao tác này để tránh tự trôi/quay khi tracking phục hồi.
- Cần trái dùng tay trái, cần phải dùng tay phải; kiểm tra hai controller đã hoạt động trong môi trường Link.
- Trong Unity cần có OpenXR loader Standalone và Oculus Touch Controller Profile. [Hướng dẫn kết nối từng bước](KET_NOI_QUEST_LINK.md).
- Sau cập nhật code, Stop Play rồi Play lại. Không dùng các object cũ từ phiên Play trước khi compile.

[Kết quả kiểm tra phần mềm](../Verification/VRLocomotion/REPORT.md). Chưa có phép đo tracking, độ dễ chịu hoặc FPS trên kính thật.
