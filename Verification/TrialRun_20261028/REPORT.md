# Kết quả chạy thử thực tế — 28/10/2026

Đã chạy Unity và suy luận AI trên máy, không chỉ đọc báo cáo cũ. Kết quả: các mẫu procedural vượt qua kiểm tra hiển thị/chuyển động/proxy trong phạm vi thử, nhưng **chưa đạt để triển khai nhận đồ sinh viên hoặc thực nghiệm headset**. Hai bộ còn mô phỏng sau dừng, chi phí vải còn cao, công cụ kiểm tra file vẫn bỏ lọt lỗi và AI vùng cổ chưa đủ chất lượng.

## Môi trường và phạm vi

- Unity **2022.3.62f1**, Direct3D 11, **AMD Radeon 860M**, batch mode trong `Verification/CloseViewProject`.
- Đã đối chiếu hash toàn bộ runtime C# giữa bản thử và dự án chính; không thay runtime hoặc checkpoint. Hash nguồn tại [runtime_sha256.json](runtime_sha256.json).
- Dùng năm trang phục procedural sẵn có. Chưa có viewer/retarget GLB mới và chưa thử file trang phục của sinh viên. Bài này không chứng minh giữ đúng hình thêu của một asset sinh viên.
- Các phép chạy Unity kết thúc trước suy luận AI và kiểm tra khoảng cách offline; không chạy train AI song song khi ghi số đo Unity.
- Chưa dùng headset, chưa đo GPU/whole-frame/dropped frames trong standalone player.

## Kết quả đã chạy

| Bài thử | Kết quả mới | Phạm vi |
|---|---|---|
| Kiểm tra dữ liệu | passed; 200 graph, 1.002 ảnh hoa văn, 90 asset PBR/599 map | Hash, split, nhãn chưa biết, dữ liệu đầu vào; không phải chất lượng mặc đồ |
| Rig chuyển động và năm bộ đồ | 450 bước, 0 vi phạm proxy | Kiểm tra thực trong Unity, có chuyển/đổi hướng rig; chưa phải CCD tam giác |
| Camera gần | 16/16 góc đạt, không mất thân trên, không đỉnh ngoài bounds | Bốn bộ có thân trên, mỗi bộ bốn hướng; chỉnh hai cực rộng/dài áo giữ cổ trong 2 mm |
| Đi chậm–dừng | 2700 bước đi trên năm bộ; cả năm dừng gait được; 0 vi phạm proxy | Mỗi bộ có warm-up, 540 bước đi và 360 bước sau lệnh dừng |
| Vải về trạng thái nghỉ | 3/5 bộ đạt sau 6 giây; Sapphire và Ivory chưa đạt | Batch hiện có không assert cờ ngủ này, nên marker `SLOW_PRESENTATION_OK` không có nghĩa toàn bộ mục tiêu ổn định đã đạt |
| Tiếp xúc với body gốc | 15 pose / 153,882 mẫu; sâu nhất 0.000 mm | Lấy đỉnh được tham chiếu và tâm mặt; không dùng body che da; không chứng nhận toàn tam giác/chuyển động liên tục |
| AI hoa văn | Suy luận lại từ **101 ảnh nguồn**: accuracy 84.16%, macro-F1 0.736 | Đọc archive, kiểm tra SHA ảnh, tính lại DINO; feature khớp cache tối đa 0. Đây là test cũ, không phải holdout mới |
| AI vùng cấu tạo 3D | Nạp checkpoint và chạy lại 28 graph test: macro-F1 0.218; cổ áo F1 0.092 | Chỉ một thiết kế test có panel cổ; chưa dùng tự sửa/fitting |
| File đầu vào lỗi | Vẫn bỏ lọt 4 trường hợp | Material index, texture index, UV accessor và buffer length sai; hai đối chứng thiếu UV/file buffer bị phát hiện; có thêm đối chứng hình học hợp lệ |

