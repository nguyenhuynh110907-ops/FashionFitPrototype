# Đứng yên → 0,35× → 0,50×: kiểm tra tiếp xúc cơ thể và vải

## Thứ tự và thay đổi

Đã kiểm tra sáu trang phục khi đứng yên trước khi chạy mức nhanh hơn. Chỉ sau khi đạt phép đo mesh đầy đủ mới thử 0,35×; chỉ sau khi 0,35× đạt mới nâng giới hạn trong bản cô lập lên 0,50× và kiểm tra tiếp. Các nút/giới hạn mới được đưa vào project Desktop sau khi các ca dưới đây đạt.

Khởi động vẫn đứng yên, mức chậm vẫn 0,18×. Thêm nút 0,35× và 0,50× vào BẮT ĐẦU và trang **THỬ ĐỒ → Nhịp trình diễn** trong menu VR. Giữ ramp đổi nhịp 0,12×/giây, warmup, trọng lực theo thời gian thực, bước mô phỏng 1/60 giây và bộ giữ tư thế khi vi phạm collider đại diện. Không tăng biên độ bước hoặc đổi solver/mesh để đạt bài thử.

## Kết quả

| Ca thử, sáu bộ | Mesh tư thế | Điểm lấy mẫu | Điểm xuyên >1 mm | Độ xuyên lấy mẫu lớn nhất |
|---|---:|---:|---:|---:|
| Đứng yên: mới mặc và sau ổn định | 12 | 190.750 | 0 | 0,79825 mm |
| 0,35×, cập nhật 60 lần/giây | 54 | 858.375 | 0 | 0,79829 mm |
| 0,50×, cập nhật 60 lần/giây | 54 | 858.375 | 0 | 0,79828 mm |
| 0,50×, cập nhật 30 lần/giây | 54 | 858.375 | 0 | 0,79828 mm |

Tổng **174 mesh tư thế, 2.765.875 điểm lấy mẫu**. Mỗi ca động chạy đủ hai chu kỳ sau khi đạt tốc độ yêu cầu, xuất bốn pha mỗi chu kỳ và thêm trạng thái sau dừng. Trong chuỗi cập nhật còn kiểm tra lúc tăng tốc và dừng bằng collider đại diện; các bộ đều đạt tốc độ yêu cầu, hoàn thành chu kỳ và dừng được, không ghi nhận vi phạm proxy. Độ giãn lớn nhất trong chuỗi khoảng 1,392 lần chiều dài nghỉ; vẫn chưa phải mô hình vải không giãn.

- [Đứng yên](Standing/body_contact.json), [runtime](Standing/runtime.json).
- [0,35×](Speed035_60fps/body_contact.json), [runtime](Speed035_60fps/runtime.json).
- [0,50× / 60](Speed050_60fps/body_contact.json), [runtime](Speed050_60fps/runtime.json).
- [0,50× / 30](Speed050_30fps/body_contact.json), [runtime](Speed050_30fps/runtime.json).
- [Nút tốc độ, clamp và dừng](controls.json), [31 mục tủ/menu](atelier-verification.json), [Grip khớp nam/nữ](pose-verification.json), [hash nguồn](source_snapshot.json).

Các số 60/30 là delta thời gian đầu vào của bài mô phỏng, **không phải FPS đo được trong kính hoặc toàn ứng dụng**. Thời gian vải trong JSON là số đo bước tính được lưu gần nhất; không dùng làm thời gian toàn frame, nhất là ca 30 lần/giây có nhiều bước mô phỏng mỗi lần cập nhật.

## Cách đo và đối chiếu

Xuất cơ thể gốc đầy đủ bằng BakeMesh, không dùng mặt nạ che da. Bề mặt cơ thể đóng kín và winding nhất quán trong các tệp đo. Kiểm tra mọi đỉnh được tham chiếu và tâm tam giác của các mesh trang phục được render, gồm váy, thân áo, cổ/nẹp/nút ở mẫu 06.

Bài đứng yên được đo trước bằng Trimesh nearest-triangle signed distance: [kết quả tham chiếu](Standing/body_contact_broadphase.json), độ sâu lớn nhất 0,61809 mm. Để đo nhiều tư thế, bản QA dùng libigl cho truy vấn khoảng cách tam giác, giữ quy tắc dấu theo mặt gần nhất của Trimesh và Embree cho các ca cần tia. Đã đối chiếu cùng 12 mesh/hash: cùng kết quả ngưỡng 1 mm và không bỏ sót điểm sâu nhất của bộ đo tham chiếu. Bộ mới ghi một số tiếp xúc dưới 1 mm khác bộ tham chiếu, nên mức lớn nhất là 0,79825 mm; không coi hai bộ đo là giống hệt nhau. Bảng trên dùng bộ mới nhất quán cho cả đứng yên và chuyển động.

Phần tăng tốc thuần phân loại trong/ngoài bằng tia đã không khớp phép đo tham chiếu và không được dùng để duyệt tốc độ. Thư viện QA chỉ nằm ngoài Unity, không thêm vào Assets hoặc runtime game. Phiên bản và nơi lưu dump được ghi trong [phạm vi kiểm tra](verification.json).

## Ảnh kiểm tra

[Mẫu 03 ở 0,50×](Speed050_60fps/Look3_Moving.png) · [Mẫu 05 ở 0,50×](Speed050_60fps/Look5_Moving.png) · [Mẫu 06 ở 0,50×](Speed050_60fps/Look6_Moving.png).

Ảnh render Unity trên PC, dùng snapshot BakeMesh cho phần skinned để tránh cache của nhiều Camera.Render trong cùng một frame Editor. Kết luận hình học dựa trên dump mesh, không dựa riêng vào ảnh.

## Giới hạn

Đây là lấy mẫu tư thế rời rạc, không phải kiểm tra liên tục mọi cặp tam giác hay chứng nhận không xuyên tuyệt đối. Có sai khác dưới 1 mm được ghi rõ; không gọi là 0 xuyên. Chưa kiểm tra tự giao giữa các lớp vải, mọi góc nhìn, mọi vật liệu/gió/kích thước áo, thao tác kéo VR, trang phục nhập ngoài hoặc phần cứng Quest thật.

Hai mẫu nam/nữ ở POSE STUDIO chưa mặc trang phục; kết quả này không chứng nhận fit đồ cho hai hình thể mới. Chưa chạy tốc độ trên 0,50×. Nếu cần đi nhanh và biên độ tay/chân lớn hơn, cần kiểm tra lại thay vì chỉ mở rộng slider.

[Cách thử trong game](../../Docs/SPEED_CONTACT_GUIDE.md).

Đã mở lại Play của bản Desktop và xác nhận giao diện có đủ nút 0,18× / 0,35× / 0,50×. Phiên Play chính được quan sát đang chạy ở 0,35×, có người mẫu mặc đồ và hai mẫu pose ở phía sau. [Log từ lần khởi động Play cuối](live-session.log) không ghi exception/C# error tại thời điểm kiểm tra. Đây là quan sát trên màn hình PC, không thay thế phép thử kính.

## Tài liệu bộ đo

- [libigl: point_mesh_squared_distance](https://libigl.github.io/libigl-python-bindings/api/igl/#point_mesh_squared_distance).
- [Trimesh: RayMeshIntersector](https://trimesh.org/trimesh.ray.ray_pyembree.html).
