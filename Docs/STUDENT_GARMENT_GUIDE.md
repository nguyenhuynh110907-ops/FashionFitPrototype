**Nhận trang phục 3D của sinh viên theo body tham chiếu**

Mở source FashionFitPrototype bằng Unity 2022.3.62f1, dừng Play rồi chọn Fashion Fit → 3D to 3D → Student Submission. Tên đích tương ứng như bảng dưới.

| Mã body | Nhân vật |
|---|---|
| fitting-female | Mannequin nữ chính dùng thử đầm |
| pose-female | Mannequin nữ ở khu POSE STUDIO |
| pose-male | Mannequin nam ở khu POSE STUDIO |

1. Chọn body và bấm “Xuất body và tạo mẫu nộp trong Scene”. BodyReference.obj cùng submission.json được xuất vào Samples/StudentGarments/<mã body>. Gửi đúng cặp tệp này cho sinh viên. Body là lưới tham chiếu trung tính, đơn vị mét, Y lên và Z trước. Các nguồn của body MakeHuman hiện có được ghi trong Tools/SourceAssets và Verification/PoseStudio/SOURCES.json.
2. Sinh viên dựng trang phục trên body tham chiếu trong phần mềm 3D. Giữ hình dáng và tam giác của body. Đặt body và trang phục cùng hệ tọa độ. Khi xuất FBX, GLB hoặc OBJ, giữ body nguồn cùng các mảnh trang phục và gửi kèm vật liệu, ảnh. OBJ cần MTL cùng ảnh được tham chiếu. UV seam hoặc thứ tự đỉnh body có thể đổi nếu hình học và mặt có hướng vẫn khớp.
3. Import dữ liệu vào Unity. Với FBX/OBJ, chọn asset rồi dùng Fashion Fit → 3D to 3D → Prepare Selected FBX or OBJ. GLB dùng glTFast đã có trong project. Trong Scene, đặt một Frame chứa renderer SourceBody và nhánh Garment riêng. Nhánh Garment chứa toàn bộ các mảnh cần nộp, cổ, nẹp, nút và phụ kiện.
4. Chọn Frame và submission.json trong cửa sổ. Mặc định sourceBodyPath là SourceBody, garmentPath là Garment. Có thể sửa đường dẫn con trong JSON theo hierarchy của file. unitMeters áp dụng lên tọa độ sau import Unity, rotationEuler và offsetMeters khai báo phép căn vào frame tham chiếu. Vị trí đặt Frame trong Scene được loại khỏi phép chuyển.
5. Bấm “Kiểm tra và tạo gói 3D”. Bản xem xuất hiện cạnh Frame. Gói cùng submission-report.json được lưu trong Assets/Resources/TransferredGarments/<tên bài>. STATIC_PREVIEW_READY nghĩa là sẵn sàng xem tĩnh. NEEDS_REVIEW giữ bản để xem lại, đồng thời chặn mặc thử qua cổng runtime. Dữ liệu đầu vào sai được báo bằng mã lỗi.

Đối với fitting-female, sau khi tạo gói đạt cổng tĩnh, bắt đầu Play rồi vào TRANG PHỤC → TRANG PHỤC 3D · GIỮ CHI TIẾT GỐC để chọn gói. Gói đích pose-female/pose-male hiện được kiểm tra và xem trong Scene. Chức năng mặc gói mới trên mannequin pose trong EXE cùng kiểm định chuyển động là bước tiếp theo.

Hàm dùng chung cho worker nhập bài là StudentGarmentPipeline.Process(frame, spec). Hàm kiểm tra phiên bản body, hình học tham chiếu, renderer, material/shader, số đỉnh, cấu trúc UV/submesh và tiếp xúc. Nhánh canonical giữ vị trí các đỉnh trang phục sau phép đổi tọa độ khai báo. Save tạo gói có bản sao material và nguồn ảnh 2D, giữ thiết lập import, rồi ghi báo cáo. CreatePreview tạo bản xem tĩnh trong Scene.

Điểm vào batch là FashionFit.Editor.StudentGarmentWindow.RunBatch. Dùng cùng Unity version và truyền -fashionSubmissionSpec với đường dẫn JSON tuyệt đối, -fashionSubmissionPrefab với đường dẫn asset đã import bắt đầu Assets/, và -fashionSubmissionReport với đường dẫn JSON đầu ra tuyệt đối. Mã thoát 0 cho gói sẵn sàng xem tĩnh, 2 cho gói cần duyệt lại và 1 cho dữ liệu bị từ chối. FBX/OBJ được chuẩn bị importer trước khi nạp prefab.

Mặc định sourceMode là canonical. Nếu body đã được đăng ký về cùng topology/thứ tự đỉnh của đích, có thể dùng paired-body. Nhánh này tái sử dụng GarmentSurfaceTransfer, yêu cầu cùng pose trung tính và cặp tương ứng hợp lệ. Body khác chưa đăng ký sẽ cần bước đăng ký trước khi chuyển.

Ngưỡng body hiện là 0,1 mm. Ngưỡng lấy mẫu xuyên tĩnh là 1 mm, được đo ở đỉnh, tâm tam giác và trung điểm cạnh. Mình đặt ngưỡng duyệt ban đầu cho độ thay đổi chiều dài cạnh ở 5%. Đây là chính sách duyệt hình học, cần hiệu chỉnh theo tiêu chí bài nộp. Các tính chất vật liệu cần số đo và bước mô phỏng riêng.

Giới hạn hiện có gồm tổng 300.000 đỉnh trang phục, binding cách body tối đa 50 cm, mesh đọc được, không blend shape chưa bake và texture 2D có nguồn sao chép được. Bản nhập GLB hiện chấp nhận các extension đã kiểm chứng trong GarmentImportPreparation. Các tà xa cơ thể, vật liệu phức tạp và body khác cần được đưa vào tập kiểm thử thực tế.

Bộ full nay có 20 bước và thêm cổng nhận bài cùng kiểm tra tiếp xúc độc lập. Chạy Verify_FashionFit.bat --suite full sau thay đổi liên quan. [Xem kết quả và ảnh](../Verification/StudentSubmission/REPORT.md).
