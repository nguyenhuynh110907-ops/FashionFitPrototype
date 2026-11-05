# Sửa mép áo, đường nối nón và bảng kéo giãn — V088

Mã log: `FITTING_READY_V088_SURFACE_READOUT`. Source vẫn ở `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`.

## Mở và xem số khi kéo

1. Thoát cửa sổ FashionFit cũ rồi mở **FashionFit VR → 01 - VAO GAME VR** trên Desktop. Vào Quest Link và bắt đầu VR như trước.
2. Bấm **Y → ĐÍCH MẶC / VẬT LIỆU**. Chọn đúng nhân vật nếu cần, rồi mở lại mục này → **Căng / nén vật liệu → Xem rõ thông số kéo / nén trực tiếp**.
3. Bảng số đứng chếch bên phải. **Grip không tự cất bảng ở trang thông số**, nên bạn có thể kéo và đọc số cùng lúc. Y cất/mở bảng; X đưa bảng về phía trước.
4. Nút **Đổi vật liệu** ở dưới bảng đổi đúng vật đang được giữ. Nếu không cầm vật, nó áp theo đích và loại đồ đang chọn. Nón đang cầm có ưu tiên; không tự áp lên mọi nhân vật.
5. Chọn **VẢI** để kéo áo/quần; **POSE** để chỉnh các điểm khớp. Grip thường nhấc nón; Trigger + Grip nón đang đội để chỉnh tại chỗ. Giữ hai tay trên một nón để kéo ngược hai phía hoặc nén; thả tay cuối thì nón chịu trọng lực.

Desktop: tab **TRANG PHỤC → Bảng thông số kéo / nén rõ ràng**. Bảng số cũng hiện trong vùng thông tin của tab.

## Đọc đúng từng dòng

| Dòng | Ý nghĩa |
|---|---|
| Dịch tay / lệnh, cm | Dịch chuyển tay yêu cầu và phần được chấp nhận sau giới hạn. Ví dụ tay đi 30 cm nhưng lệnh chỉ nhận 5 cm. Chưa phải độ giãn sợi. |
| Giới hạn lệnh, cm | Hành trình điều khiển một điểm trên áo; với nón hai tay là tổng thay đổi khoảng cách hai điểm, chia hai phía. |
| L0 | Một tay: thước quy ước 30 cm. Hai tay: khoảng cách tham chiếu giữa hai điểm neo trong mô hình. Không thay thế phép đo mẫu vải ngoài đời. |
| ε RMS / đỉnh, % | Biến dạng chiều dài các cạnh lưới so với tham chiếu. RMS tổng hợp và cực đại cục bộ khác nhau; cực đại có thể cao dù RMS thấp. |
| T màng, N/m | Ước lượng lực căng trên một đơn vị bề rộng theo E×h×ε. **N/m không phải N** của một lực kéo tổng. |
| F lệnh ảo điểm 1/2, N | Lực ảo của hai điểm tương tác; không mặc định là tay trái/phải vì thứ tự tùy lần nắm. Touch không đo lực kéo thật. |
| Hồi τ, s | Hằng số hồi; xấp xỉ 95% biến dạng dư mất sau 3τ khi cân bằng đích không đổi. Có tải hoặc tiếp xúc thì không nhất thiết trở về mesh trần. |
| E dọc/ngang, kPa | Độ cứng theo hai hướng quy ước của mô hình, chưa phải canh sợi đã nhận dạng từ rập sinh viên. |
| h, mm; khối lượng mặt, g/m² | Độ dày giả định và khối lượng trên một mét vuông; không phải khối lượng toàn bộ áo. |
| Cảnh báo ngưỡng | Khi cực đại cục bộ vượt miền đàn hồi giả định, các số T chỉ mang tính minh họa, không dùng đánh giá sức bền. |

## Các preset đang dùng

**Toàn bộ thông số dưới đây là tham số mô phỏng minh họa, chưa hiệu chuẩn bằng mẫu vật thật.** Các mức 5/10 cm là yêu cầu tương tác của dự án, không phải quy luật chung của cotton hoặc chất vải nào.

| Preset | Giới hạn lệnh | E dọc/ngang | h | Khối lượng mặt | τ | Khoảng hồi 95% | Ngưỡng ε giả định |
|---|---:|---:|---:|---:|---:|---:|---:|
| Cotton dệt | 5 cm | 180/120 kPa | 0,60 mm | 180 g/m² | 0,8 s | 2,4 s | 22% |
| Vải co giãn | 10 cm | 24/18 kPa | 0,80 mm | 160 g/m² | 1,3 s | 3,9 s | 48% |
| Nỉ định hình | 2,5 cm | 650/500 kPa | 2,00 mm | 380 g/m² | 1,8 s | 5,4 s | 9% |
| Da mềm | 2 cm | 1500/900 kPa | 1,20 mm | 650 g/m² | 1,1 s | 3,3 s | 12% |

Ví dụ: **một đoạn mẫu thực sự dài thêm 5 cm từ L0=30 cm** có ε=5/30=16,7%; dài thêm 10 cm là 33,3%. Di chuyển tay 5 cm trong game không có nghĩa mọi cạnh vải đều giãn 16,7%.

Với Cotton và ε=5%: S=E×h=180.000×0,0006=108 N/m; T≈108×0,05=5,4 N/m. Với dải rộng 5 cm và giả thiết lực phân bố đều, F≈T×0,05=0,27 N. Đây là ví dụ tính đơn vị của màng tuyến tính, **không phải lực đo trên áo trong kính**.

Lệnh tương tác dùng đường riêng `F_cmd=10×clamp(|Δ|/limit,0,1)^1,5 N`, tối đa 10 N ảo. Đường này tạo cảm giác điều khiển, chưa được fit đồng nhất với E/h hoặc dữ liệu kéo thật. Không đem F_cmd so trực tiếp với sức kéo đứt. Trọng lực mặc định có độ lớn khoảng 9,81 m/s².

Đối với tà áo dài, bảng ghi **nâng/xếp tà** riêng: dịch cả tà không đồng nghĩa kéo dài sợi. Giới hạn theo chiều dài, điểm treo và ràng buộc của solver tà; không áp trần 5 cm cho thao tác nâng cả tà.

## Các lỗi hình ảnh đã sửa

- Áo Ivory tay ngắn: dựng thân và tay thành lưới liên tục, có cửa cổ/tay/gấu rõ ràng; mở rộng bao phần hông để không cắt nhau với cạp váy. Áp cho mẫu chính và hai mannequin pose; hai bộ nam tay ngắn cũng được tái dựng.
- Nón vành: dùng cùng đường biên thân–vành và chung đỉnh, thêm mặt dưới và mép trong dày 3 mm. Đường hở sáng ở chân thân không còn do hai mép lệch nhau.
- Tủ: đáy vách đặt đúng mặt trên bệ; bệ chạm sàn, tránh các khối chồng xuyên ở góc chân tủ.

Mesh áo được tạo sẵn theo chữ ký cơ thể và rig; khi thay đồ, game nạp bản phù hợp thay vì dựng lại trường hình học. Thay đổi hình dáng/rig ngoài bộ hỗ trợ có thể cần tạo lại cache. Đây chưa phải retopology/rập may chính xác hoặc mô phỏng FEM vật liệu đầy đủ.

[Báo cáo, ảnh và kết quả kiểm tra](../Verification/SurfacePolish/REPORT.md)
