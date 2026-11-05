# Kết quả chuyển trang phục 3D → 3D

Cập nhật 29/10/2026. Thay đổi áp trực tiếp vào `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`; kiểm thử bằng bản Unity tách biệt trước khi chép. Đây là baseline hình học đã chạy, **không phải checkpoint AI mới huấn luyện**.

## Chức năng đã triển khai

- Cửa sổ **Fashion Fit → 3D to 3D → Transfer Garment**, nhận renderer từ asset FBX, GLB, OBJ đã nhập trong Unity.
- FBX/OBJ dùng importer gốc, thêm lệnh chuẩn bị Read/Write và bảo toàn lưới. GLB dùng Unity glTFast **6.12.1** từ Unity Registry, Apache-2.0; minimum Unity trong metadata là 2020.3.48f1, đã chạy trên 2022.3.62f1. Không nâng phiên bản Editor.
- Body tham chiếu và mẫu áo/cổ/nút lưu thành prefab/mesh/material native, kèm OBJ body tĩnh cho người thiết kế. Không phụ thuộc mesh tạm chỉ sống trong RAM.
- Phép khớp affine dùng double precision rồi chuyển phần dư theo barycentric của body có tương ứng. Không nghịch đảo trực tiếp tam giác body mảnh để di chuyển chi tiết nhỏ. Giữ topology, từng submesh, UV0–UV7, màu đỉnh, tham chiếu vật liệu sau import; chuyển normal/tangent và trọng số xương.
- Gói kết quả riêng trong Resources, kèm báo cáo. Kiểm tra body kín/hướng mặt, kích thước, khoảng cách, méo cạnh, sụp tam giác, tiếp xúc và bind pose. Nguồn không bị thay mesh.
- Bản mặc thử trong tab TRANG PHỤC; riêng đồ chuyển giữ tư thế nghỉ. Nút đi, tua và restart chưa thể bỏ qua cổng này. Các mẫu có cloth solver trước đây vẫn trình diễn được.

## Kiểm tra và số đo

`verification.json`: **150 kiểm tra đạt**, gồm phép chuyển tịnh tiến/quay/đổi cỡ có nghiệm biết trước, BVH đối chiếu duyệt hết tam giác, UV phụ có dữ liệu thật, normal/tangent/màu đỉnh, vật liệu, source không đổi, xương, chống bỏ qua cổng chuyển động và template lưu bền vững.

Mẫu kiểm tra thật trong dự án gồm **3 phần mesh, 15.471 đỉnh, 28.510 tam giác, 5 ô vật liệu**: thân áo/tay, chi tiết cổ/nẹp/bốn nút/đường chỉ và chân váy. Nó là bộ nghiên cứu từ trước trong dự án, không phải mesh Gucci chính hãng hoặc bộ 3D do sinh viên cung cấp.

| Trường hợp | Sai lệch vị trí so với mesh tham chiếu | Kết quả |
|---|---:|---|
| Nguồn và đích cùng body | 0 m | Giữ nguyên vị trí chi tiết |
| Body nguồn/đồ nguồn hẹp 2% theo X/Z, chuyển về body đích | tối đa 0,0002403 mm | Qua cổng hình học; thay đổi cạnh tối đa khoảng 2,514% |

Đây là phép thử có biến đổi tổng hợp biết trước, không phải sai số chung cho mọi thiết kế/cơ thể. Thời gian xử lý riêng gói mesh khoảng **1 giây** trên máy hiện tại; không tính import lạnh, tải package, biên dịch hoặc huấn luyện. Tính toán chuyển chạy khi chuẩn bị asset, không lặp mỗi frame VR.

`formats.json`: cả **FBX, GLB và OBJ** đều nhập được fixture tự tạo, mỗi mẫu 6 đỉnh / 2 tam giác / 2 ô vật liệu và kích thước 0,24 × 0,10 m. OBJ giữ texture MTL; GLB giữ texture nhúng và UV1. FBX fixture có quy ước trục riêng, phải căn frame tường minh như ghi trong JSON; kiểm tra không coi nó là bộ đồ mặc đúng sẵn. Các fixture chỉ chứng minh đường nhập này hoạt động, không chứng nhận toàn bộ spec hoặc mọi shader/extension.

