# Rà soát tủ trưng bày và giao diện VR

[Cách thử](../../Docs/ATELIER_VR_GUIDE.md).

## Những vấn đề đã xử lý

- Tủ cũ dùng gần cùng một silhouette cho sáu bộ, tên nhỏ và ít phản hồi bộ đang mặc. Đã thay bằng sáu mesh tĩnh được bake từ chính trang phục runtime, giữ phom, màu và các chi tiết hiện có; không thêm sáu cloth solver.
- Tủ mới có ba khoang, nền vải, nẹp kim loại, đèn ấm, ngăn kéo và biển tên rõ hơn. Đã mở rộng khoang để mẫu Rose Couture không chồng lên mẫu bên cạnh. Dải sáng được che sau fascia, tránh chạy ngang biển tên.
- Bảng VR cũ dồn 12 lệnh và đi theo mọi động tác đầu. Bản mới có ba tab tiếng Việt, mỗi trang sáu hành động chính, nền tương phản, nút bo góc và hover rõ.
- Menu được đặt trong không gian ở khoảng 1,3 m; Y trái mở/ẩn, X trái đưa về trước. Không ép menu chạy theo pitch/roll của đầu. Grip thành công tự ẩn menu; có nhãn tên/gợi ý gần vật đang trỏ/cầm.
- Sửa shader chữ và shader nút cho stereo instancing: thêm instance ID, stereo output và khởi tạo/chuyển eye index theo tài liệu Unity. Bản kiểm tra PC chưa thay thế thử render hai mắt trên headset.
- Thêm bản xem menu bằng chuột trên desktop, không khởi tạo XR, để người dùng kiểm tra trước khi cắm kính.

## Hình ảnh

[Tủ mới](Wardrobe.png) · [Toàn phòng](Atelier.png) · [Trang thử đồ](VR_Page0.png) · [Trang chọn trang phục](VR_Page1.png) · [Trang cài đặt](VR_Page2.png).

Ảnh được render ở Unity trên PC, không phải chụp qua thấu kính Meta. Tủ vẫn là môi trường procedural. Mẫu trong tủ là bản trưng bày thu gọn, không phải sản phẩm may gốc hoặc mô phỏng mặc áo từ móc qua từng cánh tay.

## Dữ liệu và chi phí

[preview_bake.json](preview_bake.json) ghi cách bake. Sáu preview có tổng **29.604 đỉnh**, lấy từ mesh trang phục, thu nhỏ 0,66 lần; bề mặt lớn được gom đỉnh theo ô 9 mm trong không gian gốc, các phần nhỏ được giữ chi tiết. Mesh asset nằm tại `Assets/Resources/Showroom`, không được tạo lại mỗi frame. Không huấn luyện AI hoặc tải thêm thư viện 3D.

Chỉ thêm một đèn point không bóng, giới hạn chiếu lên lớp preview. UI dùng vật liệu unlit và property block để đổi hover/selection, không nhân bản material theo mỗi lần trỏ. Chưa đo FPS trong kính; không coi các thay đổi này là bằng chứng đạt 72/90 Hz.

## Kiểm tra chức năng

[verification.json](verification.json), [log cuối](final.log), [hash nguồn](source_snapshot.json).

31 kiểm tra phần mềm đã bao gồm: tải đủ sáu preview, giới hạn kích thước từng ô, chỉ một solver váy, chọn đủ sáu bộ và nhãn đang mặc, Y chống lặp khi giữ, X recenter, menu không bám đầu, hover, tia bấm được cả sáu vùng hành động, đổi cài đặt, đóng bản xem không mở XR, Grip ẩn menu và compile hai shader UI không lỗi.

Ở bố cục hiện tại, bảng cách đầu khoảng **1,31 m**; nút hành động cao 13 cm tạo góc nhìn khoảng **5,72°** tại khoảng cách danh định 1,3 m. Đây là thông số bố cục, không phải kết quả đo khả năng đọc/bấm của người dùng.

Các bài kiểm tra di chuyển và lấy/thả được chạy riêng sau thay đổi: [di chuyển](movement-verification.json), [nón và sáu mẫu đồ](showroom-verification.json). Chưa có kiểm thử phần cứng Quest/controller thật.

Đã chạy lại project chính trong Unity, mở bản xem menu bằng chuột, chuyển sang TRANG PHỤC, mặc Emerald và xác nhận nhãn đang mặc đổi ngay; nút ẨN và góc xem bàn/tủ hoạt động. [Đoạn log từ lần khởi động Play mới](live-session.log) không ghi nhận exception hoặc lỗi C# tại thời điểm kiểm tra. 22 file nguồn/tài nguyên trong snapshot khớp bản kiểm tra cô lập. Tốc độ hiển thị ở Editor không được dùng làm kết quả FPS trong kính.

## Nguồn tham khảo

- [Meta — Hands UI best practices](https://developers.meta.com/vr/design/hands-ui-best-practices/): khoảng cách và chọn bằng tia; chỉ tham khảo phần indirect interaction, project vẫn dùng controller.
- [Unity 2022.3 — Single-pass instanced custom shaders](https://docs.unity3d.com/2022.3/Documentation/Manual/SinglePassInstancing.html): cách khai báo và truyền thông tin stereo trong shader tùy biến.

Không tích hợp Meta Interaction SDK mới hoặc thay phiên bản OpenXR trong đợt này. Kiểm tra bằng kính thật vẫn cần để xác nhận typography, cả hai mắt, tracking và độ trễ.
