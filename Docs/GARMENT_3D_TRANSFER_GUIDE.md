# Chuyển trang phục 3D sang mannequin

Bản triển khai 29/10/2026 nằm trong dự án `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`. Đây là mô hình chuyển hình học có kiểm tra, chưa phải mạng AI đã huấn luyện trên trang phục của sinh viên.

## Xem bản thử đã đưa vào game

1. Chờ Unity cập nhật package và biên dịch. Nếu đang Play, dừng rồi Play lại.
2. Vào **TRANG PHỤC → Mặc thử 3D · 3D transfer · bản nghiên cứu có cổ và nút**.
3. Quan sát cổ, nẹp, bốn nút, đường chỉ, tay áo và chân váy. Có thể đi vòng quanh bằng desktop hoặc vào VR sau khi chọn đồ.

Mẫu này lấy từ **mesh 3D nghiên cứu có sẵn trong dự án**, không phải asset Gucci chính hãng hay bộ đồ mới của sinh viên. Nó kiểm tra rằng phép chuyển giữ các phần thiết kế, không biến đồ thành một tấm vải chung. Đồ chuyển được giữ đứng yên; các nút bước đi, tua motion và restart đều bị chặn cho tới khi có mô phỏng và kiểm định chuyển động. Các mẫu procedural cũ vẫn có chế độ trình diễn.

## FBX, GLB và OBJ

| Định dạng | Đường nhập | Cần giữ khi gửi |
|---|---|---|
| FBX | Model Importer của Unity | File FBX, vật liệu và texture ngoài; đơn vị và tư thế nghỉ |
| GLB | Unity glTFast **6.12.1**, đã thêm vào Packages | GLB gốc; texture nhúng hoặc phụ thuộc ngoài nếu có |
| OBJ | Model Importer của Unity | OBJ + MTL + toàn bộ ảnh texture, đúng đường dẫn tương đối |

Ba fixture tự tạo đã được dùng kiểm tra bộ nhập. Điều này không chứng nhận mọi file thuộc ba định dạng đều giữ toàn bộ tính năng. Shader và định nghĩa vật liệu giữa phần mềm có thể khác nhau. Luồng chuyển giữ dữ liệu **sau khi Unity nhập**, cần đối chiếu render nguồn để xác nhận màu, normal, roughness và các lớp trong suốt.

GLB lõi dùng texture PNG/JPEG được kiểm tra trong fixture. Cổng nguồn chỉ chấp nhận các extension đã cho phép: `KHR_texture_transform`, `KHR_materials_unlit`, `KHR_mesh_quantization`. Những extension khác cần kiểm chứng thêm; Draco/KTX2, cloth/morph, transmission, clearcoat hoặc biến thể vật liệu chưa được coi là tương đương thiết kế nguồn. File có blend shape cần bake rõ ràng thành mesh trung tính trước, không bị âm thầm bỏ shape.

## Quy trình nên gửi cho sinh viên

1. Khi dừng Play, mở **Fashion Fit → 3D to 3D → Create Body and Garment Template**. Unity tạo một instance từ prefab nguồn có mesh/vật liệu được lưu bền vững; Undo có thể bỏ instance này.
2. Body tham chiếu để dựng đồ ở `Samples/3DTransfer/FittingBodyReference.obj`: mét, Y lên, Z trước trong frame Unity. Đây là body tĩnh, không phải OBJ có xương. Khi nhập sang phần mềm khác, kiểm tra hướng và đơn vị bằng kích thước thật.
3. Cho sinh viên dựng trang phục vừa body mẫu, giữ cổ, tay, nút, đường may, lót và các vật liệu thành dữ liệu thật. Gửi trang phục riêng bằng FBX/GLB/OBJ và ảnh texture đầy đủ. Không cần gửi ảnh 2D để tái tạo lại bộ đồ.
4. Trong Unity, giữ **SourceBody** chuẩn từ prefab; thay nhánh **Garment** bằng đồ đã nhập. Đặt đúng frame với body; kiểm tra trước/sau, vai, cổ, eo, hông. Không tự dùng tâm hộp bao của váy bất đối xứng làm tâm cơ thể.
5. Với FBX/OBJ, chọn asset trong Project rồi chạy **Fashion Fit → 3D to 3D → Prepare Selected FBX or OBJ**. Lệnh bật Read/Write, tắt mesh compression, weld và sắp lại đỉnh, nhập normal nguồn; các thiết lập khác giữ theo file. Lệnh này sửa `.meta` của asset đã chọn và nhập lại. Giữ một bản source ngoài Assets để đối chiếu.
6. Mở **Fashion Fit → 3D to 3D → Transfer Garment**. Chọn Frame nguồn, Renderer cơ thể nguồn và nhánh trang phục; kiểm tra mét/đơn vị, góc quay và dịch tâm rồi xác nhận tư thế nghỉ.
7. Bấm **Tạo bản chuyển và báo cáo**. Kết quả tạo thư mục mới ở `Assets/Resources/TransferredGarments/`, không ghi đè mesh thiết kế. Mỗi gói có `Garment.asset` cùng `transfer-report.json`.
8. Chỉ gói qua cổng kiểm tra mới được mặc thử trong tab TRANG PHỤC. Nếu thêm gói lúc đang Play, dừng và Play lại để làm mới danh sách.

