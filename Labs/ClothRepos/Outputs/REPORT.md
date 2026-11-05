# Kết quả thử repo — 03/11/2026

## Đã chạy được

[GarmentCode](https://github.com/maria-korosteleva/GarmentCode) — 492 sao tại thời điểm kiểm tra, đứng đầu bốn repo đã rà. Đã tạo rập, chia lưới CGAL, may và rủ vải bằng XPBD của đúng Warp fork, chạy CPU trên AMD64. Đã xuất OBJ, GLB, ảnh trước/sau và thống kê giao cắt. Dùng body `mean_all` đi kèm repo, không phải body trò chơi FashionFit.

| Mẫu | Đỉnh | Tam giác | Số frame | Tổng giây | Giao cắt body | Tự giao cắt | Lỗi repo báo |
|---|---:|---:|---:|---:|---:|---:|---|
| tee | 2001 | 3516 | 406 | 99.94 | 0 | 0 | {} |
| skirt | 2540 | 4705 | 406 | 93.79 | 0 | 0 | {} |
| custom | 2039 | 3596 | 406 | 79.25 | 0 | 0 | {} |

Số giao cắt được tính bởi `Cloth.count_body_intersections()` và `Cloth.count_self_intersections()` của repo trên trạng thái cuối. Đây không phải chứng nhận liên tục không xuyên trong chuyển động. Body đứng yên, không thử grip VR, đi bộ hoặc thay body. Thời gian gồm chia lưới, mô phỏng và render, không phải frame time của game.

## Điều chỉnh để chạy trên máy này

- Python riêng 3.12.10, NumPy 1.26.4, SciPy 1.13.1, NiceGUI 1.4.35. Phiên bản đầy đủ trong `RuntimePackages.txt`.
- Biên dịch `warp.dll` CPU từ fork GarmentCode bằng MSVC 2022, giữ nguyên phương trình solver. Lấy riêng `warp-clang.dll` từ bản phát hành NVIDIA đúng v1.0.0-beta.6. Không thay warp.dll bằng thư viện stock.
- Khoảng cách chia lưới 2 cm, tối đa 1000 frame, giới hạn mô phỏng 900 giây, texture 200 DPI. Các mẫu tự dừng khi đạt ngưỡng tĩnh của repo, sau giai đoạn attachment. Vẫn giữ va chạm body, điểm-tam giác và cạnh-cạnh.
- Sửa một lỗi log upstream: `Edge.init_curve()` tham chiếu `self.name` không tồn tại ở cạnh ngắn khi lưới thưa. Chỉ sửa câu in cảnh báo, không đổi hình học.
- `run_garment_trial.py` gọi trực tiếp MetaGarment, BoxMesh và run_sim upstream. Wrapper thêm giao diện Việt, cấu hình CPU và xuất GLB ghép body. GLB hiển thị dùng màu xanh đơn để quan sát phom; OBJ/ảnh gốc giữ màu từng mảnh.
- Mẫu `custom` đổi rộng cổ 0.1, sâu trước 0.3 và sâu sau 0.08 để kiểm tra đường tham số khác mẫu áo gốc. Đây là tỷ lệ thiết kế, không phải cm.

## Repo thứ hai

[Assistive Gym](https://github.com/Healthcare-Robotics/assistive-gym) — 413 sao. Đã tải mã nguồn, kiểm tra môi trường `DressingEnv` và thử phụ thuộc. Chưa chạy được môi trường mặc áo đầy đủ. TensorFlow 1.14.0 không có bản cho Python 3.12; mã còn yêu cầu Keras 2.3.0 và fork Bullet có `loadCloth`, `clothParams`, `getSoftBodyData`. Thử build stock PyBullet 3.2.7 cũng thất bại trong runtime embedded thiếu Python development headers. Không tráo stock Bullet rồi coi là kết quả của Assistive Gym. Log kiểm tra TensorFlow ở `AssistiveCompatibility.log`. Muốn tiếp tục cần môi trường legacy riêng và build đúng fork Bullet.

[HOOD](https://github.com/Dolorousrtur/HOOD) — 206 sao, [ContourCraft](https://github.com/Dolorousrtur/ContourCraft) — 101 sao. Chưa chạy trong đợt này. Ưu tiên repo nhiều sao hơn và có đường CPU trước. Không có kết quả thực nghiệm của hai repo này.

## Kiểm tra giao diện và giới hạn bàn giao

Launcher đã thử ở chế độ `--check`; tự khởi động hoặc dùng lại đúng máy chủ localhost. Trang hub, giao diện gốc, mesh GLB và ảnh trả HTTP 200. Tệp `HttpVerification.json` ghi phản hồi. Đã quan sát ảnh render thật phía trước của áo và váy, cùng kích thước mesh xuất theo mét.

Công cụ Computer Use dừng trước khi tương tác với trang vì không xác định chắc chắn URL của Chrome trên Windows để thực thi chính sách. Vì vậy chưa kiểm tra chuỗi bấm chọn mẫu/chỉnh số/mô phỏng trong Chrome. Chạy CLI, solver và HTTP đã được kiểm tra riêng. Người dùng cần thử trực tiếp giao diện và kiểm tra sau cổ, nách, cạp váy.

Chưa sửa game V092 hoặc đưa solver này vào VR. Repo có nhiều sao vẫn cần kiểm định với body, bài nộp và khối lượng mô phỏng thực tế. Không có bài nộp 3D của sinh viên trong phép thử này.

## Giấy phép và khả năng tái lập

GarmentCode và Assistive Gym giữ LICENSE nguyên bản. Warp fork cũ giữ NVIDIA Source Code License, mục 3.3 giới hạn nghiên cứu/đánh giá. Chỉ chuẩn bị môi trường thử cục bộ, chưa đóng gói fork vào bản thương mại.

Commit và SHA256 gói nguồn ở `SourceReceipt.json`, nguồn compiler ở `CompilerReceipt.json`. Các quyết định tự chọn: dùng CPU vì GPU AMD, chọn áo thun + váy xòe trên body mặc định, lưới 2 cm để thời gian thử phù hợp máy. Chưa sửa các hệ số vật liệu để khớp vải thật vì chưa có số đo thực nghiệm.
