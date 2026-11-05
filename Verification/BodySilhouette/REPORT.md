Đã kiểm tra đứng yên rồi tăng nhịp 0,35× / 0,50×: [cách thử và phạm vi đo](../../Docs/SPEED_CONTACT_GUIDE.md).

Giao diện và tủ mới: [bố cục atelier, sáu mẫu thật và menu VR tiếng Việt](../AtelierPolish/REPORT.md).

Bản mới nhất về vải: [vật liệu/chuyển động vải, hiệu năng và tiếp xúc toàn thân](../MaterialMotion/REPORT.md), [hoàn thiện nách mẫu 06](../UnderarmFix/REPORT.md). Các số đo trong báo cáo dưới đây thuộc bản cũ; không dùng để mô tả runtime hiện tại.

Bản sửa tiếp theo: [nách áo, cổ sát vai, eo liền và bề mặt vải dệt](../RealismPass/REPORT.md). Ảnh và kết quả dưới đây thuộc bản trước đợt sửa đó.

# Hình thể mannequin và phom áo — 28/10/2026

Đã chỉnh trực tiếp vùng ngực của mannequin kỹ thuật và căn lại mẫu 06. Mục tiêu là phần ngực tròn, mềm hơn khi bỏ đồ, còn áo có đường cong ôm vừa thay vì phồng thành một khối cao lên phía vai. Ảnh người mặc áo được dùng để tham khảo độ ôm/nếp vải, không dựng lại cơ thể người trong ảnh.

## Những gì đã đổi

- Làm mượt cục bộ trong không gian 3D và giảm nhẹ phần nhô nhọn ở ngực của body. Có 1.795/53.514 đỉnh thay đổi, độ dịch chuyển lớn nhất khoảng 5,8 mm. Vị trí ngoài vùng sửa giữ nguyên; UV, trọng số xương, rig và topology giữ nguyên từng byte.
- Body vẫn kín, hướng tam giác nhất quán, không có mặt bị lật. Số đo 38 tiết diện/64 hướng được đo lại từ body mới, không dùng bảng va chạm cũ.
- Giảm độ dư ban đầu của áo mẫu 06 và giảm bán kính bao làm tròn, đặc biệt theo chiều dọc; phần ngực không còn bị đẩy thành một bầu áo lớn hướng lên vai. Cache căn áo và normal được tạo lại đúng hash lưới mới.
- Thêm nếp hình học nhỏ, tối đa 0,9 mm, ở vùng sườn/nách. Đây là chi tiết được dựng sẵn để tham khảo, chưa phải nếp nhăn học từ ảnh hoặc mô phỏng vải co giãn thật.
- Cổ, nẹp và chỉ đổi cùng tông khi xem phom vải sáng; nút kim loại vẫn giữ vật liệu riêng.

## Xem trong Unity

Trong **TRANG PHỤC → mẫu 06**:

1. **Nhìn gần cổ áo và nẹp nút** để xem phần thân trên.
2. **Xem phom · vải sáng** để thấy đường cong/nếp vải rõ hơn.
3. **Bỏ đồ · xem phom mannequin** để kiểm tra cơ thể; bấm lại mẫu 06 để mặc lại.
4. **Khôi phục màu đen theo ảnh** để về màu tham chiếu.

Ảnh body: [trước — góc bên](Before_BodySideClose.png), [sau — góc bên](After_BodySideClose.png), [sau — góc trước](After_BodyClose.png). Trang phục: [vải sáng — bên](Polo_LightSide.png), [vải sáng — trước](Polo_LightFront.png), [đen — bên](Polo_BlackSide.png).

## Kiểm tra

[body_refinement.json](body_refinement.json) ghi hash và kiểm tra tính toàn vẹn của body. [profile_change.json](profile_change.json) ghi các số đo thay đổi. Body và áo không tăng số đỉnh/hạt vật lý theo mỗi frame; việc làm mượt, căn và tính normal được thực hiện offline.

Unity đã chạy **3.240 bước chậm trên cả sáu trang phục**, không ghi nhận điểm nằm trong collider đại diện; độ giãn lớn nhất khoảng 1,281. Kết quả: [verification.json](verification.json).

Đối chiếu đỉnh và tâm tam giác với body đầy đủ ở 18 tư thế đứng/đi được ghi riêng trong [body_contact_broadphase.json](body_contact_broadphase.json). Bộ kiểm tra loại điểm chắc chắn nằm ngoài bao lồi chứa toàn bộ cơ thể, sau đó dùng signed distance trên body gốc cho các điểm còn lại; đã đối chiếu cách này với bộ kiểm tra trước và thu cùng kết quả. Không ẩn da để làm kiểm tra đạt.

Kết quả trên body mới: **286.122 mẫu, 0 mẫu xuyên quá 1 mm; độ xuyên lấy mẫu lớn nhất là 0 mm**. Đây là kết quả ở các tư thế lấy mẫu, không phải chứng nhận va chạm tam giác liên tục. [source_snapshot.json](source_snapshot.json) xác nhận body, rig, số đo, cache áo và code runtime của dự án chính trùng bản kiểm thử. Kiểm tra màu preview cuối chỉ thay vật liệu; không thay các vị trí/topology đã xuất để đo tiếp xúc.

Đây vẫn là mannequin kỹ thuật và trang phục dựng tham chiếu. Chưa phải rập/lớp lót sản xuất, chưa có solver vải co giãn riêng cho thân áo/nách. Mẫu 06 giữ phần thân trên ổn định khi đi chậm; kiểm tra lấy mẫu không chứng minh không xuyên tam giác ở mọi chuyển động hoặc thao tác VR.