## Điều kiện tương ứng cơ thể

Bản đầu yêu cầu body nguồn và body đích có **cùng thứ tự đỉnh, cùng chỉ số tam giác và cùng tư thế nghỉ**. Có thể khác cỡ/phom trong giới hạn. Hai body chỉ có cùng số đỉnh chưa đủ; exporter có thể đổi thứ tự hoặc tách thêm đỉnh ở UV.

Dùng body chuẩn trong Unity và chỉ nhập bộ đồ là cách tránh mất tương ứng khi đi qua FBX/GLB/OBJ. Nếu đồ đã mặc trên avatar khác topology, phải đăng ký tương ứng body trước; công cụ hiện dừng thay vì tự đoán. Đích trong cửa sổ hiện là mannequin nữ fitting của dự án. Hai mannequin chỉnh khớp nam/nữ ở góc phòng chưa tự nhận trang phục này.

## Phép chuyển giữ gì và đo gì

Mô hình khớp affine toàn cục bằng least squares độ chính xác double, rồi nội suy phần dư theo tam giác gần nhất trên body nguồn. Điểm trang phục giữ khoảng lệch so với bề mặt body thông qua trường affine; không ép mọi điểm áo dính vào người. Cách này tránh nghịch đảo trực tiếp những tam giác body rất mảnh làm vỡ nút và đường chỉ. Normal/tangent được biến đổi, trọng số xương đích lấy theo barycentric, tối đa bốn ảnh hưởng mỗi đỉnh.

Mesh kết quả giữ thứ tự đỉnh, chỉ số từng submesh, UV0–UV7, màu đỉnh và tham chiếu vật liệu/texture sau import. Không remesh, weld, unwrap, tạo atlas hay thay ảnh họa tiết. Body tiếp xúc là mesh đầy đủ, không che người để vượt kiểm tra.

Cổng hiện tại: body kín, manifold, winding hướng ngoài; khoảng cách nguồn không quá 50 cm; lệch body không quá 15 cm; tỉ lệ cạnh body 0,65–1,5; cạnh trang phục đổi tối đa 25%; không tam giác giảm diện tích xuống dưới 10%; không điểm lấy mẫu đỉnh/tâm mặt xuyên sâu quá **1 mm**. Các ngưỡng là giới hạn kỹ thuật cho bản thử, không phải thông số vật lý của vải. Chỉ số/hash body, mesh và tư thế nghỉ được kiểm tra lại khi mặc.

Điều kiện cùng topology không tự chứng minh đúng danh tính từng điểm hoặc cùng tư thế. Cần người gửi xác nhận body nguồn; tài liệu tương ứng theo đỉnh là phần bắt buộc của bộ dữ liệu.

## Kết quả và bước tiếp theo

Xem `Verification/GarmentTransfer/REPORT.md`, `verification.json`, `formats.json`, `independent_contact.json` và ảnh `Transferred_Detail.png`. Kiểm tra độc lập lấy thêm điểm trên cạnh; không thay cho kiểm tra xuyên tam giác liên tục, tự xuyên, nhiều lớp đồ hoặc thử kính thật.

Để huấn luyện phần hiệu chỉnh phom, cần cặp **đồ 3D nguồn + body nguồn + body đích + đồ đã fit đúng trên body đích** cùng danh tính thiết kế, rập, UV và vật liệu. Render nhiều góc/cận cảnh dùng kiểm tra chi tiết; ảnh 2D không thay cho ground truth 3D. Chia train/validation/test theo thiết kế và người tạo trước khi tăng biến thể. Không coi kết quả tự chuyển của công cụ này là ground truth vật lý. Ưu tiên mẫu thực của sinh viên, dữ liệu đo vải và mô phỏng được kiểm tra; sau đó mới huấn luyện residual và đánh giá trên thiết kế giữ lại.

Tham khảo: [Unity glTFast chính thức](https://github.com/Unity-Technologies/com.unity.cloud.gltfast), [Mesh.bindposes](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Mesh-bindposes.html), [BakeMesh](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html), [nghiên cứu Deformation Transfer](https://people.csail.mit.edu/sumner/research/deftransfer/). Phương pháp trong dự án là baseline affine + barycentric residual tự triển khai, không phải bản cài đặt đầy đủ thuật toán Sumner–Popović.
