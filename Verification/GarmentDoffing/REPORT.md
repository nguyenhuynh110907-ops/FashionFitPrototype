Đã triển khai bản blouse thử: xem [kết quả và giới hạn thực tế](IMPLEMENTATION_REPORT.md). Nội dung dưới đây là đợt rà soát trước triển khai.

# Rà soát mở nút và cởi trang phục trong VR

## Kết luận

**Có thể phát triển, nhưng bản hiện tại chưa hỗ trợ tháo nút và cởi đồ vật lý.** Đây là kết quả đọc code và đối chiếu tài liệu, chưa phải kết quả chạy thử tính năng trên kính. Đề xuất thực hiện trước một áo blouse mở toàn thân; sau khi vượt kiểm tra mới áp dụng cho áo dài và các bộ còn lại.

## Hiện trạng đã xác nhận

| Hạng mục | Code hiện tại | Hệ quả |
|---|---|---|
| Nút mẫu polo | `TailoredPoloStudy.BuildButtons`, dòng 241: bốn nút được ghép chung vào mesh chi tiết | Chỉ là hình học trang trí; không có trạng thái mở/đóng hoặc ràng buộc nút-khuy |
| Thân polo | `BuildUpper`, dòng 153: tạo vỏ theo bề mặt cơ thể; `Skin`, dòng 126 gắn vào xương | Không có hai mép vải mở được theo hàng nút; phần chuyển động phụ chỉ dịch nhỏ theo pháp tuyến |
| Áo Ivory | `InteractiveShirt`, dòng 47, 51, 129: vòng trên cố định, hỗ trợ thân, neo theo Spine2 | Kéo cục bộ được, nhưng không trượt khỏi vai/tay như áo thật |
| Grip vải | `FittingClothSolver`, dòng 199, 217: bỏ qua vùng cố định, giới hạn mục tiêu kéo 12 cm từ điểm bắt đầu | Giới hạn hiện tại phục vụ kiểm tra phom, không phải đường kéo dài để cởi |
| Hai tay | `RunwayVRGrabTarget` có hai owner, áo/váy có hai vùng kéo | Tận dụng được phần quản lý tay; chưa có thao tác nút/khuy |
| Nút Bỏ trang phục | `RunwayWardrobe.ClearGarment`, dòng 179-183: xóa object, mesh/material sở hữu | Đồ không còn để cầm, thả, đặt lên bàn hay mặc lại |
| Đồ trên tủ | `RunwayShowroom.TryWear`, dòng 125-131 | Nón là pickup đội/tháo; mẫu quần áo trên tủ hiện là vật chọn preset, chưa phải cùng một bộ vải được chuyển qua lại |
| Khi dừng motion | `FashionFitApp.AdvanceSimulation`, dòng 105 | Đồ đã cởi phải có vòng cập nhật/sleep riêng, nếu không có nguy cơ đứng giữa không trung khi hết thời gian settle |
| Mẫu 3D nhập | `TransferredGarmentAsset` và `WearTransferred` chỉ cho kiểm tra phom tĩnh | Chưa có metadata khuy, đường mở, lưới mô phỏng hay cởi vật lý |
| Mannequin pose nam/nữ | `RunwayPoseStudio` tạo riêng với mẫu đang fit | Cần một garment instance gắn đúng avatar, không dùng chung trạng thái tủ toàn cục |

Danh sách dòng code, trích đoạn và SHA256: [source_audit.json](source_audit.json). Không sửa runtime trong đợt rà soát này.

## Phương án xử lý đề xuất

### 1. Làm phần cài có tác dụng lên vải

Phải có **hai mép lưới độc lập** ở đường mở; tam giác không được bắc ngang đường đó. Tách ràng buộc may vĩnh viễn khỏi ràng buộc nút/khuy có thể nhả. Mở một nút chỉ nhả đúng phần cài tương ứng; nút vẫn đính trên một bên áo, không biến mất hoặc rơi rời khỏi áo. Khi mở, phải bỏ cả liên kết kéo/uốn giả đang nối tắt qua đường mở. Cài lại chỉ được khi hai mép đủ gần và đúng hướng.

