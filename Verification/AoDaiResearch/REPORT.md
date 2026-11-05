# Áo dài: kết quả kiểm tra và thu thập dữ liệu

Ngày 30/10/2026, giờ New York. Đã hoàn thành bước tìm nguồn, tải tệp và kiểm tra dữ liệu cho **một bộ áo dài nữ**. Chưa dựng áo dài mới trong Unity, chưa train model mới và chưa chạy kiểm thử xuyên vải cho bộ này.

## Dữ liệu thực sự có trên máy

| Phần dữ liệu | Kết quả | Dùng cho việc gì |
|---|---:|---|
| Ảnh gốc mới từ Wikimedia Commons | 6 ảnh, 46.51 MB | Phom mặc, cổ, nếp rủ, viền và bố trí trang trí |
| PDF đã tải | 4 tệp, tổng 80 trang, 57.02 MB | Đọc cấu trúc, trình tự may và các biến thể thiết kế |
| Mẫu hỗ trợ từ GarmentCodeData đã có | 6 thiết kế train, 80 mảnh, 240 cặp đường ráp | Kiểm tra dữ liệu cổ/tay/thân áo/quần có seam graph; không phải 6 bộ áo dài |
| Mesh áo dài mới đã tải | 0 | Có một ứng viên, chưa nhận được mesh |
| Rập áo dài số hóa có tọa độ và seam graph | 0 | Sơ đồ PDF chưa được biến thành rập dùng cho solver |
| Chuỗi chuyển động áo dài đã hiệu chuẩn | 0 | Ảnh đứng không thay thế được chuỗi body/garment động |

Gói mới kể cả metadata và ảnh thu nhỏ: **104,816,900 byte (~104.82 MB)**. Tổng `AI/ClothData/Data`: **29,860,149,386 byte**, còn **139,850,614 byte** trước mức 30 GB đã chọn. Không tải lại các mesh lớn đã có.

[Mở trang duyệt ảnh/tài liệu](../../AI/ClothData/Data/AoDaiResearch/BROWSE.html) · [Manifest](../../AI/ClothData/Data/AoDaiResearch/manifest.json) · [Biên nhận](../../AI/AoDaiResearch/collection_receipt.json).

## Nguồn và mức tin cậy

