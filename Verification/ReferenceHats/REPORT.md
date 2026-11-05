# 10 nón theo ảnh tham chiếu — 28/10/2026

Cập nhật tiếp theo: [di chuyển tự do trong kính, TURN/WALK và bảng 12 nút](../VRLocomotion/REPORT.md). Các ảnh/kiểm thử nón dưới đây được giữ theo thời điểm hoàn thành bộ nón.

Đã thay bộ ba nón cũ bằng mười mẫu nhìn thấy đầy đủ trong ảnh người dùng gửi. Đây là **hình học 3D dựng lại theo ảnh**, không phải file CAD/scan gốc của cửa hàng. Mặt khuất, cấu tạo trong nón, số đo và một số chi tiết nhỏ được ước lượng; chưa thể gọi là bản sao chính xác sản phẩm thật.

## Danh sách và đặc điểm

| Số | Mẫu trong ảnh | Chi tiết đã dựng |
|---|---|---|
| [01](Hat_01.png) | Xanh vành phẳng | Chóp sáu múi, đường ráp, nút đỉnh, khoen thoáng, vành phẳng có chỉ, tag 686 |
| [02](Hat_02.png) | Lưỡi trai đen | Chóp tròn, vành cong, đường ráp tối màu, quai sau |
| [03](Hat_03.png) | Top hat đỏ | Chóp cao loe nhẹ, đỉnh phẳng, vành oval, đai đỏ hồng |
| [04](Hat_04.png) | Outback be | Chóp bóp lõm, vành rộng, đai da đỏ nâu và khóa cạnh |
| [05](Hat_05.png) | Chong chóng nhiều màu | Múi nhiều màu, vành xanh, trục, cánh quay màu xanh và ngôi sao đỏ |
| [06](Hat_06.png) | Cowboy da nâu | Vành cuộn hai bên, chóp lõm, chất da, đai bện và đinh trang trí |
| [07](Hat_07.png) | Lưỡi trai cam | Chóp sáu múi, lỗ thoáng, nút và vành cong có chỉ |
| [08](Hat_08.png) | Fedora nâu trong ảnh người đội | Chóp thấp bóp lõm, vành hẹp, đai đen |
| [09](Hat_09.png) | Nỉ xám/nâu | Chóp lõm, vành vừa, đai bện và khóa |
| [10](Hat_10.png) | Denim KIMES | Màu denim, chỉ sáng, chữ KIMES và nét dưới chữ |

Logo/chữ là chi tiết dựng lại, không phải texture hoặc bộ nhận diện gốc được tải từ hãng. Grain/độ dệt/da là shader thủ tục; denim hiện chưa tái tạo đầy đủ các vùng bạc màu và chữ thêu cong như ảnh gốc. Nón da chưa có mức chi tiết vết mòn như ảnh. Mẫu chong chóng không mô phỏng khí động học; cánh được quay bằng animation nhẹ.

## Cách thử

Mở **VR / CAMERA → Xem bàn nón và tủ đồ**. Danh sách 01–10 cho phép đội từng mẫu; nút **Nhìn gần nón đang đội · desktop** mở góc quan sát đầu. Grip trong kính hoặc giữ E trên desktop vẫn dùng chung cơ chế lấy/thả. Các vật trưng bày không nhân bản solver vải.

Bàn đã mở rộng cho hai hàng, năm mẫu mỗi hàng. Nón được dựng ở kích thước đội thật, không dùng ảnh phẳng làm phụ kiện. Hình [bàn nón](HatTable.png), [toàn cảnh](Showroom.png), và [nón xanh khi đội](Worn_01.png) là render Unity.

## Căn đầu và kiểm tra

Ảnh đội lần đầu phát hiện tâm nón bị lệch về sau vì trục thân không trùng tâm đầu. Đã đo vùng đầu từ mesh hiện có và đặt lại mốc theo tâm tiết diện đó; không ẩn hoặc cắt phần đầu để vượt kiểm tra. Mỗi loại nón có độ hạ xuống đầu riêng. Chữ 3D dùng depth test để chữ trên tủ không vẽ xuyên người/nón ở phía trước.

[Kết quả cuối](verification.json), [log cuối](final.log), [hash nguồn](source_snapshot.json):

- Khởi tạo đủ **10 nón + 6 mẫu đồ**, kiểm tra vị trí đỉnh hữu hạn.
- Đội đủ **10/10** bằng lựa chọn; kiểm tra Grip, thả, thay nón, reset và chọn đủ sáu trang phục.
- Phép thử vật lý thả nón xanh lên bàn/sàn đạt; không phải chứng nhận va chạm mọi hình nón khi ném nhanh.
- Năm kiểu lưỡi trai: điểm đầu đã bake phía trên mép mở nằm trong bao ellipsoid chóp, giá trị chuẩn hóa lớn nhất **0,9746 < 1**. Đây là phép kiểm bao lấy mẫu, chưa phải kiểm va chạm từng tam giác liên tục hoặc tóc/avatar khác.
- **11 mesh**, tổng **45.102 đỉnh** cho hình học nón và rotor, dùng chung vật liệu theo nhóm. Chữ TextMesh được tính riêng. Không thêm cloth solver cho mười nón.
- Vẫn đủ **10 nút VR**, Standalone OpenXR loader và Touch Profile bật. Chưa kết nối kính thật hoặc đo FPS trong kính.

Không dùng các ảnh cận để kết luận mọi góc nhìn/động tác đều không xuyên. Các ảnh tham chiếu không đủ để đo tuyệt đối kích thước/phom sản phẩm. File và code thay đổi trực tiếp trong project; không cần tải lại ZIP.

Phiên Play cũ trong Editor đã phát sinh lỗi mất solver khi Unity nạp lại script giữa lúc chạy. Đã Stop, refresh rồi Play mới để tạo lại trạng thái; [log của phiên Play mới](main_fresh_play.log) được tách riêng, không có lỗi C# hoặc exception trong đoạn kiểm tra đó. Đây là lý do hướng dẫn yêu cầu Stop/Play sau cập nhật code, thay vì tiếp tục dùng các object từ phiên Play cũ.

## Kết nối VR

Đọc [hướng dẫn từng bước Quest Link](../../Docs/KET_NOI_QUEST_LINK.md). Radeon 860M hiện chưa nằm trong danh sách GPU Meta công bố hỗ trợ Link; cần xác nhận Link chạy được trên máy trước khi thử scene trong kính.
