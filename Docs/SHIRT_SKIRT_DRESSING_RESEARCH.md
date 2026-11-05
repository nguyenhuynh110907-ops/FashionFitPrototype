**Nghiên cứu cách mặc áo thun, váy và cấu tạo cổ áo — 3/11/2026**

Ảnh được gửi cho thấy mép áo phía sau đang dựng và hở quanh gáy. Áo đang được chọn là bản tham chiếu Gucci 864361, có cổ bẻ. Đợt này đã đọc tài liệu may, bài nghiên cứu, README, phần cài đặt, giấy phép mã và các tệp code của bốn repo. Trạng thái là nghiên cứu và rà code. Các repo chưa được chạy thử trên máy này, lỗi cổ trong EXE hiện tại còn cần sửa và nghiệm thu riêng.

Với áo thun cổ tròn, bo cổ là một mảnh có chiều dài nghỉ, độ rộng và khả năng hồi phục riêng. Tài liệu University of Fashion hướng dẫn thiết kế độ rộng bo cùng độ giãn cần để qua đầu. Bo rộng và ít giãn sẽ khó nằm phẳng. Các thông số cần xuất phát từ loại cổ và mẫu vải cụ thể. [University of Fashion](https://www.universityoffashion.com/lessons/drafting-a-crew-neckline-for-knits/).

In the Folds hướng dẫn đo đường may cổ trước và sau riêng, rồi chia chiều dài bo theo tỉ lệ. Họ đưa mức khởi đầu khoảng 80% cho bo cùng vải jersey và 70–75% cho rib, sau đó may thử để điều chỉnh. Trong mô hình vật lý, nếu L0 là chiều dài nghỉ của bo và Ls là chiều dài đường ráp thì độ giãn khi ráp là Ls/L0 − 1. Ví dụ tỉ lệ 0,8 tương ứng giãn 25% lúc ráp. Đây là phép tính hình học, còn lực và hồi phục cần dữ liệu vải. [In the Folds](https://inthefolds.com/q-a-series/2023/tips-for-sewing-knit-necklines).

Phân biệt này có ích cho ảnh hiện tại. Cổ bo áo thun, cổ bẻ polo và cổ đứng cần mô tả khác nhau về đường ráp, đường gập và độ cứng uốn. Với polo trong game, cần kiểm tra mép chân cổ có ráp đúng vòng cổ thân áo và nếp gập có giữ phom dự kiến khi vai hoặc đầu chuyển động. Đây là hướng suy luận kỹ thuật từ cấu tạo cổ, cần đo trên mesh đang render để xác nhận.

Với váy thẳng, bài draping của Cornell theo dõi đường giữa trước/sau, đường ngang hông, độ dư, chiết và đường sườn. Bước cuối kiểm tra đường sườn rủ thẳng sau khi tháo phần lớn ghim. Điều này gợi ý kiểm tra phom bằng các đường cân bằng và điểm tựa eo/hông. Độ xòe và lớp lót cần giữ cấu trúc riêng theo thiết kế. [Cornell University](https://fit.cit.cornell.edu/textiles/draping/drapes/straightskirt/steps/print.html).

Đối với thao tác mặc thực sự, nghiên cứu Animating Human Dressing dùng các thao tác căn lỗ mở, đưa chi qua lỗ rồi theo chuyển động, kết hợp điều khiển và mô phỏng vải. Áp dụng vào project cần đánh dấu vòng cổ, cửa tay, gấu, vòng eo và khóa/nút. Các chuỗi dưới đây là đề xuất cho bản thử, chưa phải kết quả mô phỏng đã chạy. [Animating Human Dressing](https://web.engr.oregonstate.edu/~mjb/cs550/Projects/Papers/CAnimatingHumanDressing.pdf).

| Trường hợp | Chuỗi thao tác cần kiểm tra trong bản thử |
|---|---|
| Áo thun chui đầu | Căn đúng vòng cổ, đưa đầu và hai tay qua đúng cửa theo thứ tự đã chọn, hạ gấu, thả Grip để vải ổn định trên vai và thân |
| Kéo/cởi áo | Điểm Grip giữ tại vị trí tay, vải tiếp xúc đầu/tay khi bị vướng, lực đàn hồi tác động lên biến dạng vật liệu, trọng lực tiếp tục kéo phần tự do |
| Váy có khóa hoặc nút | Mở liên kết đóng, đưa qua chân/hông, căn cạp, đóng liên kết rồi kiểm tra tiếp xúc và độ rủ |
| Váy co giãn hoặc đầm chui | Kiểm tra lỗ mở đủ đi qua vùng cơ thể và giới hạn giãn, sau đó để vải hồi phục trong điều kiện tiếp xúc thực tế |

Các repo đã tham khảo được đối chiếu theo phần có thể học và phần còn phải kiểm định khi áp dụng vào Unity.

| Repo | Phần hữu ích cho FashionFit | Điều kiện áp dụng |
|---|---|---|
| GarmentCode | Có các phần thân trước/sau, cổ, tay, cạp, váy và quan hệ may. Code cổ có tham số trước/sau riêng. Phù hợp tham khảo cấu trúc cổ và đường ráp. | Python, PyGarment, mô phỏng dùng fork NVIDIA Warp. Cần kiểm định chuyển dữ liệu sang Unity. [Code cổ](https://github.com/maria-korosteleva/GarmentCode/blob/main/assets/garment_programs/collars.py), [thân áo](https://github.com/maria-korosteleva/GarmentCode/blob/main/assets/garment_programs/tee.py), [cạp](https://github.com/maria-korosteleva/GarmentCode/blob/main/assets/garment_programs/bands.py), [cài đặt](https://github.com/maria-korosteleva/GarmentCode/blob/main/docs/Installation.md). |
| HOOD | Mô phỏng động vải bằng GNN, có vật liệu theo vùng và thay đổi liên kết. Hữu ích để đối chiếu áo ôm, áo rộng và chuyển động khi mở liên kết. | Hướng cài công bố dùng PyTorch/CUDA. Cần đánh giá với body riêng và dữ liệu chuyển động của project. [Phương pháp](https://dolorousrtur.github.io/hood/), [README](https://github.com/Dolorousrtur/HOOD/blob/main/README.md). |
| ContourCraft | Thêm xử lý giao xuyên giữa các lớp vải dựa trên intersection contours và repulsion. Phù hợp nghiên cứu lớp đầm/lót và các lớp áo dài. | Hướng cài có CUDA 12.4 và CCCollisions. README còn để TODO tự động xuất Alembic và remesh tùy độ phân giải. [Phương pháp](https://dolorousrtur.github.io/contourcraft/), [README](https://github.com/Dolorousrtur/ContourCraft/blob/main/README.md), [cài đặt](https://github.com/Dolorousrtur/ContourCraft/blob/main/INSTALL.md). |
| Assistive Gym | Môi trường mặc áo bệnh viện có neo vải theo tay cầm, theo dõi lực tiếp xúc và cho vải ổn định. Hữu ích tham khảo logic đưa tay qua cửa áo và giữ vải. | Code dùng API vải của PyBullet và phụ thuộc cũ. Các giá trị neo, margin và trọng lực trong benchmark cần được đánh giá lại cho trang phục thời trang. [Dressing environment](https://github.com/Healthcare-Robotics/assistive-gym/blob/main/assistive_gym/envs/dressing.py), [README](https://github.com/Healthcare-Robotics/assistive-gym/blob/main/README.md). |

Giấy phép mã đã đọc của bốn repo là MIT. Khi tái sử dụng cần giữ thông báo tương ứng. Dữ liệu, checkpoint và body SMPL/SMPL-X mà các repo dẫn tới cần kiểm tra điều khoản riêng trước khi chọn nguồn đưa vào sản phẩm. [GarmentCode LICENSE](https://github.com/maria-korosteleva/GarmentCode/blob/main/LICENSE), [HOOD LICENSE](https://github.com/Dolorousrtur/HOOD/blob/main/LICENSE), [ContourCraft LICENSE](https://github.com/Dolorousrtur/ContourCraft/blob/main/LICENSE), [Assistive Gym LICENSE](https://github.com/Healthcare-Robotics/assistive-gym/blob/main/LICENSE).

Máy trong bản ghi kiểm tra hiện có dùng AMD Radeon 860M. Các hướng cài HOOD/ContourCraft vừa đọc dựa vào CUDA, nên tính tương thích và hiệu năng trên máy này còn chưa được đánh giá. Có thể dùng các repo để xây dựng bài đối chiếu offline trên môi trường phù hợp trước khi quyết định tích hợp runtime.

Kết quả rà code cục bộ cần được phân biệt với nguyên nhân đã đo bằng thực nghiệm.

| Đường code hiện tại | Quan sát xác nhận được | Việc cần đo tiếp |
|---|---|---|
| TailoredPoloStudy.BuildUpper / BuildCollar | Thân và chi tiết cổ được dựng thành hai mesh riêng. Cổ dùng các mức cao cố định và profile thân. Có đường apply cache chỉnh tọa độ sau dựng. | Xuất mesh render sau cache và sau settle, đo khoảng cách giữa vòng cổ thân áo và chân cổ quanh gáy, kiểm tra biến đổi theo xương. |
| TailoredSurfaceDynamics | Phần động là chuyển vị phụ có các vùng ghim, chi tiết cổ/nẹp dựa vào bề mặt tham chiếu. | Xác định phần dựng cổ đến từ phom/cache hay từ trạng thái mô phỏng và Grip. |
| ContinuousShirtShell / GarmentCloseFit | Các áo khác dùng đường cắt theo cao độ, giữ một số mép khi làm mượt và có bước ProjectOutside với margin 22 mm. | Kiểm tra riêng khoảng hở cổ theo vùng. Đây là nhánh khác với bộ polo đang chọn trong ảnh. |

Từ các quan sát này, hướng sửa có cơ sở là mô tả vòng cổ và đường ráp trước/sau trên mesh, rồi nối chân cổ vào đúng vòng đó. Cổ bẻ cần thêm đường gập và độ cứng uốn. Với áo thun, cần chiều dài nghỉ và đặc tính hồi phục của bo. Các tham số này phải gắn với loại cổ và vật liệu.

Để xử lý theo nhóm trang phục, bước tiếp theo nên bổ sung metadata về NeckOpening, CollarSeam, SleeveOpenings, WaistOpening, các lớp và khóa/nút. Mesh 3D bài nộp sẽ cung cấp phom gốc cùng những nhãn này. Thao tác mặc/cởi cần giải các liên kết may, Grip, tiếp xúc và trọng lực theo cùng một cơ chế.

Ưu tiên triển khai được đề xuất theo thứ tự sau.

1. Tái hiện cổ sau của mannequin nữ chính đang mặc Gucci study. Chụp trước/sau/nghiêng, xuất mesh đang render và đo mép ráp. Phải xác nhận đường code và trạng thái cache đang hoạt động.
2. Làm một bộ dữ liệu thử nhỏ có cổ bo thun, cổ bẻ polo, váy thẳng và đầm xòe. Ghi rõ đường ráp, lỗ mở, lớp và đặc tính vật liệu dùng trong phép thử.
3. Áp dụng cấu trúc đường ráp có tham khảo GarmentCode. Dùng kết quả động từ HOOD/ContourCraft làm bài đối chiếu ở môi trường phù hợp, đồng thời học cách tổ chức thao tác mặc từ nghiên cứu dressing và Assistive Gym.
4. Kiểm tra ở tư thế trung tính, quay/cúi đầu, nhấc tay, đi chậm và kéo hai tay. Theo dõi khe ráp, vùng cổ dựng, độ giãn, giao xuyên giữa lớp và thời gian xử lý. Mức chấp nhận khe hở cần theo kiểu cổ và thiết kế tham chiếu.

Các tệp đã đọc có Git blob SHA và các tệp cục bộ có hash trong [SOURCES.json](../Verification/DressingResearch/SOURCES.json). Báo cáo này cung cấp căn cứ chọn hướng sửa. Chưa có kết quả chạy repo, benchmark VR hoặc bản EXE sửa cổ mới trong đợt nghiên cứu này.
