# Grip gần, hai tay Touch, khớp và texture

Đã sửa trực tiếp nguồn ở `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype` và cập nhật player ngoài OneDrive ở `C:\Users\HUYNH\Downloads\FashionFit_Run`. Mở lại bằng launcher cũ `Open_FashionFit_Model.bat`. Không cần tải ZIP mới.

## Vấn đề và thay đổi

Grip trước đây dùng một trạng thái tay phải, có đường ray lấy vật tới 4 m và đưa pickup về khoảng cách cố định 22 cm. Các điểm khớp mannequin lại bị tắt trong preset chuẩn bị VR. Bản mới đọc riêng pose/grip/trigger của hai tay Touch bằng `UnityEngine.XR.InputDevices`, vẽ hai bàn tay đại diện đơn giản và có trạng thái giữ, hover, rearm riêng.

- Grip đồ vật chỉ trong bán kính 16 cm quanh tay, hoặc đoạn trỏ ngắn tối đa 25 cm. Tia chọn menu có thể dài 4 m và chỉ dùng Trigger. Chặn lấy vật xuyên bàn/tủ/menu, kể cả khi origin của tay nằm trong collider bàn.
- Giữ offset điểm chạm, bỏ thao tác kéo nhảy pickup về cách tay 22 cm. Mỗi target có một chủ giữ. Khi vùng chọn áo và cơ thể chồng nhau, ưu tiên áo để tránh kéo nhầm cả mannequin; cùng loại target vẫn chọn điểm gần nhất. Hai tay cầm hai món hoặc hai khớp độc lập; không cho hai tay giành cùng món.
- Grip analog có hysteresis 0,65 / 0,35. Sau mất tracking hoặc mất focus, buông an toàn và yêu cầu thả nút trước lần grip mới. Mất một tay không giải phóng đồ của tay còn lại. Hủy thao tác không tự mặc/đội hoặc thêm vận tốc ném.
- Pickup được cập nhật kinematic theo bước physics, sweep chống xuyên vật chắn và giải penetration ở pose đề xuất, sau đó `MovePosition`/`MoveRotation`. Khi thả tự do, chuyển sang Rigidbody dynamic, gravity và CCD. Giữ vận tốc thả có giới hạn. Móc treo/nón đã đội là trạng thái cố định có điểm tựa.
- Bật sẵn 17 điểm khớp trên mỗi mannequin nam/nữ; hai cổ tay/cổ chân có thể điều khiển hai chuỗi IK độc lập. Khớp cùng chuỗi hoặc khớp cha liên quan sẽ tạm khóa. Trạng thái `IsHeld` của rig tính tất cả khớp đang giữ. Các mannequin pose đứng trên bệ và giữ tư thế; chưa làm ragdoll hay tự cân bằng.
- Menu Trigger hoạt động ở cả hai tay. Y/X và cần di chuyển/quay giữ nguyên. Thêm **CÀI ĐẶT → Bề mặt vải** với 6 mẫu có sẵn; áp màu + normal +Y + roughness. Vân dệt CC0 dùng chung trên nón cotton/denim và bản trưng bày vải trong tủ. Lọc chi tiết shader nón ở xa để hạn chế vân rung; không tăng số hạt solver.
- Web `AI/MaterialLab/FabricViewer.html` có 12 mẫu và 36 map, lựa chọn kênh, lặp texture, mở ảnh gốc và nguồn. Sáu mẫu “Có trong VR” tham chiếu chính xác file đang đóng gói với Unity. Mở bằng `Open_Texture_Web.bat`. Web chạy ngoại tuyến, chưa điều khiển trực tiếp phiên Unity đang mở. Không tải thêm dataset, không train mới và không thay profile cơ học.

## Kiểm tra thực hiện

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Hai tay / vật lý / khớp / texture | 69 assertion qua; 34 khớp lấy được, 6 texture áp được, 0 lỗi runtime trong phép thử | `hands.json`, `hands-test.log` |
| Cấu hình OpenXR, Touch, menu và mất tracking | 26 assertion qua | `vr-ready.json`, `vr-ready.log` |
| Khớp và mesh mannequin | 1.930 assertion qua; 34 khớp, 4.186 đỉnh thay đổi, không kéo dài xương | `pose.json`, `pose-regression.log` |
| Build Windows | Thành công, 115.445.304 byte, 0 lỗi, 2 cảnh báo tính năng OpenXR tùy chọn | `build.json`, `build.log` |
| Player Windows thực đã xuất | 28 assertion qua, 6 trang phục, 2 mannequin pose, 1 gói chuyển 3D, texture và grip hai tay hoạt động trong phép thử, 0 exception | `standalone.json`, `Player.log`, `Standalone.png` |
| Web | 12 mẫu, 36 đường dẫn map tồn tại; chưa kiểm tra trực quan bằng trình duyệt | `web-data-check.json` |