## Thời gian xử lý vải và độ ổn định

Số dưới đây đo CPU trong Unity Editor batch mode, **không phải FPS headset hoặc toàn bộ frame**. Không có p95/p99 vì bài thử hiện chỉ lưu trung bình/cực đại.

| Trang phục | Trung bình ms/bước | Cao nhất ms/bước | Giãn cạnh lớn nhất khi đi chậm | Đã ngủ sau 6 giây dừng |
|---|---:|---:|---:|---|
| AtelierMidi | 15.66 | 36.50 | 1.239 | Có |
| EmeraldColumn | 17.00 | 29.59 | 1.126 | Có |
| RoseCouture | 14.45 | 23.60 | 1.180 | Có |
| SapphireCocktail | 16.15 | 109.37 | 1.211 | Chưa |
| IvoryShirtSet | 25.92 | 39.44 | 1.206 | Chưa |

Ngân sách toàn frame ở 72 Hz là 13,89 ms. Riêng cloth update trung bình đã khoảng **14.5–25.9 ms**, do đó số đo này chưa hỗ trợ kết luận đáp ứng VR 72 Hz. Cần profiling standalone trên headset/phần cứng đích để định lượng chính xác. Spike Sapphire được ghi lại, chưa xác định nguyên nhân bằng trace.

Max stretch là tỉ lệ của **cạnh xấu nhất** với chiều dài nghỉ; không phải toàn bộ vải giãn cùng mức. Chuyển động rig nhanh hơn ở bài riêng có max stretch 1.297. Đây chưa phải kiểm định thông số cơ học cho vải thật.

## Quan sát ảnh mới

Đã xem ảnh [Emerald nhìn gần](CloseViews/Look2_View0.png) và [Ivory nhìn bên](CloseViews/Look5_View1.png). Thân trên hiện diện đúng, nhưng cổ/mép/lỗ tay vẫn có dáng hình học thô và chưa có đường chỉ, nút hay thêu chi tiết của tài sản 3D thật. Kết quả camera/bounds đạt không đồng nghĩa đã đạt yêu cầu thẩm mỹ.

Ảnh đứng/đi mới của cả năm bộ nằm trong [SlowPresentation](SlowPresentation). Xem thêm [ảnh Emerald toàn thân khi đi](SlowPresentation/Look2_WalkingA_Front.png).

## Việc nên sửa sau bài thử

1. Hoàn thiện validator và importer giữ UV/material/normal; kiểm chứng trên khoảng 10 asset 3D thực trước fitting.
2. Điều tra Sapphire/Ivory chưa ngủ sau dừng, thêm assertion trạng thái ổn định và telemetry vận tốc; tránh coi `stopped` của gait là cloth đã đứng yên.
3. Profile chi phí physics/publish/normals, thử tách nhịp render–physics có nội suy, LOD và tránh publish nhiều lần/frame. Đối chiếu va chạm sau từng tối ưu.
4. Mở rộng đánh giá cổ áo bằng nhiều thiết kế độc lập; AI hiện chỉ gợi ý/nhận dạng offline. Giữ nguyên UV/texture gốc là cơ sở bảo toàn hoa văn.

## Bằng chứng

- [Tổng hợp máy đọc được](summary.json)
- [Log Unity chuyển động và camera](motion_close.log), [log đi chậm](slow_presentation.log)
- [Kết quả camera](CloseViews/results.json), [kết quả đi chậm](SlowPresentation/results.json)
- [Khoảng cách body gốc tại 15 pose](SlowPresentation/source_body_contact.json)
- [Suy luận lại AI từ nguồn](model_reevaluation.json), [tái hiện file lỗi](intake_rerun.json), [QA dữ liệu](data_verification.json)

Đợt này chạy kiểm chứng và báo cáo; không huấn luyện lại, không tự thay solver/importer hoặc công bố hết xuyên trong mọi chuyển động.
