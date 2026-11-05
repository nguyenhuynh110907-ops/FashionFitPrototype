# Nghiên cứu 100 ảnh người mặc trang phục

Cập nhật sau nghiên cứu: đã chạy huấn luyện bộ phân loại FitVision từ các ảnh này. Xem [báo cáo huấn luyện và kết quả thực tế](../../AI/FitVision/TRAINING_REPORT.md). Nội dung dưới đây ghi lại đợt thu thập/phân tích ảnh trước huấn luyện.

Đã thu thập và xem ảnh ngày 27/10/2026. Bộ cuối có **100 ảnh mới**, tách khỏi bốn ảnh tham chiếu cũ trong `FitStudy`. Đây là kết quả quan sát và phân loại thủ công để chuẩn bị sửa phom trang phục; chưa huấn luyện AI, dựng mesh từ ảnh hoặc thay đổi mô phỏng trong Unity ở đợt này.

Nhấp đúp `Open_Reference_Study.bat` ở gốc project, hoặc mở [index.html](index.html) bằng Chrome/Edge để xem ảnh, lọc trang phục và đọc ghi chú. Trang chỉ dùng tệp cục bộ, không có fetch/API hoặc tài nguyên mạng bắt buộc. Đã kiểm tra hiển thị, lọc nhóm và mở ảnh chi tiết qua server localhost; công cụ trình duyệt tự động không cho mở giao thức `file:`, nên chưa xác nhận thao tác mở tệp trực tiếp bằng công cụ đó. Mã `MWxxx` là số ảnh trong bộ cuối; mã `Cxxx` là số ứng viên lúc thu thập. Tìm các mã C dưới đây trong ô tìm của thư viện.

## Dữ liệu thực tế

| Nhóm chính quan sát được | Số ảnh |
| --- | ---: |
| Đầm | 52 |
| Chân váy | 7 |
| Quần | 19 |
| Áo | 4 |
| Áo khoác / choàng | 11 |
| Bộ phối có chi tiết chưa xác định chắc | 4 |
| Jumpsuit | 3 |
| **Tổng** | **100** |

Có **69 ảnh toàn thân và 31 ảnh một phần cơ thể**. Ảnh cắt gấu chỉ dùng nghiên cứu phần thân hiện rõ, không dùng suy đoán chiều dài gấu. Nhóm chính không liệt kê mọi món phụ trong ảnh: ảnh quần có thể đồng thời chứa sơ mi và áo khoác. Có ảnh nhóm và ảnh tạo dáng/ngồi bên cạnh ảnh catwalk. Không phải 100 người mẫu hoặc 100 thiết kế khác nhau; giữ một số góc của cùng trang phục để đối chiếu đường bao.

Đã xét 130 ảnh tải thành công, giữ 100 ảnh; 30 ảnh còn lại bị loại vì vùng trang phục không rõ, chủ thể không phù hợp hoặc góc dư thừa. Việc tìm kiếm gặp giới hạn tải thumbnail ở Wikimedia nên không tải được mọi ứng viên truy vấn. Bộ cuối đã đủ số lượng, không tiếp tục gửi yêu cầu tải.

Kiểm tra tệp: **100/100 ảnh giải mã được**, 100 SHA256 khác nhau và 100 SHA1 ảnh gốc khác nhau, có trường ghi công/nguồn/nhãn giấy phép. Riêng C106 nguồn ghi tác giả không rõ, giữ credit kho lưu trữ; C049 đã đối chiếu wikitext để ghi đúng Pavel Ševela thay cho thông báo bản quyền trong metadata. Dung lượng ảnh là **21.506.125 byte**, khoảng **20,51 MiB**. Khác hash không có nghĩa khác thiết kế; chưa chạy bộ tìm ảnh gần giống tự động. Kết quả máy kiểm tra trong [validation.json](validation.json).

## Những điều rút ra khi xem ảnh

