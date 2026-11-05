# Học chi tiết trang phục từ tài sản 3D — đợt 28/10/2026

Mục tiêu là nhận **file trang phục 3D thực** của sinh viên, giữ cổ áo, đường may, hình thêu, hoa văn và vật liệu khi fitting lên mannequin. Không lấy ảnh 2D để tự dựng lại thiết kế. Ảnh render PNG/JPG của một bộ đồ 3D vẫn chỉ là ảnh; cần mesh và các texture đi kèm để giữ chi tiết gốc.

Đã bổ sung dữ liệu và **thực sự huấn luyện hai baseline chạy offline**. Chưa có mô hình tạo hình thêu 3D, chưa retarget một bộ đồ sinh viên và chưa thay solver/runtime Unity bằng checkpoint này.

## Dữ liệu đã có trên máy

| Phần | Số lượng kiểm tra thực tế | Dùng để học gì |
|---|---:|---|
| GarmentCodeData v2 — mở rộng từ các archive 3D đã tải trước | 200 thiết kế riêng; 2.978.444 đỉnh | Vùng cổ áo, điểm gắn cổ/nách, đường ráp |
| Chia tập 3D theo thiết kế và split upstream | 144 train / 28 validation / 28 test | Giữ các biến thể cùng thiết kế ngoài hai tập khác nhau |
| Mẫu có panel cổ áo riêng | 33 | Phân biệt **cổ áo** với đường/điểm gắn quanh cổ |
| Mẫu có điểm gắn cổ và nách dương tính | 178 | Attachment từ nhãn nguồn, không tự đoán vị trí |
| ambientCG Fabric 1K-JPG | 90 asset / 599 ảnh map / 743.929.125 byte archive | Màu, normal, roughness, displacement và các map nguồn khác |
| AI4C textile pattern | 1.245 ảnh gốc trong hai archive, 30.569.733 byte | Sáu lớp hoa văn in/dệt trên ảnh |
| AI4C có nguồn và quyền đủ điều kiện theo metadata từng ảnh | 1.003 ảnh; 242 giữ riêng | Chỉ ảnh có nguồn/landing page và CC0, CC BY 4.0 hoặc Public Domain Mark |
| AI4C sau loại một bản trùng byte | 1.002 ảnh: 800 train / 101 validation / 101 test | Sọc, chấm, caro, plaid, hình học, hoa/lá |

200 mẫu 3D là **subset mới chuẩn bị từ bộ đã tải**, không phải 200 file 3D mới tải và không phải gói 200 ảnh trong cuộc chat khác. Không tải lại archive 3D nhiều GB. Dữ liệu bổ sung nằm ngoài `Assets`, không làm Unity import hàng nghìn file huấn luyện.

Ảnh AI4C được gom nhóm theo URL hiện vật, SHA-256 và perceptual hash trước khi chia tập; các bản gần trùng ở cùng nhóm. Đây là biện pháp chống rò rỉ dữ liệu tự động, chưa phải kiểm duyệt danh tính từng thiết kế bằng tay.

## Nguồn và quyền sử dụng

