# Hai tay cùng tương tác, tốc độ đi bộ và bệ mannequin pose

Đã sửa trực tiếp source tại `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype` và cập nhật EXE ở `C:\Users\HUYNH\Downloads\FashionFit_Run`. Mở lại bằng `Open_FashionFit_Model.bat`; không cần ZIP mới. [Hướng dẫn dùng](../../Docs/VR_TWO_HANDS_GUIDE.md).

## Kết quả triển khai

- Hai tay Touch cùng giữ một nón/mẫu đồ, mang và xoay theo hệ quy chiếu hai điểm. Không đổi kích thước vật. Thả một tay hoặc mất tracking một tay vẫn giữ tay còn lại; thả tay cuối mới áp trọng lực/đội/mặc. Vẫn hỗ trợ hai tay cầm hai món độc lập. Tối đa hai chủ giữ; có rebase offset khi chuyển 1→2→1 tay để tránh nhảy.
- Váy procedural và áo Ivory nhận hai mảng lưới độc lập, có khoảng cách tối thiểu giữa mảng. Mỗi mảng kéo tối đa 12 cm từ lần nắm; giới hạn căng giữa hai mảng và giữ phép chiếu va chạm/swept motion. Thả một tay không tắt mảng còn lại. Giữ trọng lực theo thời gian thực. Bắp chân tăng 8 mm ở proxy sau khi phép đo mesh phát hiện xuyên gần 4 mm trong ca kéo Emerald; đã đo lại sau sửa, không che da.
- Hai mannequin pose có root bệ chung chứa cơ thể, khớp, nhãn và tay nắm phía trước lệch bên cạnh. Grip tay nắm/bệ hoặc vùng thân ngoài marker để mang cả mẫu và quay theo yaw. Tư thế và chiều dài xương giữ nguyên. Trong lúc giữ bệ khóa chỉnh khớp liên quan; khi giữ khớp không cho giành cả bệ. Bệ có Rigidbody 60 kg, trọng tâm thấp, giữ thẳng đứng, chặn vật chắn/giới hạn phòng, hạ xuống sàn bằng trọng lực sau thả. Có thể kéo ngang sát sàn. Đặt lại mẫu phục hồi cả tư thế và vị trí bệ.
- Các marker khớp là trigger, không trở thành các viên cầu va chạm của Rigidbody. Vùng chọn thân vô hình không che khớp cùng mannequin trong tia chọn. Hỗ trợ hai tay giữ cùng bệ.
- Nhịp mặc định của nút đi bộ chính tăng từ 0,18× lên 0,50×; lựa chọn nhanh 0,75× và tối đa 1,00×. Giữ tùy chọn chậm 0,18×. Ứng dụng vẫn mở ở tư thế đứng yên. Tăng nhịp tối đa 0,30×/giây, giữ warmup, bước mô phỏng 1/60 giây và bộ giữ tư thế nếu vi phạm collider đại diện. Không đổi biên độ dáng đi hoặc giả tốc độ bằng giảm bước vật lý.
- Sửa quy trình build: ép physics tự động `FixedUpdate`, tránh mang chế độ mô phỏng thủ công của QA vào EXE. Trước sửa, cấu hình ở bản sao kiểm thử đã làm Rigidbody không tự chạy trong EXE dù cờ gravity bật. Các bài QA giờ phục hồi mode trước khi gọi native Editor exit; cả lệnh mở Play và các đường build đều chuẩn bị physics tự động. Kiểm tra EXE mới đo chuyển động/rơi thật theo thời gian physics, không chỉ kiểm tra cờ `useGravity`.

## Kiểm tra

