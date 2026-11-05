# Kiểm tra chi tiết và đề xuất phát triển — 28/10/2026

## Kết luận

Ưu tiên tiếp theo là **nhập và giữ nguyên tài sản 3D của sinh viên, rồi fitting có kiểm soát**. Cần làm đúng đường đi của mesh/UV/vật liệu trước khi dùng AI sửa thiết kế. Hiện hai checkpoint nhận dạng chạy offline; chưa có mô hình retarget hay phục hồi hình thêu 3D.

Đợt này đã đọc mã runtime, chạy lại QA dữ liệu, so hash 25 file runtime với snapshot kiểm tra nhìn gần, và thực hiện sáu phép thử đầu vào lỗi. Chưa chạy lại Unity/render/headset hoặc đo FPS mới. Báo cáo này là kiểm tra và kế hoạch; không sửa solver, importer hay trọng số trong đợt review.

## Những gì đã kiểm tra và phát hiện

| Mức ưu tiên | Phát hiện | Bằng chứng và ảnh hưởng |
|---|---|---|
| P1 | Nhập OBJ làm mất cấu trúc vật liệu và normal gốc | [RunwayOBJ.cs](../Assets/FashionFit/Runtime/RunwayOBJ.cs) chỉ đọc `v/vt/f`, không đọc `vn`, `mtllib`, `usemtl`; cache đỉnh chỉ theo position/UV. [RunwayWardrobe.cs](../Assets/FashionFit/Runtime/RunwayWardrobe.cs) gán một material mới. Cổ cứng có thể bị làm mềm; hoa văn/phụ liệu của file nguồn không được tái hiện đầy đủ. |
| P1 | Fitting hiện chỉ dành cho váy rời | [GarmentFit.cs](../Assets/FashionFit/Runtime/GarmentFit.cs) yêu cầu eo ở mép trên, chỉ căn eo/hông; tham số `body` chưa dùng để kiểm tra bề mặt body trong hàm này. `ApplyPending` gán mọi đỉnh trọng số 1 vào xương hông. Không phù hợp để mặc áo/đầm có cổ và tay; OBJ nhập chưa có mô phỏng/va chạm của các mẫu procedural. |
| P1 | Công cụ audit chưa đủ để nhận file glTF hợp lệ | Sáu fixture lỗi đã được chạy qua `audit_student_asset.py`: bốn trường hợp không được phát hiện — material index ngoài phạm vi, texture index không tồn tại, UV accessor ngoài phạm vi, buffer khai báo dài hơn dữ liệu thực. Hai đối chứng thiếu UV/thiếu file buffer được phát hiện. Status hiện trả `dependencies_found_requires_render_review`, không phải chứng nhận hợp lệ, nhưng vẫn thiếu cảnh báo cấu trúc cần thiết. |
| P1 | Chưa có căn cứ công bố không xuyên cho trang phục bất kỳ | Solver có swept proxy, điểm ảo ở tâm tam giác và tự tiếp xúc điểm–điểm. Cấu trúc constraint/tự tiếp xúc phụ thuộc `rings/segments`; không phải solver cho topology bất kỳ. Chưa có bằng chứng kiểm tra đầy đủ vertex–triangle/edge–edge liên tục hoặc va chạm giữa áo và váy là hai solver riêng. |
| P2 | Vật liệu hiện chỉ biểu diễn một phần chi tiết | [DressFabric.shader](../Assets/Resources/Runway/DressFabric.shader) có BaseColor/normal, smoothness dạng scalar, metallic cố định 0, alpha cố định 1 và opaque. Chưa đọc roughness/AO theo vùng hoặc độ trong của ren. 90 bộ PBR mới là dữ liệu offline, chưa được importer nối đầy đủ vào game. |
| P2 | Bộ học cổ áo và đánh giá còn mỏng | 200 thiết kế nhưng chỉ 33 có panel cổ: train **28**, validation **4**, test **1**. Test macro-F1 0,218; collar panel F1 0,092. Điểm trên hàng trăm đỉnh của cùng một cổ áo không thay cho nhiều thiết kế độc lập. MLP dùng đặc trưng cục bộ, không có message passing toàn mesh; chưa học fitting/giữ hình thêu. |
| P2 | Mô hình hoa văn có ích cho gắn nhãn, chưa đủ chứng minh giữ chi tiết | Test 101 ảnh: accuracy 84,2%, macro-F1 0,736; lớp chấm và caro chỉ có 6 ảnh test mỗi lớp. Nhãn là cả ảnh, không phải tọa độ từng hoa văn, thông số vật liệu hay hình học thêu. |
| P2 | Chưa có số đo hiệu năng mục tiêu | Physics chạy 60 Hz; VR đang đặt target 72 FPS. Chưa thấy bước nội suy mesh hiển thị giữa hai state physics. `ClothDisplayMesh.Publish` đẩy mesh/tính normals ở từng physics step; khi catch-up có thể publish nhiều lần một frame. Đây là điểm cần đo, chưa kết luận là nguyên nhân lag chính. |

