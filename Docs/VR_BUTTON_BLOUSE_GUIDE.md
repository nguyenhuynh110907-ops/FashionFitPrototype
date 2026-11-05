# Thử mở nút và cởi áo bằng hai tay

Bản đầu dùng một áo blouse màu kem, tay ngắn, mở toàn bộ phía trước với bốn nút. Áp dụng trên mannequin nữ kỹ thuật ở giữa phòng. Các mẫu đầm, polo, áo dài, mannequin pose nam/nữ và trang phục FBX/GLB/OBJ khác chưa tự có chức năng cởi này.

## Mở bản thử

Chạy `Try_Button_Blouse_VR.bat` trong thư mục project. Launcher mở EXE đã cập nhật ở `%USERPROFILE%\Downloads\FashionFit_Run` và mặc sẵn áo thử. Launcher thường `Open_FashionFit_Model.bat` vẫn dùng được; chọn **TRANG PHỤC → Mặc áo blouse cài 4 nút**.

Nếu dùng Unity Editor, mở scene FashionFitPrototype và bấm Play, rồi chọn cùng mục trên. Chỉ chạy một phiên XR: Editor hoặc EXE.

1. Cắm cáp, vào **Quest Link** trong kính và xác nhận hai tay cầm Touch hoạt động trong môi trường Link.
2. Trong FashionFit chọn **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**. Thả Grip và Trigger một lần sau khi vào để nhận lần bấm mới.
3. Trong kính, **Y** mở menu, **X** đưa menu về trước. Chọn thanh **ÁO CÀI NÚT / CỞI ÁO** ở dưới các tab. Chọn **Mặc áo cài 4 nút** nếu chưa mặc sẵn.

## Thao tác trên áo

1. Đi tới sát mannequin bằng cần trái. Cần phải quay người. Đưa tay tới một nút áo; nút sáng lên khi được chọn ở khoảng cách tối đa **3,5 cm**.
2. Bóp rồi thả **Trigger** để mở nút. Lặp lại đủ bốn nút; nút đã mở có màu xanh. Tia chọn menu không mở nút áo từ xa.
3. Dùng **Grip của cả hai tay** giữ hai vùng riêng ở gần cổ/vai áo, mỗi tay một bên. Vải nhận Grip khi tay cách một điểm lưới không quá **8,5 cm**.
4. Chọn **Bắt đầu cởi áo**, hoặc kéo nhẹ sau khi đã mở đủ nút để tự bắt đầu. Tách hai bên ra một chút, kéo chậm xuống và ra ngoài vai, đưa áo qua cánh tay/bàn tay mannequin, rồi kéo ra trước. Tránh giật mạnh hoặc kéo xuyên thẳng qua thân.
5. Thả một Grip: tay kia tiếp tục giữ. Thả Grip cuối: áo rơi theo trọng lực. Khi đổi trang phục, áo đã cởi vẫn nằm trong phòng và giữ vật liệu riêng.
6. **Đặt lại áo lên mẫu** đưa chính chiếc áo đó về tư thế mặc và đóng bốn nút. Đây là thao tác reset; chưa hỗ trợ luồn tay và mặc ngược bằng vật lý.

Có thể cài lại từng nút bằng Trigger khi đưa hai mép nẹp gần nhau dưới **2,5 cm**. Áo chưa mở đủ bốn nút sẽ từ chối bắt đầu cởi. Việc kéo được giới hạn để giảm căng lưới và xuyên cơ thể; tay ảo có thể đi trước vải nếu bạn kéo quá nhanh.

## Dọn bản thử và bật lại trình diễn

Mannequin chính giữ tư thế đứng đã hiệu chỉnh va chạm trong suốt lúc có áo thử trong phòng. Bạn vẫn đi lại bằng VR và dùng hai mannequin pose phụ như trước. Để bật lại bước đi của mannequin chính:

1. Cởi rồi thả áo, chọn **Dọn áo rời đã thả**; hoặc đổi trang phục nếu áo còn ở trạng thái mặc.
2. Dọn hết các áo thử đã rời mẫu. Tối đa ba áo thử tồn tại cùng lúc; áo đang được cầm không bị nút dọn xóa.
3. Vào **THỬ ĐỒ → Nhịp trình diễn** để chọn tốc độ.

## Thử nhanh khi chưa đeo kính

Desktop có bốn nút **Mở/Cài 1–4** và các nút bắt đầu cởi/đặt lại. Có thể kéo áo bằng thao tác Grip mô phỏng **E** sẵn có. Bài kiểm thử tự động dùng hai tay điều khiển riêng; bàn phím không thay thế trải nghiệm hai tay Touch.

## Phạm vi kiểm tra

Xem [báo cáo triển khai](../Verification/GarmentDoffing/IMPLEMENTATION_REPORT.md). Đã kiểm tra bằng phần mềm và EXE trên PC; chưa kiểm tra lại bằng kính Meta thật. Kiểm tra tiếp xúc theo mẫu hữu hạn không bảo đảm mọi tam giác luôn không xuyên trong mọi chuyển động. Va chạm giữa các lớp là proxy một chiều với chân váy mẫu; chưa phải mô phỏng nhiều lớp vải tổng quát hoặc thông số vải đo từ thực nghiệm.
