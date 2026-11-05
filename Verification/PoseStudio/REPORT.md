Đã kiểm tra đứng yên rồi tăng nhịp 0,35× / 0,50×: [cách thử và phạm vi đo](../../Docs/SPEED_CONTACT_GUIDE.md).

# Hai mannequin nam/nữ — nguồn GitHub và Grip chỉnh khớp

## Đã đưa vào project

Hai mannequin toàn thân màu charcoal được đặt trong khu POSE STUDIO bên phải phòng. Dữ liệu cơ thể nam và nữ được lấy từ [MakeHuman Community](https://github.com/makehumancommunity/makehuman/tree/a8bc2d54ff0ac92e78ff71431b1023eda42bf482), ghim commit `a8bc2d54ff0ac92e78ff71431b1023eda42bf482` để tái tạo được. [Giấy phép nguồn](https://github.com/makehumancommunity/makehuman/blob/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/LICENSE.md) tách mã AGPL và tài sản đồ họa CC0; bản này chỉ dùng base mesh, target hình thể và giấy phép tài sản.

Đây là dữ liệu hình thể được chuyển đổi và rig trong project, không phải hai FBX hoàn thiện tải sẵn. Không huấn luyện AI hoặc dựng người từ ảnh. Bản v0.7 trong Downloads không bị sửa; code được áp vào bản mới Desktop theo đường dẫn đã xác nhận.

| Mẫu | Chiều cao | Đỉnh | Tam giác | Xương/điểm Grip |
|---|---:|---:|---:|---:|
| Nữ | 1,72 m | 13.380 | 26.756 | 17 |
| Nam | 1,82 m | 13.380 | 26.756 | 17 |

Giữ topology gốc của bề mặt cơ thể, không thêm đỉnh trung điểm chỉ để tăng số lượng. Bỏ mesh helper dùng để xác định mốc khớp. Tạo rig generic, skinning tối đa bốn xương/đỉnh và material charcoal; xuất prefab Unity, mesh/material và OBJ. File OBJ không có xương.

[Hai mẫu ở tư thế ban đầu](TwoModels_Rest.png) · [Sau khi kéo cổ tay, hiện điểm khớp](TwoModels_Posed.png) · [Trang điều khiển trong VR](VR_JointMenu.png).

## Hành vi tương tác

- Chọn bằng tia hoặc đưa tay vào gần điểm. Grip phải bắt đầu kéo; thả Grip giữ tư thế. Desktop dùng E.
- Cổ tay/cổ chân điều khiển chuỗi IK hai xương, giới hạn độ với và gập. Vai/háng xoay có giới hạn; khuỷu/gối dùng trục gập; hông, lưng, cổ và đầu có giới hạn góc.
- Không thay đổi localPosition hoặc scale của xương khi kéo, nên không kéo dài chi. Làm chậm tốc độ thay đổi góc và mục tiêu IK để giảm nhảy tư thế.
- Dữ liệu vị trí/quaternion không hợp lệ bị bỏ qua. Kết thúc Grip khi mất tracking dùng cùng đường EndActiveGrab có sẵn; vẫn phải thử sự kiện tracking thật trên Quest.
- Có nút đặt lại riêng từng mẫu, bật/ẩn điểm khớp và nút tới mẫu trong giao diện desktop/VR. Khi điểm ẩn, collider điều khiển cũng tắt.
- Bổ sung vùng tránh hai mẫu khi đi bằng cần. Chưa bổ sung bộ giải va chạm giữa các bộ phận cơ thể khi tạo tư thế.

## Kiểm thử

[Kết quả khớp](verification.json), [log kiểm tra cuối](pose-verification.log), [hash file đã áp dụng](source_snapshot.json).

Hai mẫu được tải, 34/34 điểm chọn được qua đường Grip giả lập và thay đổi tư thế; có kiểm tra Grip gần ở cổ tay, giữ tư thế sau thả, trả về rest pose, giới hạn góc và bỏ qua vị trí NaN. Các localPosition xương không đổi trong bài thử. Bài BakeMesh xác nhận bề mặt cơ thể thực sự thay đổi theo xương, không chỉ di chuyển marker. Hai prefab có đủ SkinnedMeshRenderer và 17 bone references.

Các số đo chi tiết nằm trong JSON. Đây là các kiểm tra có dữ liệu đầu vào tổng hợp, không phải đánh giá người dùng hoặc kiểm tra 34 khớp bằng kính thật.

Kết quả cuối: **34/34 điểm Grip bằng tia, 2/2 lần Grip gần ở cổ tay**, 4.186 đỉnh thay đổi trên tổng hai mẫu trong bài thử kéo cổ tay. Sai lệch localPosition xương, tư thế giữ sau thả và đặt lại tư thế đều bằng 0 trong các ca thử. Sai số vượt giới hạn góc lớn nhất khoảng 0,000008°, nằm trong sai số số thực.

Kiểm tra hồi quy: [21 mục di chuyển](movement-verification.json), [lấy/thả nón và sáu bộ đồ](showroom-verification.json), [31 mục tủ/menu](atelier-verification.json). Vẫn chỉ một FittingCloth cho mannequin thử đầm; hai mẫu pose không tạo thêm solver vải.

Đã mở bản Desktop trong Unity và bấm lần lượt nút xem mẫu nam, mẫu nữ; cả hai camera và cụm điểm điều khiển hiển thị. [Đoạn log của phiên Play](live-session.log) không ghi exception/C# error ở thời điểm rà soát. 49 file nguồn/tài nguyên đã áp vào Desktop khớp hash bản cô lập đã kiểm thử; không coi FPS hiển thị trên Editor là kết quả FPS trong kính.

Ảnh được render Unity trên PC. Trong batch Editor, ảnh pose dùng bản BakeMesh mới để tránh camera tái sử dụng buffer skinned mesh của cùng một frame; runtime vẫn dùng SkinnedMeshRenderer bình thường.

## Cách dùng và giới hạn

[Hướng dẫn từng bước](../../Docs/POSE_STUDIO_GUIDE.md).

Hai mẫu mới chưa nhận các bộ trang phục runtime; không coi kiểm tra khớp là kiểm tra chống xuyên vải. Skin weight đơn giản có thể biến dạng chưa đẹp ở các góc gập mạnh. Rig generic chưa được kiểm chứng cho Humanoid retarget, chưa có khớp ngón tay, chưa lưu pose ra tệp, chưa mô phỏng tự va chạm cơ thể và chưa đo FPS/độ trễ/stereo bằng Meta Quest thật.