P1: cần giải quyết trước thử nghiệm nhận trang phục sinh viên. P2: cần giải quyết trước mở rộng chất lượng/trình diễn VR.

**Kết quả đạt trong phạm vi hiện có:** `verify_detail_pipeline.py` chạy lại passed; kiểm tra hash, chia tập, mask nhãn chưa biết, UV/face đối chiếu trực tiếp một mẫu mỗi split. Tổng ClothData 28.857.702.172 byte, còn khoảng 1,14 GB trong ngân sách 30 GB. Cả 25 file runtime vẫn khớp hash snapshot CloseView. Kết quả cũ gồm 16 góc nhìn gần và 34.758 mẫu tiếp xúc áo tại ba pose vẫn là bằng chứng của phiên bản mã đó; không thay cho chạy thử mới hoặc tiếp xúc liên tục.

Bằng chứng kiểm tra mới và fixture tái hiện: [review_evidence.json](DetailDevelopmentReview/review_evidence.json). Các fixture `.gltf` trong thư mục đó **cố ý bị lỗi**, không phải trang phục để nhập vào game.

## Hướng triển khai theo thứ tự

### 1. Hoàn thiện kiểm tra và nhập file, giữ chi tiết nguồn

Làm một đường nhập GLB/glTF dành cho runtime; FBX đi qua quy trình import/bake trong Unity Editor. Đánh giá [Unity glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.0/manual/ImportRuntime.html) thay vì tự mở rộng parser OBJ thành importer tổng quát. Tài liệu có API load bất đồng bộ; phải thử riêng phiên bản/package/material generator trên Unity 2022.3 và built-in renderer của dự án trước khi chọn. Không mặc định mọi extension đều được hỗ trợ.