`independent_contact.json`: dùng **libigl + trimesh + Embree** độc lập với bộ kiểm tra C#, trên body đầy đủ, kín và nhất quán hướng mặt. Lấy các đỉnh, tâm tam giác, và điểm 1/4, 1/2, 3/4 trên từng cạnh duy nhất.

| Trường hợp | Điểm bề mặt duy nhất | Điểm xuyên > 1 mm | Xuyên lớn nhất đo được |
|---|---:|---:|---:|
| Giữ body | 173.737 | 0 | 0,866005 mm |
| Chuyển từ body hẹp 2% | 173.718 | 0 | 0,865995 mm |

Tổng **347.455 điểm**, ngưỡng 1 mm. Điểm kiểm chứng đặt trong thân người được nhận diện sâu khoảng 25,416 mm, nên phép kiểm tra không chỉ trả về toàn bộ là bên ngoài. Không che body để đạt số đo. Ngưỡng 1 mm và lấy mẫu hữu hạn không đồng nghĩa không xuyên tuyệt đối.

Ảnh kết quả: `Transferred_Detail.png`. JSON và log hồi quy VR/tốc độ nằm cùng thư mục. Không có thử kính/controller thật trong đợt kiểm tra này.

## Lỗi tìm thấy và đã xử lý

Phân loại chỉ bằng normal của mặt gần nhất báo nhầm xuyên ở một số cạnh lõm vùng nách. Bản hiện tại dùng dấu normal khi hình chiếu nằm trong mặt, và bỏ phiếu ray parity ở cạnh/đỉnh; đối chiếu độc lập bằng signed distance. Body không kín hoặc winding sai bị từ chối.

Bản thử khung affine riêng trên từng tam giác gây méo lớn ở các tam giác rất mảnh khi đổi cỡ 2%. Cổng chất lượng đã chặn bản đó. Phương pháp hiện tại dùng affine toàn cục ổn định rồi nội suy residual để giữ chi tiết; phép thử đổi cỡ hiện đạt sai số ghi ở trên. Các con số thất bại của bản thử trước không dùng làm kết quả cuối.

## Giới hạn cần giữ khi phát triển tiếp

1. Body nguồn–đích phải có tương ứng theo đỉnh/tam giác đã xác nhận, cùng tư thế nghỉ. Chưa có đăng ký tự động cho hai avatar khác topology. Cùng số đỉnh và cùng connectivity không tự chứng minh danh tính điểm là đúng.
2. Đích trong cửa sổ hiện là mannequin nữ fitting. Chưa chứng nhận chuyển sang mannequin nam, thay đổi tư thế mạnh hoặc body cực đoan.
3. Chưa có va chạm tam giác liên tục, tự xuyên, nhiều lớp vải, mô phỏng rập/vật liệu hoặc kiểm chứng chuyển động cho đồ mới nhập. Vì vậy chưa mở bước đi hoặc kéo vải VR cho gói chuyển.
4. Không tự tạo thêm ren, thêu, sợi, khóa, lót hoặc các mặt không có trong mesh nguồn. Độ chi tiết tối đa bị giới hạn bởi asset/UV/texture/shader gốc. GLB extension chưa cho phép và blend shape bị chặn thay vì âm thầm loại bỏ.
5. Chưa nhận asset 3D thật của sinh viên trong đợt này. Cần đối chiếu các góc/cận cảnh từ phần mềm nguồn; kết quả thử này chưa đủ để gọi là “chính xác nhất” cho mọi bộ đồ.

Hướng dẫn dùng: [GARMENT_3D_TRANSFER_GUIDE.md](../../Docs/GARMENT_3D_TRANSFER_GUIDE.md). Bước kế tiếp có ích nhất là kiểm tra một bộ đồ thật cùng body nguồn, lập cặp 3D fit đã được duyệt và đánh giá theo thiết kế giữ lại trước khi huấn luyện phần residual.

Nguồn kỹ thuật: [Unity glTFast](https://github.com/Unity-Technologies/com.unity.cloud.gltfast), [metadata phiên bản từ Unity Registry](https://packages.unity.com/com.unity.cloud.gltfast), [Deformation Transfer](https://people.csail.mit.edu/sumner/research/deftransfer/), [libigl point_mesh_squared_distance](https://libigl.github.io/libigl-python-bindings/api/igl/#point_mesh_squared_distance). Mã fitting là baseline tự triển khai, không phải bản cài đặt đầy đủ phương pháp trong bài báo.