Các ghi nhận dưới đây là diễn giải hình ảnh. Cột áp dụng là đề xuất kỹ thuật cho prototype, không phải thông số được đo từ ảnh.

| Quan sát và ảnh đối chiếu | Quy tắc áp dụng vào model |
| --- | --- |
| C010, C021, C028: đầm ôm theo eo-hông nhưng không dính mọi chỗ lõm của thân | Fit theo vùng ngực, sườn, eo, hông với độ dư khác nhau; không thu nhỏ toàn bộ áo bằng một hệ số scale. Khoảng tiếp xúc collision tách khỏi độ dư thiết kế. |
| C005–C007: cùng váy xanh mở rộng khi được nâng và đổi đường bao ở góc sau | Giữ vùng eo/đường nối; cho tùng có chiều dài và diện tích riêng. Không ghim tất cả điểm tùng vào body. Dưới điểm giữ, trọng lực và constraint quyết định nếp. |
| C038/C042, C059/C061: đầm rủ có nếp dọc và khoảng không với chân | Độ rộng dưới điểm treo cần lớn hơn đường bao body; tether mềm hơn ở gấu. Nếp lớn đến từ hình học và vật liệu, không chỉ texture. |
| C024/C036: chân váy bút chì khác tùng xòe C108/C147 | Chân váy ôm cần khoảng bước hoặc xẻ phù hợp; váy xòe cần diện tích tùng. Không dùng một mẫu hình học rồi chỉ đổi màu. |
| C068/C076, C082, C166/C193: các nếp chéo tụ ở vùng thắt và lớp phủ | Đầm quấn/xếp rủ cần panel, điểm tụ và lớp phủ thật. Sóng sin đều quanh chu vi không tái hiện được kiểu nếp này. |
| C071/C097: chân xuất hiện qua khe xẻ; C077: lớp ngoài khác lớp lót | Tạo khe xẻ và lớp lót riêng. Kiểm tra va chạm cơ thể–từng lớp và lớp–lớp; không coi lộ chân qua khe thiết kế là lỗi xuyên. |
| C004/C017: áo rộng khác quần ôm; C011/C034/C047: áo khoác phủ lớp trong | Fit từng món và phân lớp từ trong ra ngoài. Cổ/vai/nách/tay cần điểm giữ phù hợp thay vì cho toàn bộ thân trên đi theo hông. |
| C032/C039/C049: quần rộng và jumpsuit vẫn có hai ống riêng | Quần cần topology đáy quần và từng ống, vùng đùi/gối độc lập. Mesh dạng ống váy không dùng thay quần. |
| C112/C136: tư thế ngồi/gập chân làm nếp tụ hông–gối | Độ dư và chiều dài phải đủ cho tư thế gập; kiểm tra ngồi và bước, không chỉ đứng thẳng. |
| C110/C153: couture có thể tích cục bộ và kết cấu lớn | Phải dựng cấu trúc/panel đỡ phù hợp. Tăng số điểm physics trên một ống đơn không tự tạo được cấu trúc này. |

Các cặp C005–C007, C024/C036, C038/C042, C064/C094 và C166/C193 hỗ trợ so sánh góc/tư thế. **Không có timestamp đồng bộ hoặc chuỗi frame liên tục**, nên không đo được vận tốc vải, độ trễ, damping hay pha bước từ các cặp này. Muốn hiệu chỉnh motion phải có video/scan 4D được cấp quyền và chuyển động kiểm thử xác định.

## Đối chiếu với prototype hiện tại

Đã đọc `Assets/FashionFit/Runtime/BodyFitProfile.cs`: bản 0.8.2 có mốc eo/hông/ngực, độ dư theo mẫu, chiều dài gấu và mức xòe riêng. Đây là nền tảng phù hợp để kiểm tra phom cơ bản. Tuy nhiên phần `Skirt()` vẫn sinh các vòng ngang từ đường bao body rồi chuyển dần sang ellipse ở gấu, thêm sóng nhỏ theo góc. Cấu trúc đó chưa mô tả được panel quấn, xếp rủ bất đối xứng, ly may, lớp lót hay đáy quần của các ảnh trên.

