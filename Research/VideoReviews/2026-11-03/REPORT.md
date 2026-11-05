# Đánh giá hai Reel và đề xuất cho FashionFit

Ngày rà soát: 03/11/2026. Phạm vi: lấy được hai video công khai, xem 14 khung hình theo mốc thời gian và đọc mô tả bài đăng; đối chiếu với repo/tài liệu gốc. Chưa chuyển toàn bộ lời nói thành bản chép. Video và khung hình nằm cùng thư mục, nguồn và SHA256 ở `RECEIPT.json`.

## Video 1 — GitHub Spec Kit

[Reel của Kỹ sư 4.0](https://www.facebook.com/reel/1140657575086031), dài 176,61 giây, giới thiệu quy trình xác định yêu cầu trước khi AI triển khai và đối chiếu kết quả sau khi làm. Hình ở 25 giây minh họa sửa một lỗi làm hỏng chức năng khác; 50–125 giây đi qua quy tắc dự án, làm rõ yêu cầu, chia việc và triển khai; 150 giây cho thấy đường rút gọn cho việc nhỏ.

[Repo chính thức](https://github.com/github/spec-kit) có 146.579 sao tại lần kiểm tra này, giấy phép MIT. [Tài liệu lệnh](https://github.com/github/spec-kit/blob/main/docs/reference/agentic-sdd.md) xác nhận luồng đầy đủ chín lệnh, bao gồm clarify, checklist, analyze và converge. Không bắt buộc dùng đủ chín bước cho mọi việc. Converge đối chiếu code với yêu cầu và bổ sung phần việc còn thiếu; vẫn cần kiểm thử thực chạy.

[Hướng dẫn dự án hiện hữu](https://github.com/github/spec-kit/blob/main/docs/guides/existing-projects.md) đề nghị bắt đầu từ một thay đổi có phạm vi nhỏ, dựa trên code và quy tắc sẵn có. Việc khởi tạo có thể thay tệp do công cụ quản lý tại đường dẫn trùng nhau; cần xem diff trên bản làm việc riêng trước khi áp dụng.

Đánh giá cho FashionFit: phù hợp để nối yêu cầu → quyết định kỹ thuật → việc cần làm → bằng chứng nghiệm thu. Hữu ích với yêu cầu kéo áo, giữ bằng hai tay, gặp đầu hoặc tay người mẫu và đổi body, vì đây là các hành vi dễ hiểu khác nhau nếu chỉ mô tả bằng một ảnh.

## Video 2 — Skills For Real Engineers

[Reel của Mint Tech](https://www.facebook.com/reel/1389200312767580), dài 46 giây, giới thiệu bộ kỹ năng của Matt Pocock. Hình ở 18–34 giây nhấn mạnh kỹ năng nhỏ có thể kết hợp, làm rõ thiết kế, dùng từ điển chung, kiểm thử và kiểm tra kiến trúc.

[Repo chính thức](https://github.com/mattpocock/skills) có 311.302 sao tại lần kiểm tra này, giấy phép MIT. [README hiện tại](https://github.com/mattpocock/skills/blob/main/README.md) dùng `GLOSSARY.md` cho từ điển nghiệp vụ, giải thích ví dụ `CONTEXT.md` thuộc phiên bản trước. README cũng khuyên chọn một cách cài cho mỗi agent để tránh tạo hai bộ skill giống nhau.

Các phần đáng chọn lọc:

- [diagnosing-bugs](https://github.com/mattpocock/skills/blob/main/skills/engineering/diagnosing-bugs/SKILL.md): tạo tín hiệu kiểm tra bắt được đúng lỗi, thu nhỏ ca tái hiện, kiểm chứng giả thuyết rồi kiểm tra lại lỗi gốc. Có nhánh riêng cho đo hiệu năng.
- [tdd](https://github.com/mattpocock/skills/blob/main/skills/engineering/tdd/SKILL.md): kiểm tra hành vi qua giao diện công khai, làm từng lát nhỏ. Tránh bài test phụ thuộc chi tiết nội bộ hoặc tự tính lại cùng một công thức để luôn đạt. Bản skill đang đọc đặt refactor ở giai đoạn review.
- [domain-modeling](https://github.com/mattpocock/skills/blob/main/skills/engineering/domain-modeling/SKILL.md): thống nhất nghĩa của các khái niệm nghiệp vụ và ghi quyết định đáng lưu. Phù hợp để phân biệt giãn vật liệu, dời cả áo, mặc vừa và giữ đồ bằng tay.

Đánh giá cho FashionFit: áp dụng được các nguyên tắc vào C#/Unity, nhưng ví dụ và công cụ kiểm tra phải dùng đúng môi trường Unity/VR. Chỉ làm rõ những điểm còn thiếu; tái sử dụng các yêu cầu người dùng đã xác nhận. Không dùng cách hỏi dồn ở mọi lần sửa.

## Đề xuất áp dụng vào dự án hiện tại

Đã đọc `AGENTS.md`, `CLAUDE.md` và `Docs/STUDIO_WORKFLOW.md` của source trên Desktop. Dự án đã có bản studio tinh gọn gồm sáu vai trò, ba quy trình và runner kiểm tra Unity. Nên giữ runner làm nơi thực thi kiểm chứng, bổ sung đặc tả hành vi và từ điển vào cùng hệ thống đó.

Phân công đề xuất:

| Thành phần | Trách nhiệm |
|---|---|
| Spec Kit hoặc mẫu tài liệu tương đương | Đặc tả hành vi, tiêu chí nghiệm thu, kế hoạch, phần việc còn thiếu |
| Các nguyên tắc chọn từ Matt Pocock | Tái hiện lỗi, kiểm thử hành vi, chẩn đoán hiệu năng, rà giới hạn module |
| FashionFit Studio hiện tại | Kiểm tra Unity, xuất mesh/ảnh/log, build thử, ghi phiên HMD thật |

Đây là đề xuất tổ chức công việc. Hai repo không cung cấp mô hình cơ học vải hay thuật toán chuyển trang phục 3D giữa body. Hiệu quả thực tế cần đo bằng số lỗi tái phát, số ca nghiệm thu đạt và chi phí mô phỏng.

### Thử đầu tiên: giữ cổ áo và vòng nách khi đổi body

1. **Ghi hợp đồng đầu vào.** Nêu đơn vị, trục, tư thế nguồn, body nguồn/đích, thông tin rig, UV và texture. Phân biệt dữ liệu có body nguồn với dữ liệu chỉ có quần áo; thông tin còn thiếu cần được báo rõ.
2. **Đặc tả hành vi.** Cổ trước/sau, vòng nách và các đường nối cần giữ quan hệ hình học đúng khi đổi body. Ghi rõ độ ôm/độ rộng mong muốn theo kiểu áo. UV và chi tiết bề mặt phải được giữ qua bước chuyển.
3. **Tạo ca tái hiện cổ lỗi.** Dùng một áo và body cố định, cùng góc trước/sau/nghiêng. Lưu đầu vào, ảnh và số đo để phát hiện chính lỗi cổ đang nhô hoặc hở bất thường.
4. **Đối chiếu phương án.** Dùng áo đã mô phỏng từ GarmentCode làm một đối chứng về rập và đường nối. Tách thời gian tính trước khỏi thời gian chạy mỗi frame. Kiểm tra lại trên body FashionFit vì kết quả body mặc định của repo chưa đại diện cho game.
5. **Nghiệm thu theo hành vi.** Chuyển body, mặc, kéo gần cổ/nách, thả, chỉnh pose và đi bộ; dùng kiểm tra mesh độc lập cùng ảnh render. Tăng tốc độ sau khi ca chậm đạt. Ghi riêng phần Touch thật chưa đánh giá.

Không đổi toàn bộ import, solver, grip và giao diện cùng một lượt. Đưa mỗi thay đổi qua cùng ca kiểm tra để xác định được nguyên nhân cải thiện hoặc hồi quy.

### Từ điển cần thống nhất

| Khái niệm | Nghĩa đề xuất |
|---|---|
| Fit | Trang phục phù hợp hình dáng, kích thước và độ rộng thiết kế trên body đích |
| Giãn | Độ dài/diện tích vật liệu thay đổi so với trạng thái nghỉ |
| Dời trang phục | Đồ vật đổi vị trí hoặc trượt trên cơ thể, có thể không đổi độ dài vật liệu |
| Hồi phục | Giảm biến dạng theo đặc tính vật liệu, đồng thời tuân theo tiếp xúc và các điểm đang giữ |
| Cầm/giữ | Một hoặc hai bàn tay áp ràng buộc tạm thời lên vật thể hoặc vùng vải |
| Tiếp xúc | Cơ thể hoặc vật khác đỡ/cản vải, kèm phản lực và ma sát theo mô hình đã chọn |

Đây là ngôn ngữ đề xuất cho bước triển khai, chưa phải thay đổi code. Trong đặc tả vật lý cần ghi rõ trường hợp áo bị đầu cản hoặc tay người mẫu khác giữ: không được tự kéo mesh xuyên vật cản để quay về vị trí mặc ban đầu.

### Tiêu chí đánh giá đợt áp dụng

- Ca cổ áo lỗi có thể bị kiểm tra bắt lại trước khi sửa và đạt sau khi sửa.
- Tệp nguồn, UV, đường cổ/nách và thông số chuyển được kiểm tra, có ảnh đủ góc để đánh giá thẩm mỹ.
- Phân biệt kết quả tĩnh, kết quả chuyển động và trải nghiệm HMD/Touch thật.
- Bài kiểm tra có thể chạy lại bằng một lệnh, dùng dữ liệu mới của đúng bản build.
- Báo cáo nêu rõ phần đạt và phần chưa đánh giá; không lấy việc build thành công hoặc checklist đã tick làm bằng chứng vải đúng vật lý.

## Trạng thái cuối phiên

Đã thu video, xem khung hình, xác minh hai repo và lập phương án. Chưa cài Spec Kit/Matt skills vào source, chưa chạy quy trình của hai bộ này trên FashionFit, chưa thay code game. Chỉ tạo tài liệu và dữ liệu xem video trong thư mục `Video_Reviews/2026-11-03`.