Không thể chỉ ẩn mesh nút hoặc xóa spring rồi giữ nguyên mặt tam giác liền phía dưới. Không tự coi chi tiết được vẽ trong texture là nút có chức năng.

**Lưu ý mẫu polo:** hàng bốn nút chỉ nằm vùng cổ/ngực. Mở hết không làm thân áo thành áo khoác mở toàn thân. Nếu giữ đúng thiết kế thì phải cởi qua đầu và tay, có đủ khoảng mở ở cổ/cửa tay; nếu muốn bản thử đơn giản hơn, dùng một mẫu blouse mở toàn thân riêng.

### 2. Thao tác bằng hai tay Meta Touch

Một tay giữ nẹp bằng Grip, tay kia đưa sát nút và bóp Trigger để mở; đổi tay được. Hai Grip vẫn giữ hai vùng vải độc lập. Dùng highlight tại nút, rung ngắn và gợi ý ngay cạnh phần cài. Mốc thử ban đầu cho nút là khoảng **3 cm**, phải hiệu chỉnh theo vị trí ngón trỏ ảo và tay cầm thực tế.

Không dùng nguyên `RunwayVRButton` của menu cho nút áo: menu hiện nhận Trigger qua tia tới 4 m. Cần ưu tiên thao tác phần cài ở gần, kiểm tra che khuất/mặt tiếp cận và tránh một lần bóp kích hoạt cả menu lẫn nút. Một nút chỉ có một thao tác đang xử lý; giữ Trigger không được mở-đóng liên tục. Không diễn giải áp lực Grip thành lực Newton thực đo.

### 3. Cởi dần theo cấu trúc và khoảng hở

Đề xuất các trạng thái độc lập: phần cài mở/đóng; trang phục đang mặc/đang cởi/đã rời; vị trí ở người/tay/sàn/kệ. Mở nút chưa đồng nghĩa đã cởi.

Cho mannequin đứng yên hoặc vào tư thế hỗ trợ cởi do người dùng chọn. Khi phần cài bắt buộc đã mở và vùng cổ/ống tay có thể thoát ra, giảm dần điểm neo theo vùng vai/tay/thân, giữ các đường may thật. Không nhả toàn bộ điểm neo ngay khi bấm nút cuối. Nếu tay còn mắc trong cửa tay hoặc nút chưa mở thì chỉ rõ chỗ mắc; không kéo xuyên cơ thể để hoàn tất thao tác.

Khi áo rời người, chuyển vị trí/vận tốc từ tọa độ body sang không gian phòng mà không nhảy hình; ngắt skinning và neo hoạt hình cũ. Giữ một nguồn điều khiển biến dạng cho cùng lưới, tránh vừa skin theo mannequin vừa bị Rigidbody kéo độc lập.

### 4. Giữ trọng lực, đồ vật và vật liệu sau khi cởi

Áo đã cởi phải tiếp tục tồn tại, rơi khi buông tay cuối, chạm bàn/sàn, có thể nhặt và đặt lên móc. `ClearGarment` cần tách khỏi thao tác cởi; chỉ xóa tài nguyên khi người dùng thực sự xóa đối tượng. Vật liệu và texture được quản lý theo vòng đời garment instance.

Solver hiện dùng gravity theo -Y cục bộ. Khi áo xoay tự do phải dùng hướng trọng lực đúng trong không gian mô phỏng và giữ vận tốc khi đổi parent. Đồ ngoài mannequin cần được cập nhật riêng, chỉ sleep khi đã ổn định. Không tự mặc lại chỉ vì thả gần thân: phải có thao tác mặc lại rõ ràng.

### 5. Kiểm soát xuyên vải trong mọi trạng thái

Phải duy trì tiếp xúc cơ thể-vải, tự va chạm, vải-vải giữa các lớp và với vật thể trong phòng. Đặc biệt kiểm tra nách, bàn tay/cửa tay, cổ, mép nẹp, gốc xẻ tà. Dùng lưới mô phỏng thấp hơn lưới hiển thị, broadphase, substep theo chuyển động; không thêm Rigidbody cho từng đỉnh hoặc tăng điểm vô hạn.