Tích hợp [Khronos glTF Validator](https://github.com/KhronosGroup/glTF-Validator) chạy cục bộ để kiểm tra buffer/accessor/index/schema trước audit bổ sung của FashionFit. Các quy tắc UV, texture, normal, alpha và metallic–roughness tham chiếu [đặc tả glTF 2.0](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html). Ví dụ map metallic–roughness dùng roughness ở G, metallic ở B, dữ liệu linear; không đưa nguyên ảnh packed vào scalar smoothness.

Lập danh sách khả năng hỗ trợ: material slots, normal/tangent, UV set/transform, alpha mask/blend, double-sided, phụ kiện rời, skinning; các extension chưa hỗ trợ phải báo rõ hoặc yêu cầu bake. Giữ file gốc và hash trước chuyển đổi. Không tự chuẩn hóa UV về [0,1] vì có UV tiling hợp lệ; UDIM cần quy trình riêng.

**Đầu ra nghiệm thu:** một viewer trong Unity hiển thị đúng tài sản nguồn khi mannequin đứng yên; chưa fitting. Bộ mẫu đầu gồm khoảng 10 asset có cổ gập, cổ đứng, V-neck, áo tay dài, váy xẻ, đầm xếp ly, ren, thêu/đính, nhiều material và hoa văn có chữ/hướng. Nhập được rồi mới chuyển sang bước 2.

### 2. Fitting bảo toàn phom và hoa văn

Tách ba phần: mesh render giữ chi tiết; mesh mô phỏng nhẹ; binding giữa hai mesh. Dùng mốc body có đơn vị/trục rõ ràng — cổ, vai, nách, eo, hông — và vùng/panel để căn. Giữ lựa chọn xác nhận mốc bằng tay khi không chắc chắn. Không gán toàn bộ đầm vào hông.

Binding cần giữ vị trí tương đối của hình học nhỏ và phụ kiện; cổ/nẹp/nút có thể cần vùng độ cứng hoặc cách gắn riêng. Lưu UV/material theo từng góc tam giác sau ánh xạ tương ứng, kể cả khi importer reindex/split vertex hợp lệ. Chỉ so số đỉnh không đủ phát hiện mất dữ liệu.

Hoa văn có thể vẫn méo dù UV không đổi nếu mesh bị kéo giãn: đo biến dạng bề mặt so với mesh nghỉ tại từng panel, quan sát chữ/sọc và vị trí đường may. Căn trên một body trước, rồi ba kích thước body và các pose tăng dần. Hỗ trợ đứng yên/chuyển động chậm tiếp tục là cấu hình thử nghiệm ban đầu.

**Đầu ra nghiệm thu:** giữ chi tiết của 10 asset trên body tham chiếu; có báo cáo mốc/biến dạng và ảnh trước–sau. Binding/retarget chưa đạt thì không dùng AI tự sửa để che lỗi.

### 3. Kiểm tra va chạm thực và tối ưu VR

Tạo bài offline phát hiện tam giác giao body và tự giao, gồm vertex–face/edge–edge, cộng phép kiểm tra quét giữa frame. Tách hai kết quả: khoảng cách đến proxy runtime và tiếp xúc với body gốc. Luôn đo body đầy đủ; không dùng che da làm bằng chứng hết xuyên.

Xử lý theo mức độ: tư thế ban đầu hợp lệ → body proxy/SDF phù hợp → constraint độ giãn/uốn và vùng neo → va chạm giữa các lớp → xử lý liên tục khi kéo nhanh. Có thể nghiên cứu [IPC và toolkit](https://ipc-sim.github.io/) làm tham chiếu cho kiểm tra/sinh dữ liệu offline; chưa có số đo để đề xuất thay trực tiếp solver VR bằng IPC hoặc hứa tốc độ realtime.

Đo standalone build trên phần cứng đích bằng [Unity Profiler](https://docs.unity3d.com/2022.3/Documentation/Manual/Profiler.html): CPU physics, cập nhật mesh, GPU, cấp phát GC, frame-time p50/p95/p99 và dropped frames. Physics và rendering có thể dùng nhịp khác nhau nhưng cần nội suy hiển thị được kiểm tra cả va chạm. Thử publish một lần mỗi render frame và giữ normals/tangent đúng; chỉ áp dụng sau đo trước–sau. Thêm LOD cho render/collider và giữ nguyên master asset.

**Ngân sách đề xuất, chưa phải số đo:** desktop 60 Hz = 16,67 ms/frame; headset 72 Hz = 13,89 ms, 90 Hz = 11,11 ms. Chốt tần số theo headset thực. Đánh giá CPU/GPU và dropped frames, không lấy FPS Editor hoặc chỉ thời gian solver làm cam kết.

### 4. Huấn luyện có mục tiêu sau khi pipeline giữ tài sản đã đúng

Tiếp tục dùng bộ hoa văn để hỗ trợ tìm kiếm/gắn nhãn. Bài AI hữu ích hơn cho fitting là nhận vùng cổ/vai/nách/đường may, dự đoán correspondence hoặc gợi ý mốc; hiển thị vùng không chắc để người dùng sửa. So baseline hiện tại với mô hình có ngữ cảnh mesh/graph, đánh giá theo từng thiết kế và từng loại cổ. Đổi kiến trúc đơn thuần không bù cho thiếu nhãn.

Mở rộng có chủ đích từ dữ liệu 3D đang có: khoảng 100 thiết kế có cổ riêng, nhiều kiểu cổ và tối thiểu khoảng 20 thiết kế độc lập cho một bài đánh giá cổ đáng tin hơn. Đây là mục tiêu thu thập, không đảm bảo đủ về mặt thống kê. Đánh giá precision/recall/F1/IoU từng vùng, sai số biên theo mm, tỷ lệ cần người sửa và thất bại fitting; luôn ghi số thiết kế hỗ trợ mỗi lớp.

Muốn học phục hồi hình thêu/nếp nhỏ cần **cặp high-detail và low/deformed của cùng bộ đồ**, correspondence/UV, body/pose và map vật liệu đi kèm. Thu một pilot khoảng 30–50 thiết kế có phép ghép đúng và nhãn kiểm duyệt trước khi mở rộng. Không biến 1.002 ảnh thành nhãn thêu 3D giả. Mọi mục tiêu dữ liệu mới phải tính dung lượng trước; ngân sách còn khoảng 1,14 GB, không tự xóa archive cũ hoặc tăng hạn mức.

Tập test hiện đã được sử dụng để báo cáo. Các lần cải tiến tiếp theo chọn bằng train/validation; lập benchmark cuối bằng thiết kế/sinh viên chưa xuất hiện trong quá trình phát triển. Không liên tục chọn mô hình theo cùng một test rồi gọi đó là đánh giá độc lập.

## Ma trận kiểm tra chi tiết trước trình diễn

Các ngưỡng dưới đây là **mục tiêu kỹ thuật ban đầu cần chốt theo loại vải và asset**, không phải kết quả đã đạt.

| Nhóm | Ca thử | Cách đo và điều kiện qua |
|---|---|---|
| Cấu trúc đầu vào | File hợp lệ; thiếu map/MTL; index ngoài phạm vi; buffer thiếu; extension chưa hỗ trợ | Không bỏ lọt sáu fixture hiện có; validator không có error; warning có phân loại; file lỗi bị chặn kèm lý do, không crash app |
| Chi tiết và vật liệu | Cổ cứng, seam UV, nhiều material, chữ/sọc, ren, normal GL/DX, phụ kiện rời | Giữ dependency/source hash; đối chiếu UV theo góc mặt và correspondence, slot map đúng; không mất phụ kiện. Normal/channels/alpha được so bằng render. Không dùng SSIM đơn lẻ giữa hai renderer làm bằng chứng |
| Fitting | Đứng yên; ba kích thước body; vai/cổ/eo; tà lệch và váy xẻ | Sai số neo mục tiêu khoảng 2 mm ở vùng cần cố định; báo độ giãn theo vật liệu và panel. Các ngưỡng này chỉ đo được khi có đơn vị và mốc tham chiếu đáng tin |
| Biến dạng hoa văn | Chữ in, đường sọc qua seam, bông hoa nằm gần cổ/eo | Giữ định danh/UV; đo độ giãn và hướng bề mặt. Giữ vùng collar/nẹp ổn định; designer xác nhận thay đổi phom chấp nhận được |
| Va chạm | Đứng 30 giây; đi chậm 60 giây; quay; 20 lần start/stop; 20 lần kéo/thả; áo chồng váy | Mọi giao cắt tam giác phát hiện được đều là lỗi; ghi độ sâu/tần suất/frame và dung sai phép đo. Kiểm tra cả frame trung gian/swept contacts, với body gốc. Không chuyển kết quả hữu hạn thành bảo đảm mọi chuyển động |
| VR và gián đoạn | Di chuyển/quay đầu; mất tracking; thả grip; mất focus; cú nhảy frame 33/50/100 ms | Không giữ grab treo, không nhảy cổ/tà hoặc camera; input và cloth khôi phục ổn định; xác nhận trên headset và runtime OpenXR thực |
| Hiệu năng | Một người mặc một bộ, đổi 10 bộ, đi gần/xa, phiên dùng 10 phút | Đo frame-time CPU/GPU và dropped frames theo headset. Theo dõi bộ nhớ để tìm tăng dần; mục tiêu không GC allocation từ cloth loop sau warm-up. Không chạy train song song khi lấy benchmark |
| AI | Thiết kế/sinh viên chưa thấy; mỗi loại cổ/hoa văn; texture atlas nhiều họa tiết | Báo per-design/per-class metrics và cỡ mẫu; có cơ chế từ chối/đề nghị sửa tay. Mô hình 3D hiện tại chưa được phép tự biến đổi thiết kế |

## Mốc phát triển nên chọn ngay

**Mốc đầu: viewer giữ đúng chi tiết của 10 file 3D, kèm validator chặn file lỗi.** Sau đó fitting đứng yên trên body tham chiếu, rồi chuyển động chậm và va chạm, cuối cùng mới tối ưu và thực nghiệm headset. AI phát triển song song bằng dữ liệu có nhãn phù hợp; chưa nằm trong đường bắt buộc để giữ hoa văn gốc.
