# Thử độ rủ và cảm giác vải

Đã sửa trực tiếp project hiện tại. Không cần tải lại hoặc giải nén bản khác.

## Thử trong Unity

1. Nếu đang chạy bản cũ, Stop, chờ Unity compile rồi Play lại.
2. Mở **VẢI / AI → CHẤT LIỆU VÀ ĐỘ RỦ**.
3. Chọn **Lụa nhẹ**, **Cotton** hoặc **Len có cấu trúc**. Các nút này đổi khối lượng, độ uốn, giảm chấn và phản ứng với không khí; không thay hoa văn/UV của bộ đồ.
4. Bấm **Bật quạt nhẹ**, đợi vài giây, rồi tắt để xem vải ổn định. Chọn lần lượt ba chất liệu trên cùng trang phục để so sánh.
5. Trong **BẮT ĐẦU**, bật **Trình diễn chậm** để xem chuyển động theo bước chân. Giữ chế độ chậm khi kiểm tra tiếp xúc.

Mẫu **05 Ivory** có thân áo mô phỏng và tương tác kéo VR đang có. Mẫu **06** có chuyển động phụ nhỏ ở mặt trước thân áo; cổ, nẹp, nút, eo và vùng nách giữ ổn định. Tay áo mẫu 06 vẫn đi theo rig, chưa phải một bộ rập được mô phỏng hoàn chỉnh.

Để nhìn rõ phom mẫu 06, vào **TRANG PHỤC**, chọn **Nhìn gần cổ áo và nẹp nút**, rồi **Xem phom · vải sáng**. Bản xem sáng dùng để kiểm tra bề mặt; có thể khôi phục màu đen trong cùng tab.

## Hiểu đúng bản thử

Độ mềm khác nhau có thể nhận thấy rõ hơn ở gấu váy/áo Ivory và khi bật quạt. Mẫu 06 giới hạn chuyển động phụ khoảng 3–6 mm nhằm giữ phom vừa sửa; không phóng đại nếp gấp để tạo cảm giác mềm giả.

Đây là hiệu chỉnh vật lý và shader có tham khảo số đo vật liệu, **không phải một AI mới được huấn luyện**. Chưa tự xác định chất liệu thật từ file sinh viên gửi. File 3D nhập ngoài vẫn cần kiểm tra rig, rập, đường may, UV và collision riêng.

[Báo cáo kết quả và giới hạn](../Verification/MaterialMotion/REPORT.md) · [Sửa vùng nách](../Verification/UnderarmFix/REPORT.md) · [Nguồn dữ liệu vật liệu](../AI/ClothData/PHYSICS_COLLECTION_REPORT.md)
