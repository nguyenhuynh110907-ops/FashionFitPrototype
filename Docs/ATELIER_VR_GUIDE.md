Bản tương tác hai tay: [Grip gần, chỉnh khớp và texture](VR_TWO_HANDS_GUIDE.md).

Vào lại VR: [mở phiên thử Quest Link và làm ba bước](VR_REENTRY_GUIDE.md).

Đã kiểm tra đứng yên rồi tăng nhịp 0,35× / 0,50×: [cách thử và phạm vi đo](SPEED_CONTACT_GUIDE.md).

Hai model nam/nữ và Grip chỉnh khớp: [hướng dẫn Pose Studio](POSE_STUDIO_GUIDE.md), [nguồn GitHub và báo cáo](../Verification/PoseStudio/REPORT.md).

# Tủ trưng bày và giao diện VR mới

Đã sửa trực tiếp project. Nếu đang dùng phiên Play cũ, Stop → chờ compile → Play lại.

## Kiểm tra ngay trên màn hình

1. Trong **VR / CAMERA**, bấm **Xem bàn nón và tủ đồ** để xem tủ mới.
2. Bấm **Xem thử giao diện VR trên màn hình**. Bản xem này không khởi tạo kính hoặc OpenXR.
3. Dùng chuột trái chọn tab/nút trong bảng 3D. Phím **Y** mở/ẩn, **X** đưa bảng về trước camera.
4. Tab **TRANG PHỤC** cho chọn đủ sáu bộ, bộ đang mặc có nhãn và màu riêng.

Tủ mới có ba khoang, nền vải tối, nẹp kim loại, ánh sáng ấm, ngăn kéo và biển tên. Sáu mẫu treo là bản trưng bày tĩnh lấy từ sáu trang phục thực tế trong game, không còn cùng một silhouette cho mọi bộ. Bản trưng bày đã giảm chi tiết ở bề mặt lớn để tiết kiệm chi phí; không chạy thêm sáu solver vải.

## Dùng trong kính Meta

| Điều khiển | Tác dụng |
|---|---|
| **Y tay trái** | Mở/ẩn menu; nếu menu ở xa hoặc sau lưng, đưa về trước |
| **X tay trái** | Đưa menu về trước mặt ngay |
| **Tia tay phải + Trigger** | Chọn tab và bấm nút; nút đổi màu khi trỏ trúng |
| **Grip phải** | Lấy/kéo vật; menu tự ẩn để nhìn rõ vật đang cầm |
| **Cần trái / phải** | Đi tự do / quay như bản di chuyển trước |

Menu có ba trang:

- **THỬ ĐỒ:** đứng yên, chạy/dừng trình diễn chậm, đến bàn/tủ, trả phụ kiện.
- **TRANG PHỤC:** sáu bộ, hiển thị rõ bộ đang mặc.
- **CÀI ĐẶT:** quay mượt/45°, tốc độ đi, đồ họa, đổi góc, đặt lại mannequin và thoát VR.

Bảng xuất hiện cách đầu khoảng 1,3 m, không bám theo động tác cúi/ngẩng hoặc nghiêng đầu. Bạn có thể quay đầu quan sát trang phục rồi bấm X khi muốn đưa bảng về trước. Khi cầm vật, có nhãn tên và gợi ý thả Grip gần đầu/thân để đội/mặc.

Mở bảng khi phía trước có khoảng trống. Bảng có kiểm tra độ sâu nên có thể bị đồ vật hoặc tường che nếu mở sát chúng; hãy quay về khoảng trống rồi bấm X để đặt lại.

Để chuyển bộ nhanh, chọn trực tiếp trong menu. Để thực hành thao tác, giữ Grip lấy mẫu treo khỏi tủ rồi đưa gần thân mannequin và thả. Mười nón trên bàn vẫn giữ cách cầm/đội như trước.

[Kết nối Quest Link](KET_NOI_QUEST_LINK.md) · [Đi tự do trong phòng](VR_FREE_MOVEMENT.md) · [Báo cáo rà soát và ảnh](../Verification/AtelierPolish/REPORT.md).

Các ảnh kiểm tra là render Unity trên PC. Cần thử thêm trong kính thật để xác nhận chữ, độ dễ bấm và độ mượt trên thiết bị của bạn.