| Mẫu hiện có | Cần giữ / ưu tiên kiểm tra tiếp dựa trên thư viện |
| --- | --- |
| Atelier Midi | Eo–hông chuyển mềm sang độ xòe; đối chiếu đường bao trước/bên và khoảng chân, không đặt nếp đều trên toàn bộ thân. |
| Emerald Column | Giữ đường thân dài gọn; kiểm tra xẻ mở thật khi bước và khi quay, tránh ép chân vào một ống quá hẹp. |
| Rose Couture | Phân biệt thân trên ôm và tùng có diện tích; hình học đường nối eo, gấu và độ giữ phom cần làm trước các nếp nhỏ. |
| Sapphire Cocktail | Kiểm tra cân bằng eo/gấu và khoảng hở chân ở bước dài, chiều dài hai bên đúng thiết kế. |
| Ivory Blouse Set | Áo có độ dư và đường nách/tay; cạp chân váy riêng, không để gấu áo xuyên cạp hoặc ghim toàn bộ áo xuống hông. |

Thứ tự làm tiếp hợp lý: (1) chọn ảnh trước/bên/sau cùng phom; (2) dựng panel và đường may; (3) fit tư thế nghỉ với độ dư; (4) dựng collision đủ kín theo động tác; (5) kiểm tra bước, quay, ngồi, kéo và thả; (6) tối ưu mesh mô phỏng rồi thêm chi tiết mesh hiển thị. Đây là kế hoạch rút ra từ quan sát, **chưa được triển khai trong đợt thu thập ảnh này**.

Ảnh 2D không cho số đo chính xác của người, pattern phía khuất, độ dày, độ co giãn hay khối lượng vải. Không lấy màu/độ bóng làm bằng chứng chắc chắn về chất liệu. Nghiên cứu ảnh cũng không thể bảo đảm không xuyên vải; điều đó phải được kiểm chứng trên mesh/collider, solver và motion thực tế.

## Nguồn và ghi công

Ảnh được lấy từ các trang tệp của [Wikimedia Commons](https://commons.wikimedia.org/wiki/Category:Fashion_photographs), gồm runway College of DuPage, Crystal Couture, các fashion show khác và ảnh người mặc tạo dáng. Nguồn gốc, tác giả, credit, URL ảnh, giấy phép, thời điểm tải và hash của **từng ảnh** nằm trong [manifest.csv](manifest.csv) và [manifest.json](manifest.json). Nhãn trang phục và ghi chú là phân loại của nghiên cứu này; nhãn truy vấn tìm kiếm không được dùng thay cho nhãn đã xem ảnh.

Giữ nguyên thumbnail tải về, không chỉnh ảnh hoặc xóa watermark. Có 61 ảnh CC BY 2.0, 5 CC BY-SA 3.0, 12 CC BY-SA 4.0, 9 Public domain, 1 CC BY 4.0, 9 CC BY-SA 2.0, 1 CC BY 2.5 và 2 CC0, theo metadata trang nguồn lúc tải. Thư viện hiển thị ghi công và liên kết điều khoản theo từng ảnh. Đặt ảnh ngoài `Assets` để không tự import vào Unity hoặc tăng dung lượng game.

Tệp hỗ trợ: [annotations.txt](annotations.txt) là ghi chú duyệt ảnh; [selection_audit.csv](selection_audit.csv) ghi chọn/loại theo mã ứng viên. `../Collect-ModelWear.ps1` là công cụ thu thập; `../Build-ModelWearStudy.ps1` tạo lại manifest/kiểm tra từ ảnh cuối đã lưu. `candidates.json` giữ metadata gốc của lần tìm kiếm; đường dẫn `Candidates/` trong tệp này là dấu vết thu thập, thư mục tạm đã được dọn. Bộ cuối dùng `Images/`, `data.js`, `index.html`, không cần ảnh ứng viên đã loại.
