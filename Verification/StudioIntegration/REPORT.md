# FashionFit Studio — V089, 02/11/2026

Đã tích hợp trực tiếp bộ quy trình tinh gọn: **6 vai trò / 3 workflow**, lệnh kiểm tra chung và công cụ ghi số liệu tùy chọn. Đây là bản áp dụng riêng từ Claude Code Game Studios commit `05ad7c8`, có MIT và attribution. Không chạy 49 AI đồng thời, không cài Claude CLI hoặc gói trả phí, không huấn luyện thêm mô hình.

[Hướng dẫn mở game, ghi phiên VR và đọc số liệu](../../Docs/STUDIO_WORKFLOW.md).

## Thay đổi

- `AGENTS.md`, `CLAUDE.md`, `.claude/agents`, `.claude/skills`, cấu hình và tài liệu theo Unity 2022.3.62f1 / OpenXR hiện có. Vai trò làm việc tuần tự; không tự nâng engine/render pipeline hoặc chuyển toàn bộ Resources.
- `Verify_FashionFit.bat`: một lệnh tạo run riêng, build và kiểm tra. Đọc báo cáo mới theo timestamp/SHA; thiếu điều kiện không nhận đạt. Mỗi run lưu manifest source, player và bằng chứng.
- `Record_VR_Performance.bat`: mở player để chính người dùng thao tác; thu CSV và JSON. Không tiêm input ở chế độ này. Có kiểm tra game cũ đang chạy; không đóng thay người dùng.
- `StudioTelemetry`: wall/CPU/GPU khi nền tảng cung cấp, percentile, số frame camera render/focus/XR/tracking; bộ đếm GC chưa có dữ liệu được ghi NOT_ASSESSED. Bộ thu chỉ được tạo khi có cờ launch. Build mới bật hỗ trợ Frame Timing Statistics.
- `StudioRenderedTrial`: kịch bản Ivory/nón đỏ với solver và PhysX cập nhật tự động qua frame. Đợt này giữ hình học và mô hình vật liệu V088; chưa tuyên bố tăng FPS hay thêm FEM.

## Kết quả kiểm tra cuối cùng

Lệnh đầy đủ đã thực thi tại project thử đồng bộ source/resources. Run: `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-02\StudioIntegration\ReleaseRuns\20261102T204838990048Z`.
Build: 0 lỗi, 2 cảnh báo optional OpenXR. Tất cả **13 giai đoạn** sau đạt:

| Giai đoạn | Trạng thái | Số mục/trạng thái |
|---|---|---:|
| evidence_gate | passed | — |
| vr_config | passed | 37 |
| bimanual | passed | 91 |
| build | passed | 0 |
| surface | passed | 28 |
| surface_contact | passed | 8 |
| pose_hat | passed | 29 |
| rendered_trial | passed | 17 |
| capture_analysis | passed | 0 |
| mens | passed | 55 |
| mens_contact | passed | 20 |
| layers | passed | 18 |
| layers_contact | passed | 5 |

`evidence_gate` có **6 bài kiểm tra**: file thiếu, báo cáo xanh cũ, tiến trình lỗi, dependency thiếu và trạng thái đang chạy không bị kết luận đạt sớm. `capture_analysis` kiểm tra độc lập số frame, tình trạng counter và percentile giữa CSV/JSON.

Va chạm độc lập: **8 trạng thái Ivory + 20 trạng thái bộ nam + 5 trạng thái áo dài**, không có điểm mẫu body contact sâu hơn 1 mm trong các trường hợp xuất; các cặp tà/quần được kiểm tra giao cạnh–tam giác. Đây là kiểm tra trạng thái lấy mẫu, chưa chứng nhận mọi tư thế, tự va chạm hoặc CCD liên tục. Bộ grip hai tay 91 mục và pose/nón 29 mục vẫn đạt.

## Thử Ivory và nón đỏ qua frame

| Quan sát | Kết quả |
|---|---:|
| Điểm áo Ivory dịch khi kéo | 4.998 cm |
| Sai lệch còn lại sau 3,2 giây thả | 1.760 mm |
| Cổ tay pose dịch theo thao tác | 12.000 cm |
| Khoảng hai điểm nón tăng | 5.000 cm |
| Khoảng hai điểm nón giảm | 5.000 cm |
| Rơi sau nhấc khỏi đầu và thả | 1.765 m |
| Cặp lực lệnh đối nhau; thả một tay giữ tay còn lại | Đạt |

Các số cm ở đây là kết quả kịch bản điều khiển, không phải hiệu chuẩn sức bền cotton. Các preset, lực ảo và mô hình hồi chưa được fit mẫu đo thực nghiệm.

## Đo hiệu năng có camera render

