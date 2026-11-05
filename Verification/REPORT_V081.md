# Runway Trial 0.8.1 — 2026-10-27

## Thay đổi

- Váy dùng cage 32×21 = 672 điểm thay vì 64×33 = 2.112 điểm; áo dùng 32×12 = 384 thay vì 64×18 = 1.152. Mesh render được chia tam giác riêng, giữ mép xẻ/lỗ tay.
- Fit phom nghỉ và điểm neo trước khi tính chiều dài constraint; không buộc đường may quá chật đấu với collider.
- Đường cong bước có 62% pha trụ, 38% pha vung; toe clearance mục tiêu 55 mm, bob hông 0–6 mm; hai-bone IK cho rig kỹ thuật. Đây là chuyển động tự dựng để thử, chưa phải motion capture.
- Khởi động tăng dần biên độ, dừng sau bước cuối rồi chuyển về đứng. Gravity dùng thời gian thực, độc lập thanh Speed.
- Đổi hệ tọa độ cả vị trí lẫn trạng thái Verlet khi mannequin tịnh tiến/quay; collider và neo được nội suy đúng đoạn ở hai substep.
- Lọc AABB, cache collider ở từng substep, một substep mặc định và hai khi VR/collider nhanh. Kiểm tra đường đi điểm–ellipsoid và conservative advancement điểm–capsule; kiểm tra tâm tam giác mỗi substep; spatial hash cho self-contact điểm–điểm.
- Áo Ivory có lỗ tay; collider tay/thân đã hiệu chỉnh để giảm kẹt vải giữa các proxy chồng nhau.
- Ngủ sau khi chuyển động vải thấp liên tục; UI hiển thị thời gian xử lý vải và số điểm vi phạm proxy nếu có.

## Kết quả Unity thực

Đã chạy `FashionFit.Editor.RunwayTrialVerification.RunBatch` bằng Unity 2022.3.62f1, chế độ `-batchmode -nographics`. Engine tự biên dịch cả runtime và Editor source. Bài thử dùng body/rig thật của project, năm mesh render, áo Ivory, biến đổi tịnh tiến/quay của mannequin và 450 bước simulation.

- Log có `UNITY_RUNWAY_TRIAL_OK`; 78.608 điều kiện đã qua, gồm các kiểm tra hữu hạn trên mesh render.
- Điểm động vi phạm proxy: **0** trong các trạng thái đã đo.
- Độ giãn constraint lớn nhất cả váy và áo: **1,299821 lần**. Đây là chỉ số regression của prototype, không phải thông số strain đã hiệu chuẩn của vải thực.
- Sai số độ cao cổ chân pha trụ lớn nhất: **0,094 mm**.
- Tổng bài thử headless: **7,37 giây**. Đây không phải phép đo FPS render/VR.
- Thời gian physics váy trung bình trong log: khoảng **9,8–11,2 ms/bước**; xử lý mesh hiển thị váy khoảng **2,2–2,5 ms/bước**. Bộ Ivory có thêm chi phí áo. Số đo thay đổi theo tải máy và chế độ Editor.

Chi tiết: `Unity_Runway_Trial.json`, `Unity-Runway-Trial.log`. JSON ghi số kiểm tra hình học trước ba điều kiện chốt; dòng OK trong log ghi tổng số sau cùng.

## Bài kiểm tra số bổ sung

`Runway_Trial_Checks.json` được tạo từ production geometry/gait/solver với lớp vector/math thay thế bên ngoài Unity. Nó kiểm tra pha trụ/vung, đường nối chu kỳ, kéo điểm xuyên ellipsoid/capsule, hữu hạn và tiếp xúc proxy trên năm mẫu. Không dùng JSON cũ của v0.8.0 để chứng nhận bản mới.

## Giới hạn của chứng cứ

Chưa chạy render trực quan hay headset trong lượt này. Chưa đo tự giao cắt tam giác–tam giác hoặc CCD toàn bộ mesh body/render; điểm/proxy bằng 0 không chứng minh mọi tam giác đều không xuyên. Chưa tích hợp solver C-IPC/OGC. Cần thử Play Mode, ghi FPS/thời gian vải và xem chuyển động trước/sau/hai bên theo `RUNWAY_TRIAL_GUIDE.md`.
