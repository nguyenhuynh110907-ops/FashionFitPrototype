Bản mới V087: [5 bộ Hollister nam tham chiếu, gồm các sửa pose/nón V086](HOLLISTER_MENS_VR_GUIDE.md).

# Chỉnh pose, nhấc nón và kéo vải — V086

Mở `FashionFit VR/01 - VAO GAME VR` trên Desktop sau khi vào Quest Link. Trong game bấm **Y** để mở bảng. Bản đúng ghi `FITTING_READY_V087_MENS_REFERENCE` trong log.

## Chỉnh dáng mannequin nam/nữ

1. Bấm nút **VẢI / đổi sang POSE** ở đầu bảng bằng tia Trigger của một trong hai tay. Nút chuyển thành **POSE**, các điểm khớp hiện ra.
2. Bấm Y để cất bảng, tiến sát mannequin. Đặt tay lên điểm khớp rồi giữ **Grip** để kéo; thả để giữ tư thế. Hai tay điều khiển được hai khớp riêng; thả một tay không nhả tay còn lại.
3. Hai tay nắm tròn phía trước bệ dùng di chuyển cả bệ. Không cần nắm xuyên vào ngực của mannequin.
4. Muốn chỉnh áo/tà/quần: bấm Y, đổi về **VẢI**, cất bảng rồi Grip sát bề mặt. Chế độ POSE ưu tiên khớp và không nhận kéo vải; chế độ VẢI ẩn điểm khớp để tránh chọn nhầm.

Các giới hạn khớp/IK vẫn được giữ. Đây là mannequin gắn trên bệ, không phải ragdoll tự đổ. Áo dài tiếp tục dùng chế độ fitting, chưa cho đi nhanh.

## Nón

- **Grip thường, một tay:** cầm/nhấc nón, kể cả nón đang đội. Di chuyển ra khỏi đầu rồi thả: nón rơi bằng trọng lực; thả sát đầu có thể đội lại theo vùng gắn nón.
- **Hai tay cùng Grip một nón:** đưa hai tay cùng hướng để di chuyển; kéo ngược hai phía để giãn, đưa gần nhau để nén. Buông một tay vẫn cầm được bằng tay còn lại.
- **Giữ Trigger trước rồi Grip nón đang đội:** chỉnh co/giãn tại chỗ, đầu làm điểm đỡ. Nhả cả hai nút rồi Grip thường để lấy nón xuống.
- Phải đưa tay đến gần bề mặt (khoảng dưới 11 cm). Tia menu không dùng kéo đồ từ xa.

## Chọn chất liệu

Trong Y → **ĐÍCH MẶC / VẬT LIỆU**, chọn đúng nhân vật và chọn vật liệu. Vải trên thân có điểm tựa từ cơ thể/đường treo; nón cầm hai tay lấy trung điểm để tách chuyển động cứng khỏi biến dạng. Vùng lực kéo lan qua các cạnh ràng buộc của lưới.

| Preset thử | Giới hạn kéo điều khiển | Thời gian hồi đặc trưng τ |
|---|---:|---:|
| Cotton dệt | 5 cm | 0,8 s |
| Vải co giãn | 10 cm | 1,3 s |
| Nỉ định hình | 2,5 cm | 1,8 s |
| Da mềm | 2 cm | 1,1 s |

Đối với nón hai tay, giới hạn là **tổng thay đổi khoảng cách hai điểm cầm**, chia đều hai phía. Đối với miếng vải trên thân, giới hạn là dịch chuyển yêu cầu tại điểm kéo; va chạm có thể đẩy các điểm thêm ra ngoài. Di chuyển tự do cả tà áo không đồng nghĩa kéo dài sợi vải: tà vẫn được nâng/xếp bằng solver riêng.

**Các số 5/10 cm là preset tương tác theo yêu cầu, chưa phải giới hạn thực nghiệm của cotton hay mọi loại vải.** Số N trong bảng là ước lượng lệnh ảo; tay Touch không đo lực kéo. Texture không cung cấp sức bền. Dữ liệu lực–chiều cao đã được kiểm tra riêng; chưa train hoặc hiệu chuẩn preset từ dữ liệu đó.

Khi thả, biến dạng hồi theo `u(t)=u_eq+(u0-u_eq) exp(-t/τ)`; `u_eq` phụ thuộc trọng lực/điểm đỡ/tiếp xúc, nên nón đang đội không co xuyên đầu. Nón rời dùng Rigidbody; tà áo dùng hạt XPBD có khối lượng và trọng lực; thân áo dùng mô hình bám da giảm bậc, chưa là FEM toàn phần.

Nếu không kéo được: kiểm tra chế độ POSE/VẢI, cất bảng Y, nhả Grip rồi nắm lại sát bề mặt; kiểm tra lớp được chọn nếu đang mặc áo dài. Khi mất tracking, game nhả thao tác để tránh giữ vật ở tọa độ cũ.

[Báo cáo và phạm vi kiểm tra](../Verification/PoseBalance/REPORT.md)
