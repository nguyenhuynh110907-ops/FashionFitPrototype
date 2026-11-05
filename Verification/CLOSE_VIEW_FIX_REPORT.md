# Lỗi mất thân đầm khi nhìn gần và cổ áo Ivory

## Nguyên nhân đã tái hiện

Đã dựng lại góc máy cao 1,65 m, cách mannequin 0,8 m, FOV 50°, xoay bốn hướng. Ảnh Before tái hiện mảng người bị khuyết và mất thân trên của đầm giống ảnh người dùng.

Bounding box của ba `SkinnedMeshRenderer` thân đầm được đặt bằng tọa độ model nhưng Unity sử dụng hệ của root bone. Hộp culling thực tế có tâm Y khoảng **2,13 m**, trong khi thân đầm nằm quanh **1,21–1,22 m**. Toàn bộ **1.024/1.024 đỉnh** của mỗi thân đầm nằm ngoài hộp. Khi camera gần không nhìn thấy hộp sai, renderer không vẽ thân đầm. Phần che da bật ở bản trước khiến lỗi thành một mảng thân người trống.

Cách Unity dùng root bone cho vùng bounds được thể hiện trong [mã Inspector chính thức](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/SkinnedMeshRendererEditor.cs). Lỗi được xác nhận bằng số đo và ảnh trong Unity của chính dự án, không chỉ suy đoán từ tài liệu.

## Sửa runtime

- Chuyển tám góc của hộp đo trong model sang hệ root bone trước khi đặt `localBounds`; áp dụng cả body và thân đầm. Cập nhật lại bounds khi đổi mesh body/coverage. Không bật cập nhật mesh ngoài màn hình cho mọi frame.
- **Tắt che da mặc định.** Body đầy đủ được dựng; tùy chọn che vùng đồ kín vẫn có thể bật bằng tay.
- Thu cổ áo Ivory theo tiết diện cổ, giảm proxy cổ và phân biệt khoảng hở cổ với thân trên. Vẫn giữ tiếp xúc vai; giảm khoảng hở theo độ cao thay vì bỏ hẳn va chạm cổ–vai.
- Căn phom nghỉ áo với bao body trước khi tính constraint; sửa lỗ tay và làm tròn giới hạn các điểm biên của mesh hiển thị. Sau làm tròn vẫn đẩy điểm ra khỏi va chạm. Giữ 384 điểm physics, không tăng mật độ mô phỏng.
- Đổi rộng/dài bằng hình học quanh đường cổ, không scale toàn object quanh sàn. Cổ giữ vị trí khi thử hai cực của thanh chỉnh.
- Sửa lỗi chỉ số của bảng tiết diện tại góc âm rất nhỏ: phép wrap float có thể thành cột 64 ở hàng cuối. Nay wrap chỉ số nguyên; có test góc gần 0 và bội của 2π.

## Kiểm chứng

Ảnh và báo cáo ở [CloseView](CloseView). `Before` ghi 0 pixel nhận diện thân đầm ở các góc lỗi. `After` kiểm tra bốn bộ có thân trên, bốn hướng mỗi bộ: **16 góc**, đỉnh trong bounds và thân trên thực sự có pixel render. Kiểm tra riêng chỉnh rộng/dài giữ cổ trong sai số 2 mm.

Đã chạy lại chuyển động năm bộ; áo Ivory được kiểm tra riêng sau các chỉnh cuối. File kết quả gốc và lần kiểm tra áo cuối được giữ riêng trong `CloseView/Motion`; không lấy ảnh xa thay cho bài nhìn gần. Các phép xuất body và kiểm tra tiếp xúc dùng **body gốc**, không dùng mesh đã che da.

Lần đo cuối riêng áo có **3 pose / 34.758 mẫu bề mặt**, không ghi nhận mẫu xuyên body; max stretch khi đi khoảng **1,206** lần. [QA áo cuối](CloseView/Motion/final_shirt_source_body_contact.json), [motion áo cuối](CloseView/Motion/final_shirt_motion.json), [QA tổng hợp và hash runtime](CloseView/verification.json). Lần đo đủ 15 pose trước chỉnh cuối áo được giữ ở `source_body_contact_initial_full.json`; kết quả cũ không thay cho lần đo áo cuối. Đây vẫn là kiểm tra các đỉnh và tâm mặt tại pose lấy mẫu, chưa chứng nhận toàn bộ tam giác/chuyển động liên tục.

Ảnh đối chiếu: [đầm trước sửa](CloseView/Before/Look2_View0.png), [đầm sau sửa](CloseView/After/Look2_View0.png), [áo nhìn bên sau sửa](CloseView/After/Look5_View1.png).

Thử nghiệm chạy ở một project kiểm tra riêng trong `Verification/CloseViewProject` để không đóng Editor người dùng. Source runtime đã được sửa tại project chính. Đây là kiểm tra CLI/render, chưa phải thực nghiệm headset. Thời gian chạy khi nhiều Unity cùng mở không dùng làm số FPS cam kết.

## Dữ liệu học chi tiết tiếp theo

Đã tạo [SurfaceDetails](../AI/GarmentDetail/SURFACE_DETAIL_DATA.md): 24 graph có nhãn cấu tạo, gồm **20 đầm xác nhận nối may** (12 train/4 validation/4 test), **2.414 cạnh rập**, **18.142 cạnh ráp 3D**. Có 2.310 ứng viên để mở rộng và hàng đợi 100 ảnh để gán nhãn chi tiết. Tổng dữ liệu khoảng 27,79 GB, vẫn dưới 30 GB.

Những trường chưa có ground truth như nút, khóa, bước chỉ, gấu, lót và thêu được để chưa biết và bỏ khỏi loss. Chỉ chạy hai bước optimizer thử để kiểm tra đường dữ liệu; chưa lưu mô hình chi tiết hoặc áp AI vào game.

## Cách nhận bản sửa

Trong dự án chính, bấm **Stop → chờ compile → Play**. Giữ mặc định đứng yên, chọn Emerald/Rose/Sapphire rồi đi gần và xoay quanh để xem thân đầm; chọn Ivory để kiểm tra cổ/lỗ tay và hai thanh chỉnh áo. Chế độ trình diễn chậm giữ nguyên.
