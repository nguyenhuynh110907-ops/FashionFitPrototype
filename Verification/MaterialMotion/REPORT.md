# Chất liệu và chuyển động phụ của trang phục — 28/10/2026

Đã đưa ba cấu hình lụa nhẹ, cotton và len vào project chính, sửa vùng nách mẫu 06 và bổ sung nút so sánh trong **VẢI / AI**. [Hướng dẫn thử](../../Docs/MATERIAL_PHYSICS_GUIDE.md).

## Thay đổi đã áp dụng

- Hạt vải có khối lượng theo diện tích tam giác và khối lượng vải trên đơn vị diện tích. Ràng buộc cấu trúc dùng XPBD, tách dọc/ngang/chéo và liên kết uốn hai bước; có giảm chấn theo thời gian, lực cản không khí và ma sát tiếp xúc gần đúng. Phần uốn này vẫn là mô hình đại diện, chưa phải năng lượng uốn theo góc nhị diện hay mô hình vải phi tuyến đầy đủ.
- Thân trước áo mẫu 06 có lưới chuyển động phụ 17 × 19, tổng 323 nút, dịch ra phía trước từ bề mặt đã fit. Biên, cổ, nẹp, eo và vùng nách được giữ ổn định. Giới hạn 6 mm cho lụa, 4,5 mm cho cotton và 3 mm cho len; không có trượt tiếp tuyến. Chỉ may bám theo bề mặt; nút/cổ/nẹp không trôi theo lớp này. Đây là chuyển động phụ có giới hạn, chưa phải mô phỏng toàn bộ rập áo.
- Shader thay độ bóng/độ nổi sợi theo chất liệu. Lớp chi tiết cổ/nẹp lấy thông số bề mặt tương ứng. Giữ UV, màu và các texture đang dùng.
- Thêm chuyển động đầu rất nhẹ khi đi. Mẫu 06 vẫn giữ thân trên và tay để tránh tái phát lỗi nách; chưa có mocap hay chuyển động tay tự nhiên đầy đủ.
- Giảm công việc va chạm bằng bao AABB bảo thủ và bỏ phép căn bậc hai khi cặp tự tiếp xúc chắc chắn cách xa. Không giảm số vòng kiểm tra để đạt kết quả đo nhanh hơn.

## Dữ liệu thực và phần hiệu chỉnh

Đã lấy bảng đo MIT, chuẩn hóa 52 phép đo của 30 loại vải. Chọn ba mẫu tham chiếu cho bản thử. Khối lượng bề mặt lấy từ số đo; độ uốn chỉ dùng tương quan để hiệu chỉnh. Độ giãn, cắt, giảm chấn, ma sát, lực cản và roughness hiện do tác giả bản thử đặt, chưa được nhận dạng bằng thí nghiệm trên từng trang phục.

| Cấu hình | Khối lượng bề mặt | Mẫu MIT |
|---|---:|---:|
| Lụa nhẹ | 66,97 g/m² | 3 |
| Cotton | 99,65 g/m² | 5 |
| Len có cấu trúc | 384,58 g/m² | 6 |

Đã lưu thêm tài liệu tham số phi tuyến của 10 loại vải Berkeley làm nguồn nghiên cứu, chưa chuyển các hệ số đó vào runtime. Nguồn, đơn vị và giới hạn sử dụng nằm trong [báo cáo thu thập](../../AI/ClothData/PHYSICS_COLLECTION_REPORT.md). Không có đợt huấn luyện AI mới trong thay đổi này.

## Kết quả kiểm tra

Unity 2022.3.62f1, bài đo Editor trên máy hiện tại. [Kết quả cuối](verification.json), [trước tối ưu](before-optimization.json), [đối chiếu hình học](optimization_geometry_comparison.json), [hash nguồn](source_snapshot.json).