Trong bài thử PhysX, nón rơi 0,284 m sau 0,24 s; nghỉ trên sàn với sai số dưới 0,001 mm ở mặt dưới collider. Pickup bị chặn bởi vách mỏng trong bài thử kéo ngang; không nhảy ở frame cầm đầu. Hai cổ tay thực sự làm biến dạng mesh, không chỉ di chuyển marker. Đây là kiểm tra hữu hạn, không phải bảo đảm mọi chuyển động/góc quay đều không xuyên. Collider nón/trang phục trưng bày vẫn là hộp xấp xỉ, không phải va chạm chi tiết từng mép nón.

`Pose_Hands.png` và `Textured_Atelier.png` là render Unity đã được xem. Công cụ Computer Use dừng trước khi đọc trang web vì không xác minh được URL Windows đủ chắc chắn để kiểm soát thao tác; không tiếp tục điều khiển hoặc đổi sang công cụ khác để vượt giới hạn đó. Trang web được kiểm tra file/liên kết, không ghi nhận là đã qua kiểm tra giao diện trình duyệt.

## Giới hạn cần biết

- Chưa kiểm tra lại end-to-end bằng kính Meta thật trong đợt này. Dữ liệu controller được đưa vào cùng hàm runtime dùng trong VR; phép thử không chứng nhận tracking phần cứng, cảm giác tay, độ trễ hoặc FPS trong kính.
- Log EXE có `XR_ERROR_FORM_FACTOR_UNAVAILABLE` ở lần dò thiết bị native khi khởi động. Phiên kiểm thử không có headset OpenXR sẵn sàng và chạy ở desktop. Con số exception bằng 0 trong JSON là lỗi C#/Unity mà bộ kiểm thử ghi nhận, không phủ nhận thông báo native này và không chứng nhận đã vào VR thành công.
- Hai tay Touch được hỗ trợ. Chưa triển khai skeletal hand tracking bàn tay trần hoặc mô hình găng tay hoàn chỉnh. Một món chỉ có một tay giữ; chưa có hai điểm kéo đồng thời trên cùng tấm vải.
- Mannequin pose nam/nữ là khu thử khớp riêng; không mặc quần áo của mannequin chính và không có mô phỏng cơ thể tự va chạm/ragdoll. Trang phục 3D chuyển từ sinh viên giữ vật liệu gốc và chặn motion/grip chưa kiểm định như trước.
- Không thay solver chống xuyên vải trong đợt này. Trọng lực solver vải và hạ mannequin chính sau khi nhấc vẫn được giữ. Chưa thêm kiểm định tiếp xúc mới trên toàn bộ vải ở tốc độ cao.
- Texture không suy ra sức bền vật liệu. Các mẫu CC0 có thể dùng bề mặt; dữ liệu cơ học nghiên cứu vẫn nằm ngoài build. Không đẩy dữ liệu lên web công khai/GitHub.
- Web và Unity chưa có cầu nối điều khiển trực tiếp. Mở map/đổi lựa chọn trên web không tự đổi vật liệu trong game; dùng menu VR để áp 6 mẫu runtime.

## Thử lại trong kính

1. Mở lại player mới, Launch Quest Link rồi vào VR. Thả Grip/Trigger một lần, kiểm tra thấy hai bàn tay đại diện.
2. Đứng xa bàn: Grip không lấy nón. Đi đến gần, cầm hai nón bằng hai tay, nhấc lên rồi thả từng chiếc xuống bàn/sàn.
3. Đến POSE STUDIO: đưa hai tay vào hai điểm cổ tay của một mannequin, giữ Grip và kéo chậm, thả để giữ tư thế.
4. Y → CÀI ĐẶT → Bề mặt vải, thử chọn mẫu bằng Trigger từng tay. Cần trái đi, cần phải quay.

Hướng dẫn đầy đủ: [VR_TWO_HANDS_GUIDE.md](../../Docs/VR_TWO_HANDS_GUIDE.md).

Tham khảo API: [Unity XR Input](https://docs.unity3d.com/2022.3/Documentation/Manual/xr_input.html). Nguồn texture: [ambientCG CC0](https://docs.ambientcg.com/license/); manifest cụ thể giữ trong `Assets/StreamingAssets/FashionFit/FabricTrial/manifest.json` và `AI/ClothData/Data/MaterialLab/texture_catalog.json`.
