# Báo cáo tham chiếu đầm và chuyển động runway

Ngày nghiên cứu: 2026-10-27

Bổ sung nghiên cứu chuyển động và tiêu chí bắt buộc không xuyên: [RUNWAY_MOTION_AND_CONTACT_RESEARCH.md](RUNWAY_MOTION_AND_CONTACT_RESEARCH.md). Báo cáo bổ sung đính chính: công thức bob hiện tại tạo khoảng biến thiên 0–24 mm; các thông số giảm mật độ/biên độ dưới đây là đề xuất để thử, chưa phải kết quả đo từ video. Lấy mẫu va chạm luân phiên không đủ để đáp ứng yêu cầu tuyệt đối không xuyên.

## Phạm vi đã xem

Đã đối chiếu ảnh đầm midi, đầm couture có train, đầm ôm–xòe và cảnh người mẫu bước trên runway; đồng thời đọc nghiên cứu gait của 69 người mẫu và hướng dẫn mô phỏng từ CLO/Marvelous Designer. Ảnh chỉ được dùng làm tham chiếu thị giác, không sao chép asset, logo hoặc thiết kế độc quyền vào project.

Tham chiếu hình ảnh:

- Robert Wun couture: thân trên có cấu trúc, phần tulle/train chuyển động trễ và giữ silhouette lớn.
  https://jingdaily.com/posts/blackpink-loved-robert-wun-is-paris-couture-highlight
- Philosophy di Lorenzo Serafini SS20 look 41: đầm midi nhẹ, eo rõ, chuyển động tập trung dưới eo.
  https://www.tag-walk.com/en/look/195316
- Carolina Herrera SS26: đường nét sạch, tỷ lệ chính xác và chuyển động được kiểm soát.
  https://www.glamourmagazine.co.uk/article/carolina-herrera-ss26-kitten-heels
- Chanel couture SS24: tulle và train cần chuyển động nhiều lớp, không thể mô phỏng như một mặt vải đồng nhất.
  https://fashionista.com/2024/01/chanel-haute-couture-spring-2024-review

## Những gì học được từ phom đầm thật

1. **Thân trên không phải vải tự do.** Cổ, vai, ngực và eo được giữ bởi đường may, lót, corsetry hoặc cấu trúc vật liệu. Khu vực này nên skin theo xương và chỉ có biến dạng thứ cấp nhỏ.
2. **Chuyển động lớn bắt đầu dưới eo/hông.** Váy xòe tạo sóng ở gấu; váy cột chỉ mở đủ cho bước chân; đầm midi nằm giữa hai mức này.
3. **Silhouette quan trọng hơn số điểm.** Một mesh nhiều điểm nhưng bán kính ngực/eo/hông sai vẫn xấu và xuyên. Trước hết phải đúng đường cong trước–sau, độ rộng vai, ease tại ngực và khoảng hở ở hông.
4. **Vải không bám đều lên cơ thể.** Các điểm tựa nằm ở vai, cổ, eo hoặc đai. Phần giữa các điểm này có khoảng hở và nếp rơi riêng.
5. **Mỗi vật liệu cần hành vi khác nhau.** Satin giữ bề mặt dài và phản sáng rõ; chiffon/tulle nhẹ, có độ trễ và nhiều lớp; vải couture dày giữ thể tích và ít rung tần số cao.

## Những gì học được từ chuyển động người mẫu

Nghiên cứu năm 2024 phân tích video runway của 69 người mẫu và phân loại thành năm nhóm gait. Xu hướng gần đây giảm kiểu bắt chéo chân quá mức. Chuyển động runway hiện đại thường giữ thân trên thư giãn, ít dao động thừa và để trang phục là tâm điểm.

Áp dụng cho mannequin:

- Giảm bob hông từ 12 mm xuống khoảng 5–7 mm.
- Giảm xoay ngực; vai giữ ổn định, tay đánh nhỏ và chậm.
- Không dùng catwalk bắt chéo quá mạnh; bàn chân đi gần đường giữa nhưng không xuyên nhau.
- Khi quay hoặc dừng, cơ thể dừng trước; gấu váy tiếp tục thêm một khoảng ngắn rồi settle.
- Tạo ba clip riêng: `RunwayWalk`, `TurnPose`, `StopSettle`, thay vì một hàm sin điều khiển toàn thân.

