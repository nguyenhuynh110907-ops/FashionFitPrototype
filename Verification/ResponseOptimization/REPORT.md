# V090 — tối ưu phản hồi và dọn build trùng

Ngày 02/11/2026. Source sửa trực tiếp: `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`. Player mới: `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V090_Response\FashionFittingRoom.exe`.
[Hướng dẫn mở và phạm vi dọn tệp](../../Docs/RESPONSE_OPTIMIZATION_GUIDE.md).

## Thay đổi

- Giữ 3 vòng ràng buộc XPBD, 8 vòng tiếp xúc mặt và các mẫu cạnh/đỉnh; không giảm lưới hoặc độ dày bảo vệ. Cache chỉ dùng khi ID mặt và tọa độ đầu vào **giống hệt** trong cùng một job; không mang kết quả cũ sang frame khác. Các phép chiếu có skin/field/capsule bất biến trong job nên đầu vào giống hệt cho cùng kết quả.
- Tính RMS/đỉnh strain và lực màng trong job Burst hiện có, giữ thứ tự cạnh, ngưỡng, tỷ số phom ban đầu và trọng số. Lần đầu vẫn chạy phép đọc managed để đồng bộ tham chiếu. Không sửa modulus, lực ảo hoặc định luật hồi.
- Lọc tam giác chắc chắn ngoài vùng với tới trước khi tính điểm gần nhất. Bao local dùng ảnh ngược của cầu world qua matrix, có dư sai số nhỏ; vẫn tìm điểm trên tam giác thật, không thay bằng các đỉnh thưa hoặc cho phép grip xa hơn.
- Khi đang giữ áo hoặc khớp pose: cập nhật solver theo từng frame render. Khi hồi/không tương tác: giữ lịch tối đa 30 Hz trước đây và cơ chế ngủ. Phần nhập Touch/tay/vật vẫn cập nhật mỗi frame; chỉ định dạng nhãn chữ được giới hạn 10 Hz.
- Thêm bộ đếm lấy **một mẫu mỗi lượt solver hoàn tất**, tách thời gian giải và xuất mesh; không lấy giá trị lặp qua frame để giả làm nhiều lần đo.
- README còn một điểm bắt đầu rõ ràng; toàn bộ README/liên kết nghiên cứu cũ được giữ trong `Docs/HISTORY_README_V089.md`. Source, model, texture gốc và dữ liệu huấn luyện không bị di chuyển/xóa trong đợt dọn.

## So sánh trước/sau

Cùng máy AMD Radeon 860M, cửa sổ 1280×900 có camera render, cùng chuỗi Ivory/nón đỏ và thời lượng theo giai đoạn. Nền: 1222 frame; mới: 1270 frame. Đây là một cặp phép đo desktop có input tiêm; các percentile có dao động theo tải máy/vSync, không phải độ trễ Touch→màn kính.

| Metric, ms | Nền V089 có đồng hồ đo | V090 | Giảm trong lượt đo |
|---|---:|---:|---:|
| Giải vật lý mỗi lượt, trung bình | 7.07 | 4.63 | 34.5% |
| Xuất mesh/chỉ số mỗi lượt, trung bình | 2.67 | 1.52 | 43.0% |
| Wall frame p95 | 27.26 | 21.74 | 20.2% |

CPU main p95: 22.96 → 19.00 ms. GPU p95: 7.94 → 6.51 ms. GC allocation vẫn **NOT_ASSESSED** ở release build này. Chưa đạt mục tiêu 72 Hz ổn định; chưa ghi phiên kính thật hoặc đo compositor/ASW.

Nhánh tìm grip: 660 truy vấn đối chiếu cùng tọa độ/đỉnh với cách quét cũ, tổng 1117.98 → 63.89 ms trong Editor, khoảng 17.5× cho riêng lô phép tìm. Không gọi đây là toàn bộ độ trễ tay nhanh lên từng đó lần.

## Phản hồi tương tác

- Kéo áo: **108/108** frame có cập nhật solver, điểm áo dịch 4.997 cm; sau 3,2 giây thả còn lệch 1.429 mm.
- Pose cổ tay: **77/77** frame có cập nhật solver, di chuyển 12.00 cm.
- Nón đỏ: hai tay kéo/nén khoảng 5.000/5.000 cm; cặp lực lệnh đối nhau; thả một tay giữ được tay còn lại; đội/nhấc/rơi theo trọng lực đạt.

## Kiểm chứng

15 giai đoạn của lệnh kiểm tra chung đều đạt:

| Giai đoạn | Trạng thái | Số kiểm tra/trạng thái |
|---|---|---:|
| evidence_gate | passed | — |
| vr_config | passed | 37 |
| picking_equivalence | passed | 660 |
| readout_equivalence | passed | 168 |
| bimanual | passed | 91 |
| build | passed | 0 |
| surface | passed | 28 |
| surface_contact | passed | 8 |
| pose_hat | passed | 29 |
| rendered_trial | passed | 19 |
| capture_analysis | passed | 0 |
| mens | passed | 55 |
| mens_contact | passed | 20 |
| layers | passed | 18 |
| layers_contact | passed | 5 |

660 đối chiếu tìm điểm gồm các phép biến đổi quay, scale không đều và scale âm. Chỉ số native: **168 kiểm tra / 24 trạng thái / 4 preset / nam và nữ**, sai số strain lớn nhất 2.09e-07, lực màng 2.15e-06 N/m.

