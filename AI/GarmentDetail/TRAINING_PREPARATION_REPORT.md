# Kết quả chuẩn bị học cấu trúc trang phục · 27/10/2026

Đã triển khai dữ liệu và đường huấn luyện graph theo rập thật của nguồn, thay vì chỉ học hình bao ngoài. Đã có trọng số pilot thực sự cập nhật qua optimizer. **Chất lượng hiện chưa đủ để dùng mặc đồ chính xác trong game.**

## Phần đã làm

| Hạng mục | Kết quả |
|---|---|
| Manifest ứng viên có body mesh | 6.304: train 5.611, validation 352, test 341 |
| Graph pilot đã chuyển đổi | 24: train 16, validation 4, test 4 |
| Nội dung giữ lại | Rập JSON, đường cong, cặp cạnh may và cờ hướng may, nhãn đỉnh, panel, topology, UV |
| Căn chỉnh | Tâm tiết diện eo body; hệ trục trước/lên; tỉ lệ chiều cao đồng đều |
| Huấn luyện pilot | 8 epoch; 16 mẫu train, 4 validation; 15.907 tham số |
| Đánh giá mô hình trên test | 0 mẫu |
| Tổng ClothData kể cả graph pilot | 27.768.649.073 byte, khoảng 27,77 GB / hạn mức 30 GB |
| Unity | Cửa sổ xem mesh theo panel, đường may, nhãn neo, body và trạng thái trước/sau drape |

Mỗi graph có 27 đặc trưng: tọa độ/pháp tuyến ban đầu, khoảng cách tới các mốc body, nhãn đường may/biên/cổ/eo/nách, vai trò mảnh, số đo và thiết lập bending nguồn. Các vị trí target không nằm trong đầu vào. Bending b1/b500 là thiết lập nguồn, chưa quy đổi thành tên vải hoặc hằng số vật liệu SI.

Loss gồm vị trí đỉnh, chiều dài cạnh và cạnh may, pháp tuyến và khớp nhãn cổ/eo với target. Nhiều mảnh có thể gặp tại một nút may; dữ liệu giữ cấu trúc này. Những mặt không xác định chắc panel được gắn nhãn chưa biết thay vì đoán.

## Kết quả thực nghiệm

Train loss giảm **0,01987 → 0,00818**; validation loss giảm **0,02017 → 0,01044**. Đây là tổng loss có trọng số trên tọa độ chuẩn hóa, không phải phần trăm chính xác.

Đọc lại checkpoint và đo trên 4 mẫu validation:

| Chỉ số | Trung bình đều giữa 4 trang phục |
|---|---:|
| RMSE khoảng cách đỉnh của mesh trước drape so với target | 271,1 mm |
| RMSE khoảng cách đỉnh dự đoán so với target | 217,0 mm |

Mô hình có cải thiện baseline này nhưng sai lệch vẫn quá lớn; chỉ chứng minh đường nạp dữ liệu, gradient, lưu/nạp trọng số và suy luận hoạt động. Validation rất nhỏ và đã dùng chọn checkpoint, không thay thế test độc lập. Chưa thể nói mô hình đã học mặc áo/đầm tốt. Xem từng mẫu trong [verification.json](verification.json), lịch sử và cấu hình trong [training_report.json](Artifacts/training_report.json).

## Kiểm tra đã thực hiện

- 24 graph: hash cache, dữ liệu hữu hạn, chỉ số mặt/cạnh hợp lệ, ownership panel, ánh xạ UV–graph và topology dựng lại.
- Đối chiếu trực tiếp một mẫu mỗi split với nguồn: rập/đường cong/cờ may, annotation, UV và cả mesh trước/sau drape được giữ. Sai lệch tọa độ lớn nhất khi khôi phục từ chuẩn hóa khoảng **0,000000107 m**.
- Hệ căn theo eo qua kiểm tra khứ hồi và cùng kết quả khi xoay/dịch cả body lẫn bộ đồ; khoảng cách chỉ thay theo tỉ lệ đồng đều.
- Không trộn nhóm thiết kế giữa train/validation/test trong manifest. Test chỉ được kiểm tra dữ liệu, không chạy mô hình hoặc chọn trọng số.
- Checkpoint nạp lại đúng schema/hash và dự đoán hữu hạn. Tổng dung lượng dưới 30 GB.
- Unity 2022.3.62f1 biên dịch và chạy render kiểm tra bằng CLI; ảnh tại [toàn bộ đồ](../../Verification/GarmentDetail-Preview.png) và [một panel](../../Verification/GarmentDetail-OnePanel.png). Đây là kiểm tra render, chưa thay thế kiểm tra mọi thao tác chuột trong cửa sổ Editor.

## Công việc còn lại trước khi áp vào mannequin trong game

1. Mở rộng train/validation sau khi kiểm tra toàn bộ graph; bổ sung mô hình có ngữ cảnh xa hơn giữa các mảnh để học đúng phom dài và nhiều lớp.
2. Thêm loss/kiểm tra tiếp xúc body, tam giác váy–body, self-contact và kiểm tra theo chuyển động. Không mặc định target mô phỏng nguồn đã sạch xuyên vải.
3. Retarget theo các mốc cổ–vai–eo–hông và số đo mannequin đích, giữ chiều dài tay/gấu, khoảng rộng cử động và các chi tiết bất đối xứng; kiểm tra độc lập trước khi đưa vào tủ đồ runtime.
4. Thêm dữ liệu người mặc và váy theo thời gian, cùng ảnh/rập/mesh đúng bộ đồ. Bộ sphere_simple hiện chỉ được xem, chưa dùng train vì giấy phép dữ liệu chưa rõ.
5. Thêm chi tiết bề mặt có nguồn cụ thể: nút, khóa, gấu, lót, texture UV, vật liệu. Graph nhiều đỉnh phục vụ hình học, không thay cho thiết kế cấu tạo và asset hoàn thiện.

Việc chỉnh sửa ở đợt này nằm trực tiếp trong dự án hiện tại. Runtime PBD và các mẫu procedural đang có chưa được thay bằng mô hình này. Hướng dẫn mở và chạy lại ở [README](README.md).