Nguồn nghiên cứu gait:

- https://pmc.ncbi.nlm.nih.gov/articles/PMC11207723/
- https://pubmed.ncbi.nlm.nih.gov/36816467/

## Vì sao bản hiện tại lag

- Mỗi váy có 2.112 điểm và mỗi áo có 1.152 điểm, nhưng solver chạy nhiều vòng constraint, nhiều substep và nhiều lần kiểm tra collider cho từng điểm.
- Chất lượng hiển thị và chất lượng mô phỏng đang dùng chung một mesh. Đây là sai hướng tối ưu: mesh render có thể đẹp, nhưng mesh physics phải thưa hơn.
- Kiểm tra virtual-particle/collider trên CPU làm chi phí tăng theo `điểm × collider × iteration × substep`.
- Cả bộ Ivory chạy đồng thời solver áo và solver váy.
- Recalculate normals trên mesh dày mỗi frame cũng tiêu tốn CPU.

## Kiến trúc nên thay thế

### Chế độ tương tác mặc định

- Váy physics: khoảng 32 segment × 18–22 ring, tương đương 576–704 điểm.
- Áo physics: khoảng 32 segment × 10–12 ring, tương đương 320–384 điểm.
- Mesh hiển thị chi tiết được skin/barycentric theo mesh physics thưa.
- 2 substep chỉ khi VR đang kéo hoặc chân chuyển động nhanh; bình thường dùng 1 substep.
- Collider ảo chỉ chạy ở vùng hông, đùi, ngực và gấu gần chân, không quét toàn bộ tam giác.
- Normals cập nhật cách khung hoặc dùng shader normal/detail để tạo cảm giác dày nếp.

### Chế độ chất lượng cao

- Chỉ bật khi mannequin đứng hoặc khi chụp ảnh.
- Tăng solver iterations và mesh render, settle vài giây rồi đóng băng/cache kết quả.
- Không dùng cấu hình này làm mặc định trong VR.

### Phân vùng trang phục

- `Bodice`: skinned mesh, gần như không mô phỏng.
- `Waist band / seam`: hàng điểm neo cứng hoặc compliance rất thấp.
- `Upper skirt`: tether ngắn, damping cao.
- `Lower skirt / hem`: tether dài, damping vừa và bend mềm.
- `Train`: solver riêng hoặc chain thưa, ma sát sàn cao hơn.

## Thông số từ phần mềm ngành may mặc

Marvelous Designer khuyến nghị particle distance lớn khi dựng/thử để có tốc độ, sau đó mới giảm xuống dưới 5 mm cho chất lượng hoàn thiện. Tài liệu cũng cảnh báo particle distance quá thấp có thể làm animation nảy; mức 8–10 thường phù hợp hơn cho animation. Collision thickness và avatar skin offset cần được điều chỉnh cùng nhau, thay vì chỉ tăng số điểm.

- Particle distance và tốc độ/chất lượng:
  https://www.marvelousdesigner.com/en-ca/manuals/articles/47358268602777
- Collision thickness và skin offset:
  https://support.marvelousdesigner.com/hc/en-us/articles/47358430412441-FABRIC-PHYSICAL-PROPERTIES-Adjust-Collision-Thickness
- Animation tips:
  https://support.clo3d.com/hc/zh-cn/community/posts/900003281663-Animation-Tips-Tricks

## Kết luận

Bước đúng tiếp theo không phải tăng thêm điểm. Cần giảm mesh physics khoảng 65–75%, tách mesh render khỏi mesh mô phỏng, thay chuyển động procedural hiện tại bằng ba clip runway có pose rõ ràng, và chia trang phục thành các vùng cứng–mềm. Cách này vừa đẹp hơn, ổn định hơn, vừa có cơ hội đạt FPS VR; tiếp tục tăng điểm/collider trên solver CPU hiện tại sẽ làm lag nặng hơn mà không bảo đảm hết xuyên.