Quan trọng: **8/8 bộ mesh body/cloth xuất trước–sau ở chuỗi bước thủ công giống hệt từng byte**, gồm neutral/kéo/hồi/pose nam/nữ. Các bài contact độc lập của build mới đạt **8 Ivory + 20 bộ nam + 5 áo dài** trạng thái, không có điểm mẫu sâu hơn 1 mm; các cặp tà/quần đã thử không giao cắt cạnh–tam giác. Không chứng nhận mọi tư thế/tự va chạm hoặc CCD liên tục.

Build 0 lỗi, 2 cảnh báo optional OpenXR. Các thông số sức bền vẫn chưa hiệu chuẩn; tối ưu không biến chúng thành số đo vật liệu thật.

## Dọn tệp

Đã xóa **1403 tệp build sinh lại được**, **1,154,649,626 byte = 1.075 GiB**, trong 8 thư mục làm việc trùng. Trước xóa: kiểm tra đường dẫn tuyệt đối nằm trong workspace; không có reparse/junction; đối chiếu SHA256 mọi file runtime với bản giữ; từ chối nếu có file lạ hoặc game đang chạy tại nơi định xóa. Các symbol Burst `DoNotShip` đi kèm bản build thừa cũng được loại bỏ.

- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-01\LayeredGarment\Player` → bản giữ: `C:\Users\HUYNH\Downloads\FashionFit_Run`.
- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-01\HollisterMens\Player` → bản giữ: `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V087_Mens`.
- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-02\SurfacePolish\Player` → bản giữ: `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V088_Surface`.
- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-02\StudioIntegration\ReleaseRuns\20261102T204838990048Z\Player` → bản giữ: `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V089_Studio`.
- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\Bimanual_Work\BeforePlayer` → bản giữ: `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\VRHands_Work\Player`.
- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\GarmentDoffing_Work\BeforePlayer` → bản giữ: `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\Bimanual_Work\Player`.
- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-01\PoseMaterial\BeforePlayer` → bản giữ: `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\GarmentDoffing_Work\Player`.
- `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-01\LayeredGarment\BeforePlayer` → bản giữ: `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-01\PoseMaterial\Player`.

Giữ nguyên project chính `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`, toàn bộ AI/dữ liệu huấn luyện/checkpoint/reference/sample, QA dependencies, project Unity thử và các bản đã cài V089/V088. Mỗi thư mục cha có dấu chỉ dẫn `REMOVED_DUPLICATE_*.txt`; `cleanup_plan.json` giữ danh sách/hash và nơi giữ bản giống hệt. Dọn tệp không được dùng để giải thích trực tiếp mức giảm thời gian solver.

## Giới hạn và bước kiểm tra tiếp

Chưa thao tác kính Meta/Touch thật trong đợt này. Vào Quest Link trước, mở game V090 hoặc shortcut ghi hiệu năng 10 phút. Các số frame/XR/tracking và thời gian app phải được xem riêng với cảm giác điều khiển. Kết quả kiểm tra lại đường dẫn đã cài sẽ được ghi ở `actual_*` / `deployment.json`.


## Kiểm tra lại đúng bản đã cài

Đã chạy chính `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V090_Response\FashionFittingRoom.exe`: **28 kiểm tra surface/readout, 8 trạng thái contact độc lập, 19 kiểm tra Ivory/nón đỏ qua frame đều đạt**; đối chiếu CSV/JSON đạt. Lượt này camera xác nhận 1206 frame, wall p95 23.66 ms. Đây tiếp tục là desktop có input tiêm, chưa thử bằng kính thật. Hai launcher game/VR và công cụ ghi hiệu năng `--check` đạt. Toàn bộ source đã giao và 175 tệp player được đối chiếu SHA256. Xem `actual_checks.json`, `actual_surface.json`, `actual_contact.json`, `actual_trial/`.


## Hoàn tất dọn các build thử của đợt tối ưu

Sau khi EXE đã cài được xác minh, đã dọn thêm 528 tệp trong 3 build tạm do chính đợt này tạo. Các tệp giống đã có ở V089/V090 giữ nguyên tại đó; mọi phần riêng của build thử được giữ đúng một lần trong archive SHA256 đã kiểm tra, chỉ 562,825 byte: `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\_Luu_tru\Phat_trien_2026-11-02\ResponseOptimization\UniqueBenchmarkArtifacts.zip`. `temp_cleanup_plan.json` có bản đồ khôi phục từng file từ canonical/ZIP. Không mất bản runtime duy nhất hoặc dữ liệu huấn luyện.

Tổng đã dọn **1,931 tệp build trùng/tạm**; dung lượng file được loại bỏ 1,640,291,768 byte, trừ archive phần riêng còn **1,639,728,943 byte ≈ 1.527 GiB**. Đây là tổng của các thao tác dọn, không phải chênh lệch dung lượng trống ổ đĩa trước/sau vì còn build V090 mới và hoạt động hệ thống.

Log native OpenXR có `XR_ERROR_FORM_FACTOR_UNAVAILABLE` trong lượt desktop; kiểm tra này chưa xác nhận khởi tạo HMD/Quest Link. Cần vào Link và thử bằng kính thật.
