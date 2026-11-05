# Body Fit 0.8.2 — 2026-10-27

## Phạm vi thực hiện

Đã tải bốn ảnh tham chiếu vào `References/FitStudy/Images`, ghi nguồn và quyền trong `References/FitStudy/README.md`. Đã quan sát ảnh đó và render trực tiếp cả năm trang phục bằng Unity trước/sau để hiệu chỉnh phom. Không tải bộ scan 4D-DRESS cần đăng ký và không huấn luyện mạng AI trong lượt này.

Đã đo 38 tiết diện ngang, mỗi tiết diện 64 hướng từ body kỹ thuật của dự án. Bộ đo loại các tam giác chịu trọng số tay lớn và dùng bao lồi để đóng khoảng trống ở vai/hai chân. Kết quả được bake thành `TechnicalBodyFitData.cs`, runtime chỉ nội suy bảng nhỏ.

## Các sửa đổi chính

- Eo tự nhiên chuyển từ mốc hard-code 1,045 m sang 1,12 m, tương ứng vị trí eo hẹp trong dữ liệu body đã đo.
- Phom dùng đường bao theo góc và tâm trước/sau thay đổi theo độ cao, thay các ống ellipse phóng lớn ở ngực/vai.
- Ba thân đầm mới có mép cổ thấp, vùng vai/tay được mở đúng thiết kế; pháp tuyến được đảo ra ngoài cho các hàng mesh đi từ dưới lên.
- Thân áo và chân váy dùng cùng đường eo, chồng nhẹ 8 mm; blend skinning theo khoảng chiều cao mới.
- Giữ độ ôm eo/hông cho Emerald và Ivory; nới và giữ thể tích chân váy riêng cho Rose, Sapphire, Atelier. Không tăng mật độ physics so với 0.8.1.
- Áo Ivory dùng tiết diện body cho cổ, ngực, eo; lỗ tay được thu gọn chiều sâu và phần thân trên có hỗ trợ mềm. Vùng đang được Grip vẫn có thể kéo. Áo sáng và chân váy tối dùng material riêng để đọc rõ hai món.
- Đường căn OBJ dùng mốc mới, xử lý tâm trước/sau ở eo và hông riêng. Thêm dung sai 10 micromet khi chọn tiết diện để phép đổi m/cm không làm mất một hàng mesh ở biên.

## Kết quả fit ở tư thế đứng

Các số đo lấy từ mesh render sau 40 bước settle; không phải số ước lượng từ ảnh chụp.

| Mẫu | Độ dư hướng kính eo | Bề rộng gấu | Chồng đường nối bodice–váy | Pháp tuyến bodice hướng ra ngoài |
| --- | --- | --- | --- | --- |
| Atelier | 12 mm | 60,0 cm | Không có bodice | Không áp dụng |
| Emerald | 12 mm | 50,1 cm | 8 mm | 100% |
| Rose | 12 mm | 84,1 cm | 8 mm | 100% |
| Sapphire | 12 mm | 64,4 cm | 8 mm | 100% |
| Ivory | 12 mm | 50,5 cm | Áo riêng | Không áp dụng |

Độ dư hướng kính không phải độ dư chu vi. Chu vi vòng eo trang phục trong bài này khoảng 77,7 cm, đường bao body khoảng 70,5 cm. Thông số là lựa chọn prototype, chưa hiệu chuẩn cho một loại vải may thật.

Chi tiết: `FitStudy/After_Fit_Measurements.json`. Ảnh trực quan: `FitStudy/Before_Look*_Front/Side.png` và `FitStudy/After_Look*_Front/Side.png`; ảnh After dùng material Ivory hai màu và pháp tuyến mới, nên khác biệt không chỉ do silhouette.

## Kiểm tra đã chạy

- Unity 2022.3.62f1 biên dịch source và render 20 ảnh trước/sau, mỗi mẫu có góc trước và bên.
- Kiểm tra độ dư eo, hướng normal, chỗ nối và độ khác biệt phom xòe/ôm đã qua; log có `GARMENT_FIT_CAPTURE_OK After`.
- Bài motion Unity dùng rig thật, năm bộ gồm áo Ivory, tịnh tiến/quay mannequin và 450 bước: log có `UNITY_RUNWAY_TRIAL_OK`, 78.608 điều kiện đã qua, **0** điểm động vi phạm proxy ở các trạng thái được đo. Max stretch toàn bộ khoảng **1,30045** lần; sai số độ cao cổ chân pha trụ dưới **0,1 mm**.
- Bài fit độc lập với lớp math thay thế: **36** điều kiện qua, gồm đơn vị m/cm, bảo toàn mesh nguồn, vị trí/tâm eo, hữu hạn và phom xòe khác phom ôm.

Log cuối: `BodyFit-Verification.log`; log render/fit: `Garment-Fit-After.log`. JSON thử số cũ của 0.8.1 được giữ làm lịch sử, không chứng nhận phom 0.8.2.

## Giới hạn còn lại

Đây vẫn là trang phục procedural, chưa phải pattern may có đường chiết, lót và vật liệu được đo thực nghiệm. Các mốc bake chỉ áp dụng body kỹ thuật này. Phép kiểm tra point/proxy không chứng minh toàn bộ tam giác body/vải không giao cắt. Chưa kiểm thử headset trong lượt này. Ảnh Unity đã được quan sát trực tiếp để kiểm tra phom, nhưng cần người dùng đánh giá lại trong Play Mode ở góc máy và chuyển động thực tế của họ.
