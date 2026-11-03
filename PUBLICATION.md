# Bản công bố mã nguồn — 03/11/2026

Repo chứa mã nguồn FashionFit V092, tài nguyên runtime Unity, công cụ AI/thu thập dữ liệu, checkpoint nhỏ đi kèm, mẫu chuyển FBX/GLB/OBJ, tài liệu và bằng chứng kiểm tra đã có; thêm bản thử GarmentCode và báo cáo hai video.

## Mở bản clone

1. Tải/clone repo về một thư mục cục bộ ngoài OneDrive.
2. Trong Unity Hub, chọn **Add project from disk** và trỏ tới thư mục có `Assets`, `Packages`, `ProjectSettings`.
3. Mở bằng **Unity 2022.3.62f1**, chờ Package Manager/Library hoàn tất. Xem [hướng dẫn](Docs/STUDIO_WORKFLOW.md) và [Quest Link](Docs/VR_REENTRY_GUIDE.md).
4. Bản thử trên web có bước thiết lập Python riêng tại [Labs/ClothRepos](Labs/ClothRepos/README.md).

EXE đã build trên máy phát triển nằm ngoài source. Repo này không chứa bản cài Windows hoặc Android APK. Các shortcut Desktop và đường dẫn tuyệt đối trong tài liệu lịch sử mô tả máy phát triển cũ; người clone cần build và cấu hình đường dẫn tại máy mình.

## Các tệp giữ cục bộ

- Unity Library, Temp, Logs, thư mục build, các Unity project QA lồng nhau và cấu hình từng máy.
- Môi trường Python, DLL/compiler dùng để kiểm tra, dataset tải về và backbone lớn. Mã thu thập, mô tả nguồn và manifest còn trong repo.
- Export QA dạng bin/npz/bytes ở Verification và log thô có thể tái tạo.
- Video Facebook gốc và ảnh trích từ video; báo cáo dẫn lại các bài đăng gốc.
- Mã nguồn và binary đã tải của GarmentCode/Warp/Assistive Gym; bản thử ghim commit, checksum, giấy phép và có script tải lại.

Các tệp đã chọn được quét mẫu credential phổ biến trước khi công bố. Đây là lượt quét tự động, không phải chứng nhận bảo mật.

## Trạng thái kiểm chứng

Các báo cáo trong Verification giữ ngày/phạm vi kiểm tra của lần chạy gốc. Việc công bố repo không phải một lượt chạy lại toàn bộ Unity/VR.

GarmentCode đã chạy ba ca tĩnh trên CPU, số giao cắt cuối do bộ đếm repo trả về bằng 0. Chưa kiểm chứng các ca đó với grip VR hoặc body FashionFit. Mặc/cởi tự do vẫn có phần INCOMPLETE. Thông số vật liệu đang minh họa và chưa hiệu chuẩn từ đo vải thật.

## Nguồn tài nguyên và giấy phép

Xem [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md), `THIRD_PARTY/` và `Labs/ClothRepos/THIRD_PARTY/`. Giữ giấy phép riêng của từng tài nguyên/thư viện. Mẫu thử Warp cũ có giới hạn nghiên cứu/đánh giá. Chưa cấp một giấy phép chung mới cho toàn bộ code riêng của dự án trong đợt công bố này.