**Ảnh:** 5 ảnh bảo tàng do Daderot chụp, giấy phép CC0; 1 ảnh `Red Ao dai` của Khương Việt Hà được phát hành public domain. Đã lưu URL, tác giả, giấy phép, kích thước gốc và hash từng ảnh. Ảnh bộ sưu tập có hiện vật trùng ảnh riêng: không đếm thành trang phục độc lập, không chia ngẫu nhiên các ảnh đó sang cả train và test. Tệp [photo_review.json](../../AI/ClothData/Data/AoDaiResearch/photo_review.json) ghi nhóm trùng và quan sát thủ công. [Trang gốc ảnh bảo tàng](https://commons.wikimedia.org/wiki/File:Ao_dai,_Viet,_Hanoi,_2010_-_Vietnamese_Women%27s_Museum_-_Hanoi,_Vietnam_-_DSC04129.JPG), [ảnh người mặc áo đỏ](https://commons.wikimedia.org/wiki/File:Red_Ao_dai.JPG).

**Tài liệu may chính:** [MĐ31 - Thiết kế và may áo dài, Trường Cao đẳng Cơ điện Hà Nội](https://hcem.edu.vn/wp-content/uploads/2025/10/MD31-May-ao-dai.pdf). Đã nhìn trực tiếp trang 6-11 và 16 sau khi render bằng Poppler. Sơ đồ cho thấy thân nối liền tà, tay raglan, chiết tạo dáng, cổ trong/ngoài và lớp dựng; nẹp, vạt con là phần cục bộ. Tuy nhiên số đo eo/mông trang 6 không khớp các phép tính mẫu trang 7. Vì vậy **không dùng các số đó làm nhãn huấn luyện hoặc tự động suy kích thước rập**. Phải đo mannequin và kiểm tra lại rập.

**Ba PDF còn lại:** bảng vải Folkwear 139 (1 trang; không phải rập trả phí), catalogue Across Ao Dai do Đại học Bologna lưu trữ (32 trang; thiết kế diễn giải đương đại), catalogue triển lãm Valverde (10 trang; tham khảo lịch sử/chi tiết). Chưa xác minh giấy phép train mở cho cả 4 PDF: giữ để đọc, không đưa vào loader huấn luyện hoặc repo public. [Folkwear](https://www.folkwear.com/products/139-vietnamese-ao-dai), [Bologna](https://zonemoda.unibo.it/wp-content/uploads/sites/5/2019/05/across-ao-dai-catalogo-interno-21x21.pdf), [catalogue triển lãm](https://aodaifestival.com/wp-content/uploads/2014/05/Valverde.AoDai_.Catalogue.ADF_.pdf).

**Dữ liệu 3D hỗ trợ:** giữ nguyên ID, design group, official train split, số đo và hash nguồn của [GarmentCodeData](https://igl.ethz.ch/projects/GarmentCodeData/). Chỉ trích rập, SVG, YAML và vertex labels cho 6 thiết kế; rest/draped mesh vẫn nằm trong Parquet gốc. Đây là dữ liệu tổng quát có giấy phép CC-BY-4.0 theo hồ sơ dataset; không đổi nhãn shirt/pants thành áo dài. [Chỉ mục 6 mẫu](../../AI/ClothData/Data/AoDaiResearch/support_patterns/index.json).

**Nguồn chưa sử dụng:** mẫu 3D `Marvelous to substance test 2 Aodai` của ghostnoface có metadata CC-BY-4.0, 2,180 đỉnh và 4,124 mặt; endpoint tải trả HTTP 401 nên chưa kiểm tra UV/topology/rập. [Ứng viên Sketchfab](https://sketchfab.com/3d-models/marvelous-to-substance-test-2-aodai-88015bd2f553451b8a6e4fb694b1ee14). [VietFashion](https://hng0303.github.io/VietFashion/) công bố 21K ảnh tổng hợp phục vụ retrieval, không dùng làm ground truth vật lý. Một mẫu trên [Fab](https://www.fab.com/listings/0a8b3dfa-b837-417f-ac3b-3b7120dfca12) ghi không cho phép AI nên đã loại khỏi thu thập train. Luận văn lụa do IUH lưu trữ không tải được (TLS/HTTP 403); không tính vào kết quả.

## Bộ áo dài đề xuất và cách mô phỏng

Đề xuất riêng cho bản thử: áo dài nữ màu xanh cổ vịt, tay raglan dài, cổ đứng, quần màu ngà; trước mắt ưu tiên nhìn rõ phom và độ rủ. Đây là lựa chọn thiết kế, không phải bộ đồ đã được phục dựng từ một ảnh.

- Thân trước và thân sau kéo liền xuống tà; hai bên hông mở ở mốc được thiết kế. Không tạo đường nối eo giả hoặc khoét giữa tà trước.
- Tay, cổ, nẹp và phần chồng ở khóa/cài có nhóm vật liệu, đường ráp và hướng canh sợi riêng. Lớp dựng cổ được liên kết với cổ, không coi như tấm vải bay tự do.
- Quần có hai ống thật. Cổ và phần cài chỉ có lớp cục bộ; lớp lót thân là tùy chọn, không tự nhân đôi toàn bộ áo.
- Va chạm phải xử lý cơ thể-vải, tà-quần, tà trước-tà sau, vùng nách và vật thể trong phòng. Chừa độ dày vật liệu và loại trừ hàng xóm đã may để tránh tự đẩy sai.
- Tách lưới mô phỏng với bề mặt hiển thị chi tiết. Texture, đường chỉ và micro-normal bổ sung cảm giác vải; tăng số điểm đơn thuần không tạo ra độ rủ đúng.

[aodai_blueprint.json](../../AI/AoDaiResearch/aodai_blueprint.json) ghi rõ semantic parts, seam plan, contact pairs và dữ liệu cần đo. Tọa độ rập, edge IDs, độ dày, khối lượng trên diện tích, kéo dọc/ngang, uốn, ma sát và độ bền vẫn để `null`: **không suy cơ học hay sức bền từ ảnh/texture**.

Rà soát code hiện tại cho thấy `FittingClothSolver.cs` còn dùng topology segments/rings; self-contact hoạt động trong một solver. `RunwayWardrobe.StepCloth` bước váy/áo riêng. Vì thế thêm preset dạng ống không đủ cho áo dài may nhiều mảnh và quần. Cần topology panel/seam và bộ tiếp xúc giữa các lớp trước khi đưa bộ này vào game. Vị trí và hash code đã kiểm tra được lưu trong [source_review.json](../../AI/AoDaiResearch/source_review.json).

## Kiểm tra đã thực hiện và bước tiếp theo

Kiểm tra offline **PASS**: hash 10 tệp gốc; SHA1/kích thước 6 ảnh; đọc số trang 4 PDF; 30 thành phần đã trích; 48 thành phần trong Parquet; 480 đầu mút của 240 cặp đường ráp; 6 design group train độc lập; 19 liên kết/tệp ảnh cục bộ. Không có tệp tải dở. Hai file runtime được rà soát giữ nguyên hash. [Kết quả máy kiểm tra](verification.json).

Thứ tự triển khai phù hợp: đo mannequin → dựng rập riêng có seam/dart/grain → may và settle tĩnh → kiểm tra tiếp xúc tam giác/cơ thể và giữa các lớp → bước chậm/quay/dừng → grip một và hai tay → tăng tốc khi QA đạt. Sau đó mới sinh cặp pattern/body/material/mesh động đủ tốt để train. Cần kiểm tra góc trước, bên, sau và đo frame time trên PCVR; chưa có kết quả FPS hay chứng nhận chống xuyên cho áo dài mới.
