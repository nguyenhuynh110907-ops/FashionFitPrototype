# Bàn nón và tủ chọn đồ cho VR — 28/10/2026

Bản cập nhật mới: [10 nón theo ảnh tham chiếu](../ReferenceHats/REPORT.md). Báo cáo dưới đây ghi bản bàn ba nón đầu tiên; số lượng và ảnh không đại diện bản hiện tại.

Đã thêm trực tiếp vào scene tạo lúc chạy: một bàn mặt đá sáng/chân kim loại, ba nón có thể cầm, một tủ gỗ mở với sáu mẫu chọn đồ. [Hướng dẫn Quest Link và thao tác](../../Docs/VR_SHOWROOM_QUICK_START.md).

## Hành vi

- Grip phải lấy vật gần tay hoặc nhắm tia lấy từ xa tối đa 4 m. Nón/mẫu đồ tiến về gần tay, giữ hướng tương đối khi xoay tay. Không tạo bản sao vô hạn.
- Thả nón gần đầu mannequin trong 30 cm để đội; Grip lại để tháo. Đổi nón trả nón cũ về bàn.
- Thả mẫu đồ gần thân mannequin trong 65 cm để mặc bộ đầy đủ; mẫu treo được trả lại tủ. Sáu mẫu dùng cùng sáu bộ đang có, không thêm model hãng mới.
- Thả ở ngoài vùng mặc: vật trở thành rigidbody có trọng lực, va chạm bàn/sàn. RESET PROPS đưa chúng về chỗ trưng bày.
- Trigger phải dùng bảng nút; thêm TABLE / RACK và RESET PROPS. Tia đổi màu khi chọn/cầm; gửi xung rung nếu tay cầm hỗ trợ.
- Desktop có phím E dùng chung đường chọn/giữ/thả để kiểm tra trước khi kết nối kính. Có nút đội nón/chọn bộ trực tiếp.
- Bàn/tủ có collider chặn tia và vùng chặn di chuyển bằng cần. Không chặn chuyển động cơ thể thật.

Mẫu trong tủ là hình học đại diện thu gọn, không phải sáu bộ cloth được mô phỏng đồng thời. Sau khi mặc vẫn chỉ có một solver váy đang hoạt động; áo Ivory bổ sung solver riêng như trước. Nón là vật cứng, không mô phỏng vải/nỉ biến dạng.

## Kiểm thử Unity

Chạy `FashionFit.Editor.ShowroomVerification.RunBatch` trong bản sao project Unity 2022.3.62f1. [Kết quả JSON](verification.json), [log cuối](final.log), [hash code](source_snapshot.json).

| Trường hợp | Kết quả |
|---|---|
| Khởi tạo bàn, tủ, vật tương tác | 3 nón, 6 mẫu đồ, collider/rigidbody hợp lệ |
| Grip bằng tia và Grip sát tay | Đạt |
| Lấy xuyên mặt bàn từ dưới | Bị chặn |
| Thả nón lên bàn và xuống sàn | Không rơi xuyên trong bài đo physics |
| Thả gần đầu để đội, thay nón cũ | Đạt |
| Grip và đem sáu mẫu tới thân để mặc | 6/6 lựa chọn đúng |
| RESET PROPS | Không còn vật đang cầm/đội/rơi |
| Chặn di chuyển vào bàn | Đạt kiểm tra vị trí |
| OpenXR loader Standalone và Touch Profile | Có và được bật |
| Khởi tạo bảng nút VR | Đủ 10 nút, gồm TABLE / RACK và RESET PROPS |
| Solver váy sau vòng thử | 1, không nhân bản solver khi Grip mẫu treo |

Ảnh render: [toàn cảnh](Showroom.png), [bàn nón](HatTable.png), [tủ đồ](Wardrobe.png). Ảnh là render từ scene kiểm thử, không phải ảnh từ headset.

Project chính đã được mở lại, biên dịch thành công và chạy Play. Đã bấm tab VR/CAMERA, nút xem bàn/tủ, đội Bucket và đổi góc quan sát để kiểm tra nón trên mannequin. Không ghi nhận lỗi C# hoặc exception runtime trong lần kiểm tra này.

## Phạm vi chưa xác nhận

Chưa có headset/controller thật để đo tracking, haptics, vị trí Grip hoặc FPS trong kính. Kiểm tra tự động gọi cùng đường xử lý Grip/physics; không chứng minh việc ghép nối USB hay runtime Meta trên PC đã thành công.

Unity báo Radeon 860M, chưa nằm trong danh sách GPU hỗ trợ Link của Meta. Cần kiểm tra môi trường Link hoạt động trước; không coi việc loader/profile có sẵn là bằng chứng headset đã kết nối. [Yêu cầu Meta](https://www.meta.com/help/quest/articles/headsets-and-accessories/oculus-link/requirements-quest-link/).

Nón fit theo một mốc đầu đo từ bounds và bám xương đầu; chưa có bộ giải tiếp xúc nón/tóc cho mọi avatar nhập ngoài. Vùng nhận mặc dùng khoảng cách tới mốc, chưa có thao tác mặc áo từng tay hoặc mô phỏng kéo cả bộ từ móc lên cơ thể. Đường vải cũ không thay đổi trong đợt này.
