[Bệ pose di chuyển bằng Grip và điều khiển hai tay mới](VR_TWO_HANDS_GUIDE.md).

# Hai mannequin nam/nữ và chỉnh khớp bằng Grip

Đã thêm vào bản project `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`.

## Thử ngay bằng màn hình

1. Mở đúng project Desktop bằng Unity 2022.3.62f1. Stop → chờ compile → Play.
2. Vào **VR / CAMERA**. Ở đầu bảng có **MẪU NAM / NỮ · CHỈNH KHỚP**.
3. Chọn **Mẫu nữ 3D · nhìn gần và chỉnh khớp** hoặc **Mẫu nam 3D · nhìn gần và chỉnh khớp**. Camera tới khu POSE STUDIO và hiện điểm xanh.
4. Trỏ chuột vào một điểm xanh, giữ **E**, rê chuột để kéo. Thả E để giữ tư thế. Giữ chuột phải để thay đổi góc nhìn nếu cần.
5. Dùng **Đặt lại mẫu nữ/nam** để trở về tư thế ban đầu. **Ẩn điểm khớp** chỉ ẩn các nút điều khiển, vẫn giữ tư thế.

## Thử bằng kính Meta qua Link

Kết nối kính theo [hướng dẫn Quest Link](KET_NOI_QUEST_LINK.md), rồi vào VR trong project.

1. Bấm **Y trái** mở menu, chọn **CÀI ĐẶT → Mẫu nam / nữ, chỉnh khớp** bằng tia tay phải và **Trigger**.
2. Chọn **Mẫu nữ 3D / Mẫu nam 3D → nhìn gần**. Menu ẩn, điểm khớp hiện.
3. Đưa tay phải gần điểm xanh hoặc trỏ tia vào điểm; giữ **Grip phải** (nút bên hông tay cầm).
4. Kéo nhẹ hoặc xoay tay; điểm đang cầm chuyển vàng. Thả Grip để giữ tư thế.
5. Bấm Y để mở menu lại; X trái đặt menu về trước khi cần. Trong trang mẫu có nút hiện/ẩn điểm và đặt lại riêng từng mẫu.

Nên bắt đầu với cổ tay rồi thử khuỷu, vai, cổ và đầu. Ở cự ly gần có thể chọn trực tiếp bằng tay; điểm cổ tay/cổ chân dùng IK hai xương, không kéo giãn chi.

| Điểm | Cách điều chỉnh |
|---|---|
| Cổ tay | Kéo để vai/khuỷu gập theo; xoay tay để chỉnh hướng bàn tay |
| Cổ chân | Kéo để háng/gối gập theo; có giới hạn độ với và độ cao mục tiêu |
| Vai, khớp háng | Kéo/xoay để xoay phần chi từ gốc |
| Khuỷu, gối | Gập theo một trục có giới hạn |
| Hông, lưng dưới, ngực/lưng trên, cổ, đầu | Xoay có giới hạn góc |

## Model và tài sản Unity

- Hai hình thể người trưởng thành từ dữ liệu đồ họa **MakeHuman Community trên GitHub, CC0**; không dựng từ ảnh tham chiếu và không sao chép mã ứng dụng AGPL.
- Nữ cao 1,72 m, nam cao 1,82 m; mỗi mẫu **13.380 đỉnh, 26.756 tam giác, 17 xương/điểm điều khiển**.
- Hai prefab có SkinnedMeshRenderer và xương: `Assets/FashionFit/Models/PoseMannequins/FemaleCC0.prefab`, `MaleCC0.prefab`. Có mesh/material riêng để tái sử dụng trong Unity.
- OBJ để mở trong phần mềm 3D: `Samples/Pose_Female_Body.obj`, `Samples/Pose_Male_Body.obj`. **OBJ không chứa rig**; dùng prefab nếu cần xương.
- Dữ liệu gốc, giấy phép và hash: `Tools/SourceAssets`, [nguồn đã ghim](../Verification/PoseStudio/SOURCES.json).

## Phạm vi bản thử

Đây là hai mẫu chỉnh tư thế riêng ở bên phải phòng. Mannequin nữ đang thử đầm và solver vải hiện tại hoạt động độc lập; chưa chuyển các bộ đầm sang hình thể nam hoặc tự căn trang phục cho hai mẫu mới.

Rig là bộ xương generic 17 xương do project tạo từ các mốc trong mesh; chưa cấu hình Avatar Humanoid để retarget animation, chưa có điều khiển từng ngón tay. Giới hạn góc và IK giúp tránh kéo dài xương, nhưng chưa có bộ giải tự va chạm toàn thân; các tư thế ép tay/chân qua thân vẫn cần chỉnh lại. Skinning dùng bốn trọng số mỗi đỉnh, có thể cần chỉnh weight khi gập cực hạn.

Tư thế giữ trong phiên Play, chưa lưu ra tệp. [Báo cáo và ảnh kiểm thử](../Verification/PoseStudio/REPORT.md). Chưa kiểm tra bằng headset/controller thật hoặc đo FPS trong kính.
