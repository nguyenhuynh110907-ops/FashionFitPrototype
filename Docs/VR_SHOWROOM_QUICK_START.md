# Bắt đầu với Meta Quest Link: bàn nón và tủ đồ

Hướng dẫn kết nối ngắn, chia rõ PC/kính/Unity: [KET_NOI_QUEST_LINK.md](KET_NOI_QUEST_LINK.md).

Giao diện mới chia ba tab tiếng Việt: [ATELIER_VR_GUIDE.md](ATELIER_VR_GUIDE.md). Y trái mở/ẩn, X trái đưa menu về trước. Các lệnh bàn/tủ và trả phụ kiện nằm trong tab THỬ ĐỒ.

Đã sửa trực tiếp project. Bàn có **10 nón dựng theo ảnh tham chiếu mới**; tủ có sáu mẫu chọn trang phục. Giữ Grip để đem vật ra, xoay tay để xoay vật, thả Grip để đặt hoặc mặc. [Danh sách nón và ảnh kiểm tra](../Verification/ReferenceHats/REPORT.md).

## 1. Kiểm tra trên máy trước khi cắm kính

1. Mở `Open_FashionFit.bat`, chờ Unity compile rồi Play.
2. Vào **VR / CAMERA → Xem bàn nón và tủ đồ**.
3. Trỏ chuột vào nón hoặc mẫu đồ, giữ **E** để giả lập Grip. Vật được kéo về gần camera; thả E để đặt xuống. Cửa sổ Game phải có focus.
4. Danh sách **10 NÓN THEO ẢNH THAM CHIẾU**, số **01–10**, đội nhanh từng mẫu lên mannequin. Bấm **Nhìn gần nón đang đội · desktop** để xem chi tiết. Các nút **Lấy … từ tủ** mặc nhanh để kiểm tra lựa chọn.
5. Bấm **Trả nón / mẫu đồ về bàn và tủ** để bắt đầu lại.

Mẫu treo trong tủ là mô hình chọn đồ thu gọn, không phải sáu bộ vải đang được mô phỏng cùng lúc. Khi thả gần mannequin, game mặc bộ đầy đủ tương ứng. Cách này giữ chi phí mô phỏng thấp hơn việc chạy sáu bộ trên kệ.

## 2. Chuẩn bị PC và kính

Đường chạy này là **PCVR**: Unity chạy trên Windows, Quest nhận hình ảnh và gửi tracking/tay cầm qua Link. Không cần build APK hay bật Developer Mode để bắt đầu đường chạy này.