- **GarmentCodeData v2:** [ETH Zurich, DOI và CC BY 4.0](https://doi.org/10.3929/ethz-b-000690432), [trang dự án](https://igl.ethz.ch/projects/GarmentCodeData/). Bản repack dùng revision `0e900868c0f7728b0522a13f917203082e8395d4`; hash từng thành phần được kiểm tra trước chuyển đổi. Phải giữ attribution của tác giả. Bộ này có cấu tạo/rập/mesh, không có target thêu hoặc texture gốc trong repack hiện tại.
- **ambientCG:** [giấy phép CC0](https://docs.ambientcg.com/license/), nguồn từng asset và bản chụp trang giấy phép nằm trong manifest. Giữ archive gốc, SHA-256 cục bộ, CRC của từng map và kích thước ảnh. SHA cục bộ không được gọi là chữ ký/hash mật mã do tác giả công bố. 1K là bản **dữ liệu tham khảo** đã chọn theo dung lượng; không ép file sinh viên xuống 1K.
- **AI4C:** [bản phát hành Zenodo v1, Henk Vanstappen/Datable](https://zenodo.org/records/11409357). Đã đối chiếu MD5 công bố của hai archive và JSON. Metadata record ghi MIT, nhưng **quyền của từng ảnh khác nhau**; loader chỉ chọn các ảnh đủ điều kiện theo trường rights/source trong bản phát hành. Chưa kiểm tra lại từng trang bảo tàng. 233 ảnh có quyền không rõ, 2 ảnh NC-ND và 7 ảnh còn thiếu nguồn/landing page không được đưa vào train.
- ClothesNet có texture/mesh nhưng yêu cầu đăng ký và chỉ cho nghiên cứu phi thương mại; GarmageSet có điều kiện truy cập và CC BY-NC-ND. Chưa nhập hai nguồn này vào tập train mặc định. Không lấy giấy phép code làm giấy phép cho asset.

Danh sách nguồn nằm ở `AI/ClothData/Data/Raw/DetailSources/`: `materials_manifest.json`, `pattern_manifest.json`, catalogue, license snapshot và receipt. Các archive không được giải nén nhân đôi.

## Kết quả huấn luyện thực tế

**Nhận vùng cấu tạo 3D:** MLP dùng tọa độ/normal và thống kê láng giềng từ mesh đã đăng ký về hệ body; 7.268 tham số, 30 epoch, 4.320 optimizer steps trên CPU. Nhãn collar panel suy ra từ tên/label panel nguồn; neck attachment lấy từ danh sách đỉnh nguồn. Hai nhãn này tách biệt. Nhãn không có nguồn được mask, không đổi thành âm tính. Không đưa bit đường may/cổ áo hoặc vai trò panel vào input.

- Chọn epoch 24 bằng validation macro-F1 **0,254**; sau đó mới đánh giá test một lần.
- Test macro-F1 **0,218**; riêng collar panel F1 **0,092**, neck attachment **0,346**. Recall cao nhưng precision thấp, còn nhiều điểm dương tính sai. **Không đủ chất lượng tự sửa hoặc căn cổ áo sinh viên.**
- Không có nhãn thêu, nút, khóa, kích thước chỉ, chuyển động hoặc cặp high/low mesh để học phục hồi chi tiết. Đã loại head eo không có mẫu dương tính khỏi bài học này.

**Nhận sáu nhóm hoa văn:** encoder DINOv2-small cục bộ được đóng băng; chỉ train linear head 2.310 tham số trong 200 epoch. Không tải thêm backbone, không gửi ảnh ra dịch vụ ngoài.

- Validation: accuracy **84,2%**, macro-F1 **0,727**; chọn epoch 29.
- Test 101 ảnh: accuracy **84,2%**, macro-F1 **0,736**. Baseline luôn đoán hoa/lá đạt **46,5%** accuracy.
- Test F1: hoa/lá **0,946**, sọc **0,923**, plaid **0,762**, hình học **0,667**, caro **0,615**, chấm **0,500**. Các lớp nhỏ chỉ có 6 ảnh test, nên chưa suy rộng cho thiết kế sinh viên.
- Đây là **phân loại cả ảnh**, không định vị từng bông hoa, không sinh sợi/thêu 3D và không tự chuyển hoa văn sang UV khác. Vẫn giữ texture gốc để hiển thị.

Checkpoint và toàn bộ lịch sử/metric/confusion matrix nằm trong [Artifacts/DetailRegions/training_report.json](Artifacts/DetailRegions/training_report.json) và [Artifacts/PatternDetails/training_report.json](Artifacts/PatternDetails/training_report.json). Không dùng test để cập nhật trọng số hoặc chọn epoch.

## Nhận file của sinh viên và giữ chi tiết

Yêu cầu bộ bàn giao gồm **FBX + texture** hoặc **GLB/glTF**; nếu OBJ thì phải có **OBJ + MTL + toàn bộ texture**. Giữ bản thiết kế gốc CLO/Marvelous/Blender để xuất lại khi thiếu phụ kiện. FBX được CLO mô tả là chứa mesh, material, joint và animation: [tài liệu CLO](https://support.clo3d.com/hc/en-us/articles/115002000227-Compatible-File-Format). Style3D hướng dẫn xuất OBJ kèm unified UV, bake texture và normal: [tài liệu Style3D](https://help.style3d.com/studio/en/4152/f7e8/8736).

1. Ghi đơn vị, trục, body tham chiếu và mốc eo/cổ/vai. Căn theo body; không kéo đồng loạt mọi đỉnh về tâm làm méo cổ, nếp xếp hoặc tà bất đối xứng.
2. Giữ mesh render, UV seam, material slot, normal/tangent, phụ kiện và file texture gốc. Hoa văn thường nằm trên BaseColor/normal; cổ áo/nút có thể là hình học riêng. Shader vật liệu phức tạp cần bake các map tương thích engine và đối chiếu ảnh render.
3. Chạy kiểm tra phụ thuộc trước nhập. Công cụ mới đọc OBJ/MTL và glTF/GLB, ghi hash/resolution, phát hiện thiếu map, thiếu UV, nhiều UV trên cùng điểm và nhiều material; **không sửa file nguồn**. Công cụ chưa kiểm tra đầy đủ accessor, extension, texcoord transform hay sự tương đương render glTF.
4. Tách mesh mô phỏng nhẹ khỏi mesh render giữ chi tiết. Retarget chỉ thay vị trí bằng binding có kiểm soát; giữ UV/material. Kiểm tra trước–sau bằng render cận cảnh, độ méo UV, độ giãn cạnh và tiếp xúc. Đây là bước triển khai tiếp theo, **chưa có trong runtime nhập OBJ hiện tại**.
5. Chỉ train phục hồi chi tiết khi có cặp **cùng bộ đồ**: mesh/vật liệu gốc chất lượng cao và phiên bản biến dạng/giảm chi tiết, UV hoặc correspondence, body, pose và đơn vị chính xác. Chia tập theo thiết kế/sinh viên, không chia các render của cùng bộ đồ sang test. Quyền trưng bày bài sinh viên và quyền dùng để huấn luyện được ghi riêng trong [template](student_submission.template.json).

Đường `RunwayOBJ`/`RunwayWardrobe` hiện vẫn là preview hình học một vật liệu chung, bỏ qua MTL/source normals và chưa fitting cổ/tay. Tăng số ảnh train không tự sửa hạn chế này. **Chưa công bố game giữ đúng chi tiết file sinh viên** cho tới khi có pipeline nhập và retarget được đối chiếu trên asset thật.

## Chạy lại

Đã chạy huấn luyện và QA ở đợt này; không cần nhấp train lại để mở Unity. Tại thư mục project:

```powershell
# Tải lại/tiếp tục các nguồn nhỏ, giữ tệp đã kiểm tra:
.\Collect_Garment_Detail_Data.bat
# Chuẩn bị 200 graph, train offline, kiểm tra:
.\Train_Garment_Details.bat
# Chỉ kiểm tra nguồn và kết quả hiện có:
.\.ml-runtime\python\python.exe AI\GarmentDetail\verify_detail_pipeline.py
# Kiểm tra một file thực, không thay đổi nguồn:
.\.ml-runtime\python\python.exe AI\GarmentDetail\audit_student_asset.py "D:\BaiSinhVien\dress.glb"
# Thử nhận nhóm hoa văn từ ảnh texture, không vẽ lại ảnh:
.\.ml-runtime\python\python.exe AI\GarmentDetail\predict_pattern_details.py "D:\BaiSinhVien\textures\BaseColor.png"
```

QA tại [detail_pipeline_verification.json](detail_pipeline_verification.json) ghi tổng dung lượng hiện tại, hash, chia tập, kiểm tra UV/face chính xác trực tiếp từ nguồn trên một mẫu mỗi split, và hồi quy phát hiện thiếu texture. Đây không phải kiểm chứng không xuyên vải hoặc chất lượng hình thêu. Toàn bộ ClothData vẫn dưới ngân sách **30.000.000.000 byte**. Không xóa dữ liệu cũ để tạo chỗ.
