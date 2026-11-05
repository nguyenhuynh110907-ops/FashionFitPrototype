# Mở mô hình FashionFit

## Mở nhanh, không cần Unity Editor

Chạy **`Open_FashionFit_Model.bat`** trong thư mục dự án hiện tại, hoặc mở:

`C:\Users\HUYNH\Downloads\FashionFit_Run\Open_Model.bat`

Bản Windows nằm ngoài thư mục OneDrive, không phải chờ đồng bộ project hoặc biên dịch Unity. Giữ nguyên `FashionFittingRoom.exe`, `FashionFittingRoom_Data`, `UnityPlayer.dll` và `MonoBleedingEdge` trong cùng thư mục; không chỉ chuyển riêng EXE.

- Tab **TRANG PHỤC**: chọn sáu bộ đồ hoặc bản thử chuyển 3D.
- Tab **VR / CAMERA**: xem hai mannequin nam/nữ, chỉnh khớp và chuẩn bị Quest Link.
- **W/S** tiến–lùi, **A/D** đi quanh mannequin, chuột phải nhìn, **R** đặt lại góc/vị trí theo điều khiển hiện tại. Có thể dùng menu VR/CAMERA để đổi góc.
- Đồ chuyển 3D mới vẫn kiểm tra đứng yên; các mẫu có cloth solver giữ chế độ trình diễn đã kiểm định.
- Với Quest qua Link: kết nối kính và vào Link trong kính trước; trong ứng dụng chọn VR/CAMERA rồi **Vào VR**. Đợt sửa này không thử headset/controller thật.

Bản chạy đóng gói model và tài nguyên đã có. Các cửa sổ Editor để chuyển FBX/GLB, xem dữ liệu huấn luyện và tạo asset vẫn cần mở source trong Unity. Sau khi đổi code/asset, cần xuất lại player để cập nhật bản Windows.

## Mở để chỉnh sửa dự án

Chạy **`Open_FashionFit.bat`** trong:

`C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`

Dùng Unity **2022.3.62f1**, chờ import/compile rồi chọn **Fashion Fit → Open and Run Prototype** nếu cần. Launcher sẽ báo khi đúng project đã mở, tránh khởi chạy thêm Editor trùng.

Khi script được nạp lại giữa một phiên Play, các bộ giải và dictionary C# có thể mất trạng thái. Bản sửa chặn việc tiếp tục dùng solver mất dữ liệu và tự khởi động lại phiên Play ở tư thế đứng yên. Các điều chỉnh tư thế/chuyển động tạm trong phiên Play được đặt lại; cần vào VR lại sau reload. Asset và dữ liệu nguồn được giữ nguyên.

## Dấu X đỏ trong ảnh

Đó là [lỗi đồng bộ OneDrive theo tài liệu Microsoft](https://support.microsoft.com/en-us/onedrive/what-do-the-onedrive-icons-mean), không tự chứng minh file mô hình bị hỏng. Bạn đã xác nhận OneDrive báo **đầy dung lượng**. Chuyển sang bản chạy cục bộ giúp mở mô hình nhưng không tự giải phóng dung lượng tài khoản.

Thư mục nguồn chứa cache Unity, môi trường Python và dữ liệu gần 30 GB. Những thư mục này vẫn nằm ở vị trí cũ, nên dấu đồng bộ ở đó có thể còn. Đợt sửa này không đổi tài khoản/cài đặt OneDrive, không di chuyển hoặc xóa bản gốc. Bản chạy mới giúp mở mô hình độc lập với việc đồng bộ thư mục source.

Kết quả kiểm tra và file log: `Verification/ProjectRecovery/REPORT.md`.
