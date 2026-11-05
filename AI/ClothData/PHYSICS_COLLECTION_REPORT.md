Dữ liệu texture và cơ học vải mới: [nguồn tải, kiểm tra nhãn và cách dùng](../MaterialLab/README.md). Giữ ngân sách dữ liệu dưới 30 GB.

# Dữ liệu vật lý vải — đợt 28/10/2026

Đã tải dữ liệu đo nhỏ để hiệu chỉnh bản thử chất liệu trong Unity. Không tải thêm bộ video nhiều GB; giữ ngân sách thu thập đợt đầu khoảng 30 GB. Bản bổ sung này chiếm khoảng 4 MB gồm nguồn tải, bản giải nén và tài liệu.

## MIT: 30 loại vải, 52 phép đo

Nguồn chính: [Estimating the Material Properties of Fabric from Video — dataset](https://people.csail.mit.edu/klbouman/pw/projects/materialproperties/dataset.html), Bouman, Xiao, Battaglia, Freeman, ICCV 2013. [Bài báo](https://people.csail.mit.edu/klbouman/pw/papers_and_presentations/iccv2013_bouman.pdf).

Đã tải [material_properties.zip](https://people.csail.mit.edu/klbouman/pw/data/materialproperties/material_properties.zip) và bảng hình `materialproptable.png`. Lưu ở `Data/Physics/MIT/`, ngoài `Assets`. File Excel gốc được giữ nguyên, chỉ đọc cached values bằng ZIP/XML; không sửa hoặc ghi đè workbook.

- ZIP: 45.664 byte, SHA-256 `99021ff230ac8f08b87e111ff5d30fc4fc23303c10a1ffa1f1ac05eb934bc01d`.
- Có 52 dòng đo cho 30 ID; giữ ký hiệu mẫu A/B/C và độ tin cậy nguồn 1–4. Không suy diễn A/B là hướng dọc/ngang.
- `measurements_si.json` giữ hàng Excel, giá trị gốc, đơn vị và giá trị SI. Tên vải được đối chiếu từ bảng hình nguồn.
- Đổi khối lượng bề mặt oz/yd² sang kg/m², chiều dày inch sang mm. Kiểm tra chéo từ mật độ/thickness: có dòng nguồn bị làm tròn, sai số tương đối lớn nhất khoảng 2,38%; parser ghi sai số và chặn trên 3%.
- Độ uốn nguồn là **EI của dầm**, lbf·in² → N·m². Không đổi nhãn thành độ cứng uốn tấm N·m vì nguồn không cung cấp bề rộng mẫu cần thiết.

Chạy lại xử lý cục bộ bằng Python: `AI/ClothData/prepare_material_physics.py`. Script tạo JSON SI và catalog `Assets/Resources/Fitting/cloth-materials.json`; không tự tải dữ liệu. Catalog dùng khối lượng đo của ID 3/5/6 và tỷ lệ độ uốn để hiệu chỉnh ba cấu hình. Các trường còn lại được đánh dấu `authored_fields`, không nhận là thông số đo.

## Berkeley: tài liệu 10 chất liệu phi tuyến

Nguồn: [Data-Driven Elastic Models for Cloth: Modeling and Measurement](https://wanghmin.github.io/publication/wang-2011-dde/), Wang, Ramamoorthi, O'Brien, SIGGRAPH 2011. [Trang dữ liệu/tài liệu](https://objf.ai/papers/Wang-DDE-2011-08/), [supplement tham số](https://objf.ai/papers/Wang-DDE-2011-08/material_parameters.pdf).

Đã lưu PDF 2.260.286 byte và `source.json` ở `Data/Physics/Berkeley2011/`. SHA-256 PDF: `f39b2b5270e3588321228d8579bacd844d0b5466be9b43b0c03ce61d1cd47749`.

PDF mô tả 10 chất liệu với hệ số kéo/uốn phi tuyến và theo hướng. Đây mới là **tài liệu tham khảo**, chưa phải 10 profile Unity đã chuẩn hóa. Không chép trực tiếp các hệ số vào scalar XPBD vì mô hình cấu thành và đơn vị sử dụng khác nhau. Không tải/chạy hoặc tích hợp mã ARCSim trong đợt này.

## Phạm vi sử dụng

Nguồn MIT chưa nêu rõ giấy phép thương mại/phân phối lại ngay trên trang dataset. Giữ raw research ngoài `Assets` và ngoài gói game; chưa coi chúng là tài sản CC0. Không dùng giấy phép mã của dự án khác thay giấy phép dataset. Cần xác nhận quyền sử dụng nếu phân phối các file nguồn hoặc thương mại hóa dữ liệu.

Đợt này không train model mới, không xác nhận mô hình đã học chất liệu từ ảnh/video. Ba profile Unity là bản hiệu chỉnh thử nghiệm. [Kết quả runtime và giới hạn](../../Verification/MaterialMotion/REPORT.md).
