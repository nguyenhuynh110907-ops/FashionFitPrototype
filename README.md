# FashionFit — phòng thử đồ Unity / VR

**Bản mã nguồn V092 · Close Fit, grip khớp và bản thử mặc/cởi vật lý.** Unity 2022.3.62f1, Windows x64, kính Meta qua Quest Link.

[Hướng dẫn V091](Docs/CONTACT_DRESSING_GUIDE.md) · [Kết quả và lỗi còn lại](Verification/ContactDressing/REPORT.md). Chế độ mặc/cởi vật lý được bật riêng trong bảng Y.

## Bắt đầu

| Bạn muốn | Mở |
|---|---|
| Chơi trong VR | Desktop → **FashionFit VR → 01 - VAO GAME VR**, hoặc [Open_FashionFit_Model.bat](Open_FashionFit_Model.bat) |
| Ghi hiệu năng khi dùng kính | **04 - GHI HIEU NANG VR**, hoặc [Record_VR_Performance.bat](Record_VR_Performance.bat) |
| Sửa trong Unity | [Open_FashionFit.bat](Open_FashionFit.bat) |
| Kiểm tra tự động | [Verify_FashionFit.bat](Verify_FashionFit.bat) |
| Chọn texture trên web | [Open_Texture_Web.bat](Open_Texture_Web.bat) |

Vào game: **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**. Y mở menu; X đưa menu về trước; cần trái đi, cần phải quay. **VẢI** để kéo trang phục, **POSE** để chỉnh khớp. Grip thường nhấc nón; hai tay kéo/nén; tay cuối thả thì vật chịu trọng lực.

[Kết nối Quest Link](Docs/VR_REENTRY_GUIDE.md) · [Tối ưu và phạm vi dọn tệp](Docs/RESPONSE_OPTIMIZATION_GUIDE.md) · [Mục lục đầy đủ](Docs/INDEX.md)

## Thư mục chính

| Thư mục | Nội dung |
|---|---|
| `Assets`, `Packages`, `ProjectSettings` | Source, model, texture và cấu hình Unity chính |
| `AI`, `Checkpoints`, `.ml-runtime` | Dữ liệu/công cụ/mô hình huấn luyện; giữ nguyên |
| `Reference`, `References`, `Samples` | Tài nguyên tham chiếu và dữ liệu mẫu; giữ nguyên |
| `Docs` | Hướng dẫn và lịch sử |
| `Tools` | Công cụ phát triển, huấn luyện, kiểm tra |
| `Verification` | Báo cáo và bằng chứng kiểm thử |

Game đã build nằm ngoài source tại `C:\Users\HUYNH\Downloads\FashionFit_Run`; launcher chọn bản mới. Các thư mục `*_Work` trong Downloads là công việc kỹ thuật, không phải bản nên mở chơi.

## Phát triển tiếp

- [Chuyển trang phục 3D FBX/GLB/OBJ](Docs/GARMENT_3D_TRANSFER_GUIDE.md)
- [Dữ liệu huấn luyện](AI/ClothData/README.md) và [texture/cơ học vật liệu](AI/MaterialLab/README.md)
- [Áo dài nhiều lớp](Docs/LAYERED_AODAI_VR_GUIDE.md), [hai tay VR](Docs/VR_TWO_HANDS_GUIDE.md), [bảng kéo/nén và đơn vị](Docs/SURFACE_READOUT_VR_GUIDE.md)
- [Quy trình Studio và ghi hiệu năng](Docs/STUDIO_WORKFLOW.md)
- [Lịch sử README và toàn bộ liên kết nghiên cứu cũ](Docs/HISTORY_README_V089.md)

Vật liệu đang dùng preset minh họa, chưa hiệu chuẩn sức bền. Các kiểm tra va chạm là các trạng thái lấy mẫu; chưa chứng nhận mọi tư thế hoặc 72 Hz trên kính thật.

## Public source snapshot — 03/11/2026

- [Phạm vi công bố và cách mở dự án](PUBLICATION.md).
- [Nhận bài trang phục FBX/GLB/OBJ theo body mẫu](Docs/STUDENT_GARMENT_GUIDE.md).
- [Thử GarmentCode: áo, váy và cổ áo](Labs/ClothRepos/README.md).
- [Đánh giá Spec Kit và Skills For Real Engineers](Research/VideoReviews/2026-11-03/REPORT.md).

Clone repo, dùng Unity Hub mở thư mục chứa `Assets`, `Packages`, `ProjectSettings` bằng Unity 2022.3.62f1. Unity tải lại các package và tạo Library. EXE đã build, thư viện Python, dataset tải về và video gốc không nằm trong Git. Các đường dẫn Desktop/Downloads trong hướng dẫn lịch sử mô tả máy phát triển ban đầu.
