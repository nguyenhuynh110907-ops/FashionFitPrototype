# Verification report — v0.8.0 hi-res cloth

Ngày kiểm tra: 2026-10-26.

## Đã kiểm tra tự động

- Compile toàn bộ runtime C# bằng Roslyn và assemblies của Unity 2022.3.62f1: đạt. Hai cảnh báo CS8032 chỉ xuất hiện do chạy source generator Unity ngoài Editor; không có lỗi source.
- Chạy nguồn solver/geometry thật với math substitutes: đạt 99 checks.
- 1.920 bước cloth, tương đương 16 giây mô phỏng kiểm tra.
- Năm preset: Atelier Midi, Emerald Column, Rose Couture, Sapphire Cocktail và Ivory Shirt Set.
- Mỗi preset có kiểm tra kéo/thả VR, hữu hạn sau khi thả, trọng lực và reset xác định.
- Mỗi váy dùng 2.112 điểm PBD; áo Ivory dùng thêm 1.152 điểm PBD ở runtime.
- Max constraint stretch ghi nhận: 1.445675.
- Điểm thấp nhất: Y = 0.09482867, cao hơn sàn solver Y = 0.012.

Kết quả máy đọc nằm trong `Numerical_Checks.json`.

## Chưa thể xác nhận tự động

- Render/shader/UI trong một phiên Unity Editor có license.
- Frame time CPU/GPU thực tế.
- Tracking, controller mapping và cảm giác sử dụng trên kính thật.
- Mọi animation hoặc OBJ tùy ý ngoài ba thiết kế procedural.
- Self-collision đầy đủ và fitting có độ chính xác sản xuất.

Thực hiện `VR_EXPERIMENT_SETUP.md` trước khi đánh dấu bản build cho một thiết bị cụ thể.
