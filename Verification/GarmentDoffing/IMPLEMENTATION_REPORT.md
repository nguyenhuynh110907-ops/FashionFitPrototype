# Áo cài nút và cởi bằng hai tay — bản thử đã triển khai

Đã chỉnh trực tiếp project `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype` và cập nhật EXE `C:\Users\HUYNH\Downloads\FashionFit_Run\FashionFittingRoom.exe`. Mở `Try_Button_Blouse_VR.bat` để mặc sẵn áo thử. [Hướng dẫn thao tác](../../Docs/VR_BUTTON_BLOUSE_GUIDE.md).

## Đã thực hiện

- Một blouse mở toàn bộ phía trước, tay ngắn, bốn nút. Hai mép nẹp là các đỉnh lưới riêng; mở nút thật sự bỏ liên kết giữa chúng. Nút vẫn bám trên một mép áo. Không dùng cách đổi màu nút để giả cởi áo.
- Trigger chỉ tác động nút trong 3,5 cm, kiểm tra đường chạm qua thân/phòng. Không lặp lại khi giữ Trigger; chặn hai tay đảo cùng nút hai lần trong một frame. Cài lại khi hai mép dưới 2,5 cm. Grip có hai chủ sở hữu độc lập, mỗi tay giữ một điểm; thả/mất tracking một tay giữ nguyên tay kia.
- Các trạng thái mặc → đang cởi → rời mannequin. Chỉ bắt đầu cởi khi mở đủ bốn nút. Điểm tựa giảm dần trong 0,65 giây; vải tiếp tục chịu trọng lực, khoảng cách cạnh, uốn và va chạm sau khi rời người. Dùng Grip gần cổ/vai, kéo chậm qua vai và bàn tay, rồi ra trước.
- Áo đã cởi tồn tại độc lập khi đổi outfit; sao chép các texture tùy chỉnh để việc xóa texture outfit không làm mất texture áo đã thả. Tối đa ba áo trong phòng, dọn riêng áo rời không được cầm. Đặt lại đưa chính áo cũ lên người và đóng nút; chưa phải mặc ngược bằng vật lý.
- Mesh áo 646 đỉnh, 1189 tam giác. Dữ liệu va chạm là SDF lấy từ toàn bộ cơ thể ở tư thế fit, bước voxel 6 mm, khoảng hở 8 mm, kiểm tra thêm tâm/cạnh mặt để giảm xuyên giữa các đỉnh. Dữ liệu chỉ hợp lệ với đúng cơ thể/tư thế; runtime kiểm tra khóa hình học trước khi dùng.
- Áo thử dùng mannequin nữ kỹ thuật chính và giữ tư thế đứng đã kiểm tra. Khóa di chuyển/gait của mẫu chính khi còn áo thử; vẫn cho người dùng đi quanh bằng VR và thao tác mannequin pose phụ. Va chạm chân váy là bao ngoài bảo thủ một chiều, không giả định áo nhập ngoài là cùng loại proxy.
- Áo chạy vật lý 60 Hz độc lập render; bỏ các phép xử lý mặt đã chắc chắn không thay đổi, ngủ mô phỏng khi ổn định. Sửa nguyên nhân lag do Grip áo làm thức cả solver váy cũ: giờ chỉ Grip váy thật/gió mới đánh thức solver đó, và vẫn có đuôi settle khi thả. Thời gian xử lý áo được cộng vào chỉ số vải của giao diện. Không tăng số điểm lưới vô hạn.

## Kết quả kiểm tra

| Phép thử | Kết quả |
|---|---|
| Áo: gần/xa, mở nẹp, hai tay, thả từng tay, rơi, tồn tại, reset và khóa tư thế | 43 assertion, 0 exception |
| Hồi quy hai tay/nón/kéo vải/bệ | 95 assertion |
| Cấu hình OpenXR, tracking, menu mới và quay lại trang phục | 28 assertion |
| Điều khiển nhịp đi bộ | 8 assertion |
| Khớp nam/nữ | 1930 assertion |
| Windows player xuất thực tế | 18 assertion, 0 exception C#/Unity |
| Build Windows | 0 lỗi, 2 cảnh báo, physics tự động FixedUpdate |

