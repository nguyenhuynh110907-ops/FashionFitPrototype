# Kết quả bản thử chi tiết đầm — 28/10/2026

Đã sửa trực tiếp dự án chính. Mẫu 06 dựng theo ảnh tham chiếu Gucci 864361: cổ gập có chân cổ và độ dày, tay ngắn, nẹp hai lớp, bốn nút kim loại mô phỏng Double G, hai đường may thân trước và đường chỉ nhỏ. Nẹp/chỉ/nút được căn lên bề mặt áo thực tế; kiểm tra góc bên đã loại bỏ hiện tượng chi tiết lơ lửng. Đã đóng khe xẻ giữa Emerald ở cả mặt hiển thị và solver.

## Kiểm tra bản cuối

| Phép kiểm tra | Kết quả |
|---|---|
| Unity 2022.3.62f1, kiểm tra mã/lưới/trọng số/chuyển trang phục | Đạt 37.438 điều kiện |
| Topology sáu cage váy | 1.280 tam giác/mẫu; chỉ có biên eo và gấu, không có khe giữa |
| Mô phỏng chậm mẫu 02 và 06 | 1.080 bước, không ghi nhận điểm nằm trong collider đại diện |
| Căn áo offline | Cache hợp lệ với hai hash lưới nguồn; UV và trọng số xương giữ nguyên |
| Lưới áo mẫu 06 | 7.586 đỉnh áo + 5.264 đỉnh cổ/nẹp/nút/chỉ; cage váy vẫn 672 điểm |
| Body gốc, sáu tư thế đứng/đi của mẫu 02 và 06 | 164.016 mẫu đỉnh và tâm tam giác; **0 mẫu xuyên quá 1 mm** |
| Độ xuyên lấy mẫu lớn nhất | Khoảng **0,212 mm** tại áo mẫu 06; mẫu 02 là 0 mm |
| Da bị ẩn để kiểm tra | Không |

Dữ liệu: [verification.json](verification.json), [source_body_contact.json](source_body_contact.json), [hash nguồn](source_snapshot.json). Bốn phép đo được dùng lại vì hash **toàn bộ file nhị phân chứa cả body, quần áo và topology** trùng khớp tuyệt đối; hai tư thế đi mới của mẫu 06 đã được đo lại. Không dùng số đo của một lưới khác thay cho bản cuối.

Ảnh bản cuối: [trước](Look6_Standing.png), [cận cổ](CollarClose.png), [bên](Side.png), [sau](Back.png). Ảnh tham chiếu và nguồn ở [GucciDressStudy](../../References/GucciDressStudy/source.json).

## Lỗi đã phát hiện và cách xử lý

Kiểm tra collider đơn giản ban đầu bỏ sót vùng nách. Đối chiếu với body thật phát hiện lỗi khoảng 30,7 mm ở bản dựng đầu; căn bề mặt và làm mượt áo đã giảm mạnh lỗi. Khi vung tay, bản tiếp theo vẫn có lỗi đến khoảng 4,3 mm ở tư thế lấy mẫu. Vì áo trên chưa có solver vải động riêng, **bản cuối giữ tay/thân trên ổn định cho riêng mẫu 06**; chân, chuyển động hông và váy vẫn trình diễn chậm. Các trang phục khác tiếp tục dùng chuyển động trước đó. Kết quả cuối trong bảng thuộc chế độ giới hạn này.

Nẹp/chỉ bị nổi xa bề mặt ở góc bên đã được sửa bằng phép chiếu lên lưới áo đã căn. Bề mặt áo được làm mượt và tính normal riêng để giảm hiện tượng sao chép cả gồ ghề nhỏ của da lên áo. Việc căn này chạy offline; game không chạy Python hoặc truy vấn tam giác cơ thể mỗi frame.

## Giới hạn thực tế

Đây là nghiên cứu dựng hình học từ ảnh, **không phải mesh Gucci chính thức hoặc kết quả huấn luyện AI chuyển ảnh thành 3D**. Phần khuất và cấu trúc bên trong được ước lượng; phom và cổ chưa thể coi là bản sao chính xác của sản phẩm thật. Không có lớp lót/rập may hoàn chỉnh hoặc solver tương tác VR cho phần cổ/nút/áo trên.

Các kết quả tiếp xúc là kiểm tra đỉnh/tâm tam giác ở sáu tư thế, với ngưỡng 1 mm. Chưa chứng minh không xuyên tam giác liên tục, chưa kiểm tra mọi góc kéo VR, tự va chạm vải hoặc headset thật. Không dùng kết quả này để khẳng định mọi chuyển động đều không xuyên vải.

Cách mở và các nút kiểm tra: [GUCCI_DETAIL_TRIAL.md](../../Docs/GUCCI_DETAIL_TRIAL.md).