`RunwayBodyCoverage` đang giữ được body gốc nhưng có thể ẩn tam giác khi mặc. Khi mở áo cần hiện lại vùng cơ thể tương ứng hoặc tắt mask trong suốt thao tác; không để lộ lỗ rỗng ở thân và không dùng mask để che lỗi va chạm. Kết quả chống xuyên phải đo trên hình học đầy đủ, gồm mặt/cạnh, không chỉ các đỉnh hoặc capsule thô.

## Chọn nền mô phỏng

Đề xuất mở rộng đường mô phỏng PBD/XPBD hiện có sang mesh may nhiều mảnh, có fastener constraints và garment instance độc lập. Đây là thay đổi nền mô phỏng, không phải vá thêm vài collider vào nút. Chưa có cơ sở để hứa FPS hoặc không xuyên tuyệt đối.

Unity Cloth có constraints, self/intercollision, nhưng va chạm với thế giới cần collider được khai báo và tương tác với body là một chiều; chỉ thêm component này không tạo ra logic khuy hay quy trình cởi. [Tài liệu Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Cloth.html).

Có thể tham khảo [PositionBasedDynamics](https://github.com/InteractiveComputerGraphics/PositionBasedDynamics), giấy phép MIT, cho constraint PBD/XPBD và tiếp xúc SDF. Đây là thư viện C++ tham khảo, không phải plugin Unity cài là có ngay; chưa tải hay đưa thư viện này vào dự án.

Không cần train AI để quyết định mở một nút đã được khai báo. Ưu tiên rập/mesh và metadata có kiểm chứng; mô hình AI nhận dạng chi tiết có thể hỗ trợ nhập dữ liệu về sau, không thay thế việc kiểm tra đường mở và khả năng cởi.

## Đầu vào cần cho từng bộ đồ

Lưu ID mảnh/lớp, UV/material, đường may cố định, hai mép mở, vị trí nút-khuy, loại phần cài, vùng neo, đường cởi dự kiến và kích thước cổ/cửa tay. Mesh FBX/GLB/OBJ liền kín không đủ: cần tác giả đánh dấu hoặc chỉnh topology trước. Với áo dài, dùng đúng phần cài bên cổ/nách/sườn theo mẫu được chọn, giữ nguyên cấu trúc tà và quần; không tự khoét đường mở giữa thân cho tiện.

## Lộ trình và kiểm tra nghiệm thu

1. Dựng một blouse mở toàn thân và dữ liệu phần cài; kiểm tra mở/cài từng nút và hai tay khi mannequin đứng yên.
2. Thử cởi tay trái/phải, cầm hai tay, buông một tay, buông cả hai, rơi xuống bàn/sàn, nhặt và mặc lại. Kiểm tra mất tracking, thao tác từ sau lưng/qua tường, và giữ Trigger lâu.
3. Kiểm tra mannequin di chuyển sau khi cởi: áo không chạy theo body; thay bộ mới không xóa áo cũ hoặc làm mất texture. Kiểm tra vùng thân mở không bị rỗng do body mask.
4. Đo va chạm tam giác-cơ thể, cạnh-cạnh và giữa các lớp khi cởi. Mục tiêu thử ban đầu là không có mẫu đo xuyên sâu hơn 1 mm; thất bại thì chưa tăng tốc. Đây là tiêu chí kiểm thử dự kiến, chưa phải kết quả đạt.
5. Đo frame time P95/P99 theo tần số kính PCVR thực tế. Sau khi bản blouse đạt, mới áp dụng cho áo dài nhiều lớp và hai mannequin pose nam/nữ, rồi kiểm tra lại trước khi tăng tốc bước đi.

[implementation_plan.json](implementation_plan.json) ghi trường dữ liệu, phân tách trạng thái, thao tác và kịch bản nghiệm thu. **Đợt này chỉ kiểm tra và báo cáo phương án; chưa có chức năng mở nút/cởi đồ mới để thử trong Unity.**