Trong Editor, khe hai mép tại nút đo từ 5.99 mm khi cài sang 462.21 mm sau kéo. Khi thả tay cuối, tâm áo hạ 0.625 m. Trong EXE thực, độ rơi là 0.624 m. Tỷ lệ giãn cạnh lớn nhất tại trạng thái cuối phép kéo: Editor 1.803, EXE 1.864; vật liệu này vẫn là bản thử, không phải vải đã hiệu chuẩn cơ học.

### Va chạm đo độc lập

11 mesh áo trong các trạng thái cài, kéo theo 12 giây, giữ và nằm sàn; 80,773 điểm gồm đỉnh, tâm mặt và ba điểm trên mỗi cạnh duy nhất. Không phát hiện mẫu xuyên cơ thể trên 1 mm trong các ca này. Cơ thể BakeMesh nguyên vẹn, không dùng mặt nạ che da; khoảng cách tam giác đo bằng libigl, dấu trong/ngoài bằng Embree ray parity, có điểm đối chứng nằm trong thân. Tự pháp tuyến mặt gần nhất không đủ tin cậy ở nách lõm nên không dùng cách đó để gán dấu.

`body_contact.json` lưu kết quả từng pose và SHA-256. Raw mesh giữ tại `C:\Users\HUYNH\Downloads\Fashion_Fitting_Room_v0.5.2\ModelGrip_Work\UnityProject\Verification\GarmentDoffing\Trial\Contact` ngoài OneDrive. Phép đo hữu hạn này không chứng nhận va chạm liên tục hoặc mọi chuyển động tay. Chưa kiểm tra đầy đủ mọi cặp tam giác tự giao hay xuyên giữa tất cả các lớp áo/váy.

### Hiệu năng và thử kính

Editor: trung bình 12.07 ms cho một lần cập nhật kiểm thử 1/60 giây; đỉnh 42.86 ms. EXE diagnostic trên PC, cửa sổ chạy ẩn: trung bình frame khi kéo 1.29 ms, P95 11.38 ms; nền trước khi mặc áo 0.32 ms, sau ổn định 0.13 ms. Các số này không phải FPS trong headset và không chứng nhận đạt 72/90 Hz PCVR. Cần thử trên kính Meta để đánh giá cảm giác độ trễ tay và chất lượng trình diễn.

Chưa có phiên kính thật trong đợt này. Log native có thể ghi `XR_ERROR_FORM_FACTOR_UNAVAILABLE` khi probe OpenXR không có headset; số exception nêu trên là bộ đếm C#/Unity, không phủ nhận log native đó.

## Nguồn và giới hạn

Đây là blouse mới từ lưới phom có sẵn trong project, giảm lưới và tách nẹp ngoại tuyến; không nhập quần áo thương hiệu mới và không huấn luyện AI. `authored_mesh.json` ghi nguồn và hash mesh. Fast-simplification 0.2.0 chỉ dùng lúc tạo asset, không kèm thư viện chạy trong game; [giấy phép MIT của công cụ](AuthoringTools/fast-simplification-LICENSE.txt) được giữ để tham khảo. `body_sdf.json` mô tả cache dẫn xuất; hash file nguồn authoring là snapshot lúc tạo, không phải hash Closed.bin bị ghi lại sau mỗi lần QA.

Không tự áp mở nút/cởi lên mọi FBX/GLB/OBJ, polo, áo dài hoặc mannequin pose. Các đối tượng đó cần mesh tách mảnh, khuy/điểm tựa và dữ liệu va chạm riêng. Chưa có may đường gấp cổ, khuyết nút chi tiết, nếp vật liệu đã đo thực tế, tearing, mặc ngược hay self-collision tam giác đầy đủ. Tay đi trước vải khi bị giới hạn kéo chống giãn; cần kéo chậm. Báo cáo rà soát trước triển khai vẫn giữ tại [REPORT.md](REPORT.md).
