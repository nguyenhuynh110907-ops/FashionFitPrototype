Cập nhật 01/11/2026: đã có [mẫu áo dài procedural thử trong Unity, chọn từng lớp và Grip](../../Docs/LAYERED_AODAI_VR_GUIDE.md). Hồ sơ nghiên cứu phía dưới giữ nguyên; chưa train AI/rập số hóa hoàn chỉnh.

# Dữ liệu áo dài - giai đoạn nghiên cứu

Đã thu thập gói tham chiếu cho **một áo dài nữ tay raglan, cổ đứng, hai tà và quần riêng**. Đây là đề xuất bản dựng đầu, chưa phải mẫu mặc được trong Unity hoặc model AI mới huấn luyện.

- [Báo cáo kết quả](../../Verification/AoDaiResearch/REPORT.md)
- [Xem ảnh và tài liệu đã tải](../ClothData/Data/AoDaiResearch/BROWSE.html)
- [Thiết kế mảnh, lớp và va chạm đề xuất](aodai_blueprint.json)
- [Kiểm tra nguồn và các điểm cần giữ lại để xác minh](source_review.json)
- [Danh mục gốc, giấy phép, SHA256](../ClothData/Data/AoDaiResearch/manifest.json)
- [6 ví dụ rập hỗ trợ, giữ nguyên official train split](../ClothData/Data/AoDaiResearch/support_patterns/index.json)

Chạy kiểm tra cục bộ từ thư mục project:

```powershell
.\.ml-runtime\python\python.exe AI\AoDaiResearch\verify_references.py
```

Raw data và bản xem ảnh nằm ngoài `Assets`, được quy tắc `AI/ClothData/Data/` hiện có bỏ qua khi đưa lên Git. PDF công khai chỉ dùng đọc tham khảo vì chưa xác minh quyền huấn luyện/phân phối lại. Ảnh Commons được lưu kèm giấy phép từng tệp. Dữ liệu GarmentCodeData là mẫu tổng quát có rập/mesh, không đổi nhãn thành áo dài.

Chưa đủ dữ liệu để train chuyển động áo dài: thiếu rập áo dài số hóa theo số đo mannequin, chuỗi mesh động nhiều lớp và phép đo cơ học của cùng loại vải. Bước thực hiện phù hợp tiếp theo là dựng rập riêng, mô phỏng may và hiệu chuẩn một bộ trước khi mở rộng tập huấn luyện.
