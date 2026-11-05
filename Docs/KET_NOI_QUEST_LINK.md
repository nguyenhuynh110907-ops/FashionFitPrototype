Vào lại VR: [mở phiên thử Quest Link và làm ba bước](VR_REENTRY_GUIDE.md).

# Kết nối Quest với Fashion Unity — làm lần lượt

Đây là cách chạy **Unity trên PC và xem bằng Quest 2/3/3S qua Link**. Chưa cần build APK hay bật Developer Mode. Làm xong từng phần rồi mới chuyển tiếp.

## A. Trên PC

1. Mở/cài ứng dụng **Meta Horizon Link** từ Meta, đăng nhập tài khoản của bạn.
2. Cắm Quest vào cổng USB 3 bằng **cáp có truyền dữ liệu**, không chỉ sạc. Ưu tiên cổng/cáp USB 3 và chạy USB Test để kiểm tra kết nối.
3. Trong Link, kiểm tra kính có xuất hiện ở **Devices**. Nếu có **USB Test**, chạy thử cáp.
4. Mở **Settings → General → OpenXR Runtime**, chọn Meta làm runtime đang hoạt động nếu chưa phải. Đóng rồi mở lại Unity sau khi đổi runtime.

Nếu Link chưa nhận kính ở phần này, chưa cần sửa các nút trong game. Kiểm tra cáp/cổng USB và [hướng dẫn Link của Meta](https://www.meta.com/help/quest/509273027107091/).

**Máy hiện dùng Radeon 860M:** mẫu này chưa được liệt kê trong [danh sách GPU hỗ trợ Link](https://www.meta.com/help/quest/articles/headsets-and-accessories/oculus-link/requirements-quest-link/). Vì vậy chưa thể hứa máy hiện tại chạy được Link; cần vào được môi trường PCVR ngoài game trước. Nếu máy có GPU rời, kiểm tra Link và Unity đang sử dụng GPU đó.

## B. Trong kính Quest

1. Đeo kính, cầm hai Touch controller.
2. Bấm nút Meta/Oculus để mở menu → **Quick controls / Quick Settings → Link / Quest Link**.
3. Chọn PC đã cắm cáp → **Launch**.
4. Xác nhận thấy môi trường Link và hai tay cầm hoạt động. Đường dùng cáp này không cần mở SteamVR cùng lúc.

Tên menu có thể khác theo phiên bản hệ điều hành. Đây là [quy trình kết nối chính thức của Meta](https://www.meta.com/help/quest/509273027107091/).

## C. Trong Unity trên PC

1. Mở `Open_FashionFit.bat`, chờ compile.
   Nếu Unity đang Play khi code vừa được cập nhật, **Stop rồi Play lại** để tạo lại scene và trạng thái vải; chỉ thay script trong phiên Play cũ có thể làm mất trạng thái mô phỏng.
2. Stop Play nếu đang chạy. Chọn **Fashion Fit → Configure OpenXR**.
3. Nếu cần kiểm tra: **Edit → Project Settings → XR Plug-in Management → Standalone** có OpenXR; trong OpenXR có **Oculus Touch Controller Profile**.
4. Bấm **Play → VR / CAMERA → Kiểm tra cấu hình OpenXR → Vào VR**.
5. Đeo lại kính. Quay đầu: camera phải quay theo. Cần trái để đi, cần phải để quay.

Project khởi tạo XR bằng nút **Vào VR**, nên không cần bật **Initialize XR on Startup**. Giữ platform Windows ở lần thử này. [Tài liệu cấu hình Unity OpenXR](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.11/manual/project-configuration.html).

## D. Lấy nón và thay đồ

**Menu mới:** Y tay trái mở/ẩn; X tay trái đưa bảng về trước. Chọn tab **THỬ ĐỒ / TRANG PHỤC / CÀI ĐẶT** bằng tia và Trigger phải. [Hướng dẫn tủ/menu mới](ATELIER_VR_GUIDE.md).

Di chuyển tự do: [VR_FREE_MOVEMENT.md](VR_FREE_MOVEMENT.md). Cần trái đi theo hướng nhìn, cần phải quay; trong **CÀI ĐẶT** đổi cách quay/tốc độ. Thả cả hai cần về giữa sau khi vào VR hoặc khi tracking phục hồi.

- **Trigger phải**: bấm các nút trong kính.
- **Grip phải**: nút bóp ở **bên hông** tay cầm, dùng để lấy nón/mẫu đồ. Trỏ tia vào vật rồi giữ, hoặc đưa tay sát vật rồi bóp.
- Thả nón gần **đầu mannequin** để đội; lấy lại bằng Grip để tháo. Đây không phải nón đội lên đầu người chơi.
- Thả mẫu đồ gần **thân mannequin** để mặc. Mẫu trong tủ là bản chọn thu gọn; game đổi sang bộ đầy đủ.
- **THỬ ĐỒ → Đến bàn và tủ đồ**: tới khu bàn/tủ. **Trả phụ kiện**: trả nón và mẫu đồ về chỗ cũ.

Bàn hiện có **10 nón dựng theo ảnh bạn gửi**. Desktop cũng chọn được từng mẫu 01–10 trong **VR / CAMERA**; bấm **Nhìn gần nón đang đội** để kiểm tra. Trỏ chuột vào vật và giữ **E** để thử đường Grip trước khi có kính.

Nếu có hình nhưng không có tia, kiểm tra controller phải/Touch Profile; thả Grip rồi bóp lại khi tracking đã trở lại. Nếu giật, chọn **CÀI ĐẶT → Đồ họa nhẹ**, để mannequin đứng yên và tắt quạt vải. Chưa có thử nghiệm trên kính thật để xác nhận FPS.

Chi tiết hơn: [hướng dẫn bàn/tủ](VR_SHOWROOM_QUICK_START.md) · [ảnh và kiểm thử 10 nón](../Verification/ReferenceHats/REPORT.md).