Máy: AMD Radeon(TM) 860M Graphics; cửa sổ 1280×900, vSync=1, targetFrameRate=60. Thu **1241 frame**, camera xác nhận **1241**, thời gian 13.24 giây sau warmup 1.0 giây. Có input tiêm cho bài thử, chưa có XR.

| Metric | p50 | p95 | p99 | Đơn vị |
|---|---:|---:|---:|---|
| Wall/rendered frame | 11.42 | 26.73 | 41.85 | ms |
| CPU main thread | 9.76 | 21.99 | 37.01 | ms |
| GPU (944 mẫu) | 3.06 | 7.65 | 8.78 | ms |

GC allocation: **NOT_ASSESSED**, không hiểu các trường 0 là không cấp phát. Last pose-step p95 khoảng 10.77 ms là giá trị lần chạy solver gần nhất, có thể lặp ở nhiều frame; chưa phải chi phí tổng riêng của một frame.

| Giai đoạn bài thử | Frame camera vẽ | Wall p95, ms |
|---|---:|---:|
| ivory_neutral | 166 | 41.85 |
| ivory_pull | 116 | 41.27 |
| ivory_recovery | 318 | 24.84 |
| ivory_pose | 99 | 25.34 |
| red_hat_stretch | 133 | 25.54 |
| red_hat_compression | 147 | 24.75 |
| red_hat_equip_detach | 156 | 18.05 |
| red_hat_gravity | 106 | 18.15 |

**Chưa đạt mục tiêu 72 Hz ổn định** trong lượt desktop này: p95 frame cao hơn 13,89 ms; 470 / 1241 lượt vượt ngân sách đề xuất. Chưa được quy toàn bộ chênh lệch cho solver: wall còn chứa pacing/chờ, CPU có cả xử lý test/UI, timing GPU có độ trễ. Số đo gợi ý ưu tiên phân tích CPU/pose trước khi tăng mật độ lưới, nhưng cần Unity Profiler theo marker để xác định nguyên nhân cụ thể.

Hai lượt sơ bộ được giữ trong thư mục nghiên cứu: cửa sổ ẩn cho số liệu không đại diện render; lượt hiện tiếp theo phát hiện fixture teleport còn trạng thái nội suy Rigidbody cũ. Đã sửa fixture đặt nón, thêm xác nhận camera, rồi chạy lại toàn bộ; không dùng báo cáo xanh cũ thay cho lần lỗi.

## Chưa đánh giá và bước tiếp

- **Physical HMD/Touch acceptance: NOT_ASSESSED.** Có kiểm tra phần mềm OpenXR, chưa cầm kính/tay thật trong đợt này.
- Chưa ghi phiên 10 phút Quest Link, chưa có compositor dropped frames/ASW/độ trễ toàn chuỗi.
- Chưa hiệu chuẩn vật liệu, chưa mô phỏng đứt/rách hoặc cải thiện toàn bộ chi tiết trang phục bằng dữ liệu mới.
- Bước tiếp: người dùng mở shortcut ghi hiệu năng, vào VR và thực hiện các thao tác trong guide. Dựa trên capture có XR rồi tối ưu đoạn CPU thực sự gây trễ và kiểm tra lại tiếp xúc sau mỗi thay đổi.

## Cài đặt

Source: `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`. Player: `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V089_Studio\FashionFittingRoom.exe`. Giữ V088, cài cạnh bản trước. Kết quả đối chiếu đường dẫn đã cài được bổ sung trong `deployment.json` và `actual_trial/`.


## Kiểm tra lại đúng bản đã cài

Đã chạy chính `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V089_Studio\FashionFittingRoom.exe`: **28 kiểm tra surface/readout, 8 trạng thái contact độc lập, 17 kiểm tra Ivory/nón đỏ qua frame đều đạt**; đối chiếu CSV/JSON đạt. Lượt này camera xác nhận 864 frame, wall p95 44.43 ms. Đây tiếp tục là desktop có input tiêm, chưa thử bằng kính thật. Hai launcher game/VR và công cụ ghi hiệu năng `--check` đạt. Toàn bộ source đã giao và 175 tệp player được đối chiếu SHA256. Xem `actual_checks.json`, `actual_surface.json`, `actual_contact.json`, `actual_trial/`.

Log native OpenXR ở lượt desktop đã cài có `XR_ERROR_FORM_FACTOR_UNAVAILABLE`; phần khởi tạo HMD chưa được kiểm chứng. Các kết quả đạt bên trên là kiểm tra desktop và input tiêm, không xác nhận Quest Link đã kết nối. Khi ghi phiên thật, cần vào Link trước và kiểm tra số frame XR/tracking trong capture.
