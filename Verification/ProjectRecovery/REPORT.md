# Sửa lỗi và chuẩn bị mở mô hình

29/10/2026. Source: `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`.

## Hai vấn đề khác nhau

**Dấu X đỏ:** trạng thái không đồng bộ được của OneDrive theo [Microsoft](https://support.microsoft.com/en-us/onedrive/what-do-the-onedrive-icons-mean). Người dùng đã xác nhận ứng dụng báo **đầy dung lượng**. Không có bằng chứng từ dấu này rằng mesh bị hỏng. Không thay tài khoản/cài đặt OneDrive, không xóa/move bản gốc hoặc dữ liệu AI. Dấu đồng bộ ở thư mục cũ có thể tiếp tục hiện cho đến khi xử lý quota.

**Lỗi Unity thực tế:** log `Logs/QuestLink-Trial-Visible.log` ghi **146.805 lần `NullReferenceException`**, bắt đầu ở `FittingCloth.Step`, qua `RunwayWardrobe.StepCloth` và `FashionFitApp.AdvanceSimulation`. Bộ giải C# không serializable bị mất khi script reload giữa Play, trong khi các reference component được Unity phục hồi làm điều kiện Ready cũ vẫn đúng. Lần cập nhật code trước chưa xử lý vòng đời này. [Unity serialization/hot reload](https://docs.unity3d.com/2022.3/Documentation/Manual/script-Serialization.html).

## Bản sửa

- Thêm generation flag `[NonSerialized]` cho phiên runtime; reference model/player còn tồn tại không đủ để coi mô phỏng là sẵn sàng.
- Kiểm tra đủ solver/display/bộ đệm ở vải và áo, chặn Step/Grip khi mất dữ liệu; dọn Grip không gọi solver null.
- Player không xử lý input trên phiên chưa sẵn sàng.
- Hook Editor phân biệt reload script trong Play với domain reload thông thường khi bắt đầu Play. Sau reload giữa phiên, dừng và dựng lại Play ở tư thế đứng yên. Các tư thế/điều chỉnh tạm trong Play được đặt lại, cần vào VR lại.
- Launcher Editor tránh mở thêm đúng project đang chạy.
- Xuất player Windows ở `C:\Users\HUYNH\Downloads\FashionFit_Run`, ngoài OneDrive. Player chứa tài nguyên chạy game, không sao chép thêm bộ dữ liệu nghiên cứu gần 30 GB hoặc môi trường Python.

## Kết quả kiểm tra

- `guards.json`: **15 kiểm tra đạt**. Cố ý bỏ solver vải/áo và generation flag, gọi Step/input/Grip không phát sinh lỗi; sáu trang phục bình thường vẫn sẵn sàng và không báo vi phạm proxy trong phạm vi thử.
- `reload.json`: **reload script thật khi đang Play**, xác minh đối tượng vải được tạo mới, trở về tư thế đứng, không ngoại lệ. Không chỉ mô phỏng cờ trạng thái.
- `vr-regression.json`: **26 kiểm tra phần mềm VR**; không có thử kính/controller thật.
- `build.json`: Windows build thành công, **0 lỗi**, 2 cảnh báo khuyến nghị API input OpenXR (PoseControl/StickControl). Không đổi đường điều khiển đã dùng để xử lý các cảnh báo tùy chọn này.
- `standalone.json`: **16 kiểm tra trên EXE thực**, tải đủ hai mannequin nam/nữ, sáu trang phục, một gói chuyển 3D, menu VR; chế độ trình diễn mẫu cũ hoạt động, gói chuyển chưa kiểm định motion vẫn bị chặn đi. Có render/screenshot và **0 ngoại lệ C#**.

Log native OpenXR có báo headset unavailable khi thử vì kính chưa kết nối; không coi đó là xác nhận PCVR đã thử thành công. Chưa đánh giá FPS ổn định hoặc chứng nhận mới về xuyên vải từ phép kiểm tra mở này.

Ảnh bản chạy: `Standalone.png`. Log gốc được giữ nguyên để đối chiếu, không bị xóa.

## Mở

Chạy **`Open_FashionFit_Model.bat`** trong source, hoặc **`C:\Users\HUYNH\Downloads\FashionFit_Run\Open_Model.bat`**. Giữ toàn bộ thư mục player. Để dùng các cửa sổ chuyển FBX/GLB hoặc chỉnh source, chạy `Open_FashionFit.bat` trong project và dùng Unity 2022.3.62f1.

[Hướng dẫn mở mô hình](../../Docs/MO_MO_HINH.md). Mỗi khi chỉnh code/asset, cần xuất lại player để cập nhật bản Windows. Kiểm tra hash các file triển khai được ghi trong `source_snapshot.json`.