Máy hiện ghi nhận Unity dùng **AMD Radeon 860M**. GPU này không nằm trong danh sách hỗ trợ Link của Meta tại thời điểm kiểm tra; chưa thể cam kết máy hiện tại chạy được Link. Nếu máy có GPU rời, kiểm tra Unity và Meta Horizon Link có dùng GPU đó; nếu chỉ có 860M, hãy xác nhận Link vào được môi trường PCVR trước khi kiểm tra game. [Yêu cầu PC/GPU chính thức](https://www.meta.com/help/quest/articles/headsets-and-accessories/oculus-link/requirements-quest-link/).

1. Cài/cập nhật **Meta Horizon Link** trên Windows và cập nhật hệ điều hành Quest. Dùng ứng dụng chính thức từ Meta.
2. Kết nối bằng cáp dữ liệu USB phù hợp. Nguồn Meta hiện hướng dẫn USB-C 3.2, tối thiểu 5 Gbps, cắm vào cổng USB 3.2. Thử bằng cáp trước để tách lỗi Wi-Fi.
3. Trong kính, mở menu Meta/Oculus → **Quick controls → Link → chọn PC → Launch**. Một số phiên bản giao diện dùng tên **Quick Settings / Quest Link**.
4. Trong ứng dụng Link trên PC, kiểm tra **Settings → General → OpenXR Runtime** và chọn Meta làm runtime nếu chưa phải. Khởi động lại Unity sau khi đổi runtime.
5. Cầm hai Touch controller và xác nhận chúng hoạt động trong môi trường Link trước khi vào game.

Các bước kết nối/cáp dựa trên [hướng dẫn Link/Air Link của Meta](https://www.meta.com/help/quest/509273027107091/). Tên menu có thể khác theo phiên bản Horizon OS. Không cần chạy SteamVR đồng thời cho đường Meta Link này.

## 3. Kiểm tra Unity

Project dùng Unity **2022.3.62f1**, OpenXR **1.11.0**, XR Plug-in Management **4.4.0**, Input System **1.7.0**. Không cần cài thêm XR Interaction Toolkit cho hệ Grip đang dùng.

1. Stop Play. Nếu cần, chọn **Fashion Fit → Configure OpenXR**.
2. Vào **Edit → Project Settings → XR Plug-in Management → PC, Mac & Linux Standalone**: bật **OpenXR**.
3. Trong **OpenXR → Interaction Profiles**, có **Oculus Touch Controller Profile** và bật profile này. Chạy **Project Validation** và xử lý các lỗi đỏ phù hợp với Standalone.
4. Bản project khởi tạo XR bằng nút **Vào VR**, nên **Initialize XR on Startup** được tắt có chủ đích; không cần bật để nút này hoạt động.
5. Giữ build target **Windows Standalone**, chưa chuyển Android ở lần thử đầu.

[Cấu hình OpenXR chính thức của Unity](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.11/manual/project-configuration.html).

## 4. Vào game bằng kính

1. Launch Link trong kính trước.
2. Play trong Unity, mở **VR / CAMERA**.
3. Bấm **Kiểm tra cấu hình OpenXR**, rồi **Vào VR · OpenXR / Quest Link**.
4. Kiểm tra chiều cao sàn, tracking đầu và dòng trạng thái tay cầm trên màn hình PC. Khi runtime hỗ trợ Floor origin, game dùng chiều cao sàn; nếu không, dùng chiều cao bù 1,65 m cho Device origin.

| Điều khiển | Tác dụng |
|---|---|
| Cần trái | Đi theo hướng nhìn |
| Cần phải | Quay người/camera |
| CÀI ĐẶT → Quay | Đổi quay mượt / từng nấc 45° |
| CÀI ĐẶT → Tốc độ đi | Đổi tốc độ đi 0,80 / 1,25 / 1,80 m/s |
| Trigger phải | Bấm bảng điều khiển |
| Grip phải, nút bóp bên hông tay cầm | Lấy/giữ nón, mẫu đồ, hoặc kéo các mục tương tác cũ |
| Thả Grip | Đặt vật; đội/mặc khi ở gần vùng phù hợp |
| THỬ ĐỒ → Đến bàn và tủ đồ | Đưa vị trí quan sát tới khu bàn/tủ |
| THỬ ĐỒ → Trả phụ kiện | Trả tất cả nón/mẫu đồ về vị trí trưng bày |
| CÀI ĐẶT → Thoát VR | Về desktop |

**Trigger và Grip là hai nút khác nhau.** Nón/mẫu đồ dùng Grip phải. Cơ chế lấy đồ hỗ trợ trỏ tia trong 4 m hoặc đưa tay vào gần vật khoảng 13 cm. Vật ở xa tiến về gần tay khi giữ Grip; có rung nhẹ nếu controller hỗ trợ. Tia chuyển vàng khi nhắm trúng vật và xanh khi đang giữ.

Bạn có thể dùng cần trái đi tự do tới mọi khu vực có lối đi, không bị khóa quanh mannequin. Sau khi vào VR hoặc vừa mất tracking, thả cả hai cần về giữa rồi đi tiếp. [Hướng dẫn di chuyển](VR_FREE_MOVEMENT.md).

## 5. Bài thử bàn/tủ

1. Giữ mannequin đứng yên; bật **QUALITY** chế độ nhẹ nếu cần.
2. Trỏ nón trên bàn, giữ Grip, đưa tay lên để kiểm tra vật đi theo tay; xoay cổ tay để xoay nón.
3. Đưa nón tới **đầu mannequin** rồi thả, trong phạm vi khoảng 30 cm tính từ mốc đội. Nón bám theo đầu; không phải đội lên đầu người chơi. Grip nón lần nữa để tháo. Chỉ giữ một nón đang đội; nón trước trở về bàn.
4. Thả nón ở xa đầu: nón chịu trọng lực, va chạm bàn/sàn. Có thể lấy lại bằng Grip.
5. Grip một mẫu trong tủ, đưa tới trước thân mannequin rồi thả trong vùng khoảng 65 cm: bộ đầy đủ được mặc, mẫu chọn quay về tủ. Thử đủ sáu lựa chọn.
6. Thả mẫu đồ ở xa mannequin: mẫu rơi như vật cứng, không tự mặc. Mẫu trưng bày chưa mô phỏng vải.
7. Vào **THỬ ĐỒ → Trả phụ kiện**. Thử mất tracking tay phải hoặc thoát VR: không được còn vật kẹt trạng thái đang giữ.

Vùng chặn bàn/tủ áp dụng cho di chuyển bằng cần. Di chuyển cơ thể thật không bị game giữ lại; bàn/tủ là vật ảo. Bắt đầu trong vùng chơi trống, có boundary và dây đeo controller.

## Nếu chưa chạy được

- **Không vào được Link ngay cả ngoài game:** kiểm tra PC/GPU/cáp bằng công cụ Meta trước. Sửa code Unity không khắc phục được GPU không được runtime hỗ trợ.
- **OpenXR không khởi tạo:** Stop Play, kiểm tra runtime Meta và loader Standalone, Launch Link rồi mở lại Unity.
- **Có hình, không có tia:** kiểm tra controller phải/tracking và Touch Profile. Sau khi tracking trở lại, thả Grip rồi bóp lại.
- **Không lấy được vật:** tia phải chạm vật, không bị mặt bàn/tủ hoặc bảng nút chắn; dùng Grip bên hông, không dùng Trigger. Có thể đến gần để lấy trực tiếp.
- **Đồ chưa tự mặc:** đưa mẫu đến vùng thân, thả Grip; không cần kéo mesh trang phục trên mannequin để đổi đồ.
- **Giật:** đứng yên, tắt quạt vải, chọn QUALITY nhẹ. Chưa có phép đo headset thật để xác nhận 72/90 FPS.

[Báo cáo kiểm thử bàn/tủ](../Verification/Showroom/REPORT.md). Chưa kiểm thử trên kính thật; kết quả hiện có là Unity tự động và giao diện desktop.