| Kiểm tra | Kết quả |
|---|---|
| Cầm/di chuyển/xoay hai tay, 7 ca kéo vải, giữ tư thế và bệ | 95 assertion qua, 0 exception |
| Bệ kéo ngang trên sàn, chặn vách mỏng, giới hạn phòng, reset | 9 assertion qua |
| Nút nhịp đi bộ, tăng tốc, dừng và clamp | 8 assertion qua |
| Cấu hình OpenXR và menu | 26 assertion qua |
| Toàn bộ 34 khớp nam/nữ và mesh | 1930 assertion qua |
| Windows build | 115,457,768 byte, 0 lỗi, 2 cảnh báo OpenXR tùy chọn |
| EXE thực đã xuất | 37 assertion qua, 0 exception C#/Unity trong phép thử |

Nón không nhảy khi tay thứ hai tham gia hoặc rời ra trong bài thử. Bệ di chuyển khoảng 0.843 m, giữ nguyên các local bone position/rotation, và hạ về cao độ root khoảng 0,030 m tương ứng chiều dày bệ trên sàn. Các số này là bài mô phỏng hữu hạn, không phải đo bằng kính.

Trong EXE thực: physics tự động được xác nhận; nón rơi 1.801 m, bệ kéo được 0.461 m và hạ về root Y=0.030 m sau 1.00 giây physics. Không suy ra kết quả từ số frame render.

## Đo tiếp xúc độc lập

Xuất cơ thể nguyên vẹn bằng BakeMesh, không dùng mặt nạ che da. Kiểm tra các đỉnh được tham chiếu và tâm tam giác trang phục bằng libigl distance + quy tắc dấu Trimesh/Embree như bộ kiểm tra trước.

| Ca | Mesh tư thế | Điểm lấy mẫu | Điểm xuyên >1 mm | Lớn nhất |
|---|---:|---:|---:|---:|
| Contact | 21 | 320,883 | 0 | 0.79825 mm |
| Speed075_60fps | 54 | 858,375 | 0 | 0.79828 mm |
| Speed100_60fps | 54 | 858,375 | 0 | 0.79828 mm |
| Speed100_30fps | 54 | 858,375 | 0 | 0.79828 mm |

Tổng 183 tư thế / 2,896,008 mẫu; không phát hiện điểm xuyên trên 1 mm trong các ca này. Mỗi ca đi bộ chạy đủ hai chu kỳ sau đạt nhịp, kiểm tra lúc tăng tốc/dừng và xuất trạng thái sau dừng. 60/30 trong tên ca là delta thời gian đầu vào bài mô phỏng, không phải FPS đo trong kính. Các JSON ở thư mục con chứa kết quả từng tư thế và SHA-256.

Raw dump `.bin` giữ ở bản kiểm thử ngoài OneDrive: `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\ModelGrip_Work\UnityProject\Verification\Bimanual\Contact` và `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\ModelGrip_Work\UnityProject\Verification\SpeedContact`. Không nhân bản dump lớn vào Desktop đang đầy quota OneDrive. `source_snapshot.json` ghi hash source và các file player chính; `deployment.json` ghi kết quả đối chiếu.

## Giới hạn

Chưa thử lại bằng kính Meta thật; chưa chứng nhận tracking, cảm giác tay hoặc FPS PCVR. EXE thử desktop có thể ghi `XR_ERROR_FORM_FACTOR_UNAVAILABLE` ở native probe khi không có headset sẵn sàng; 0 exception trong JSON là bộ đếm lỗi C#/Unity, không phủ nhận thông báo native đó. Đây không phải chứng nhận liên tục mọi cặp tam giác, mọi tư thế/loại vải hoặc mọi chuyển động tay. Chưa kiểm tra tự giao giữa mọi lớp vải.

Mannequin pose giữ tư thế, không ragdoll/tự cân bằng; collider bệ/thân/nón vẫn xấp xỉ. Các mannequin pose chưa mặc bộ đồ của mannequin trình diễn chính. Trang phục chuyển 3D của sinh viên tiếp tục giữ vật liệu gốc và chặn motion/grip chưa kiểm định. Texture web giữ nguyên, không huấn luyện AI mới hoặc suy ra sức bền thật từ ảnh.