| Kiểm tra | Kết quả |
|---|---|
| Sáu trang phục, trình diễn chậm | 3.240 bước, 0 vi phạm collider đại diện |
| Ba chất liệu có gió trên sáu trang phục | 3.240 bước bổ sung, không lỗi số học/vi phạm proxy |
| Độ giãn lớn nhất trong chuỗi thử | 1,355 lần chiều dài nghỉ; vẫn cần cải thiện độ không giãn |
| Chuyển động phụ áo lớn nhất | 5,484 mm |
| Bao AABB so với kiểm tra không dùng bao, cảnh collider chuyển động | Sai khác vị trí lớn nhất 0 ở bài đối chiếu 180 bước |
| CPU tính vải trung bình trước/sau tối ưu | 19,50 → 12,87 ms, giảm khoảng 34% |
| CPU tính vải lớn nhất sau tối ưu | 25,56 ms |

Đây là thời gian **bước tính vải**, không phải thời gian toàn frame hay chứng nhận FPS. Mẫu 05 còn trung bình 18,92 ms trong bài đo; chưa đủ để cam kết VR 72/90 Hz. Bản so sánh trước/sau có sai khác đỉnh vải tối đa 0,124 mm do thay thứ tự phép tính số thực; đã kiểm tra tiếp xúc lại trên bản cuối.

Thử một mảnh vải cùng hình học, treo cố định mép trên, 4 giây có gió 0,32 m/s rồi 4 giây tắt gió:

| Chất liệu | Khối lượng mảnh | RMS lệch khỏi vị trí nghỉ lúc có gió | Tốc độ lớn nhất cuối giai đoạn tắt gió |
|---|---:|---:|---:|
| Lụa | 73,52 g | 17,52 mm | 11,60 mm/s |
| Cotton | 109,39 g | 15,47 mm | 3,45 mm/s |
| Len | 422,19 g | 12,90 mm | 0,206 mm/s |

RMS trên bao gồm cả tác dụng của trọng lực, không phải phép đo gió riêng. Các con số thể hiện ba cấu hình phản ứng khác nhau, chưa xác nhận khớp video thí nghiệm.

### Tiếp xúc với cơ thể

[Bản đo cuối toàn thân](Optimized/body_contact_broadphase.json): 18 tư thế của sáu mẫu, **286.125 điểm ở đỉnh/tâm tam giác**, body kín đầy đủ, không dùng mặt nạ. Không có mẫu xuyên quá 1 mm; độ xuyên lấy mẫu lớn nhất **0,6181 mm** ở mẫu 06. Không nên gọi kết quả này là không xuyên tuyệt đối.

[Vùng nách cuối](underarm_visibility.json): sáu góc nhìn 512 px, **0 pixel da lộ trong dải nách mục tiêu**, đối chứng bỏ áo 189.108 pixel; refresh cache giữ nguyên scene. [Chi tiết sửa nách và ảnh](../UnderarmFix/REPORT.md).

Các phép đo lấy mẫu không thay thế va chạm tam giác liên tục; chưa kiểm tra toàn bộ thao tác VR hoặc mọi chất liệu tại mọi góc cơ thể. Chưa chạy trên kính thật.

## Giới hạn và hướng tiếp theo

Mannequin vẫn là mô hình kỹ thuật; mẫu 06 vẫn là hình học dựng tham chiếu, chưa đạt chất lượng nhân vật/quần áo chụp thật. Mẫu 05 còn mép vai/lỗ tay cứng và bề mặt bóng rõ trong ảnh thử lụa; bản vật liệu này chưa giải quyết cấu trúc rập đó. Cần chuyển sang rập có đường may, rig/sleeve fit tốt và solver tiếp xúc tự va chạm ở tam giác trước khi mở rộng chuyển động tay. Không thể đạt chất lượng đó chỉ bằng tăng số điểm.

Với file 3D của sinh viên, ưu tiên giữ nguyên UV, normal, PBR và chi tiết gốc; kiểm tra đơn vị, hướng mặt và rig rồi mới gán vật liệu vật lý. Bản thử không tự suy ra độ co giãn hay tự biến mọi OBJ thành trang phục động. Bước tiếp theo nên là hiệu chỉnh một bộ rập thật bằng video thử vải và kiểm tra hiệu năng trên kính, trước khi mở rộng huấn luyện mô hình vật liệu.
