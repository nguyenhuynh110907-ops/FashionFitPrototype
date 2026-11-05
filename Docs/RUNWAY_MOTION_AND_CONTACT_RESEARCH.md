# Chuyển động runway và tiêu chí không xuyên vải

Ngày: 2026-10-27. Phạm vi: nghiên cứu nguồn sơ cấp và rà mã hiện tại. Chưa huấn luyện AI, tải dataset, chạy lại mô phỏng đầy đủ hay tích hợp solver ngoài vào Unity trong lượt này.

## 1. Kết luận có thể áp dụng

Chuyển động vải cần phụ thuộc lịch sử vận tốc, gia tốc và tiếp xúc với cơ thể. Cùng một tư thế chân nhưng đang bước tới, vừa dừng hoặc vừa quay sẽ cho hình dạng váy khác nhau. Vì vậy chỉ gắn váy vào xương hay dùng một pha sin chung không thể tái tạo đầy đủ chuyển động này. Đây cũng là lý do nghiên cứu Motion Guided Deep Dynamic 3D Garments dùng trạng thái vải trước đó và quan hệ vải–cơ thể để dự đoán biến dạng [S2].

Yêu cầu “không xuyên” phải kiểm tra cả cơ thể–vải, vải–vải và vải–sàn theo đường chuyển động giữa các frame, với độ dày tiếp xúc được khai báo. Hiện tại dự án chưa đạt tiêu chí này. Năm lần lấy mẫu collider và tâm tam giác chỉ làm giảm bỏ sót, không phải bảo đảm va chạm liên tục.

## 2. Cách bước đi truyền sang trang phục

Bảng dưới là đặc tả hành vi đề xuất để dựng/chọn animation và hiệu chỉnh mô phỏng. Đây là suy luận thiết kế, chưa phải số đo thực nghiệm trên asset hiện tại. Cần dùng sự kiện tiếp đất của từng bàn chân thay vì đóng đinh mốc phần trăm cho mọi người mẫu.

| Giai đoạn | Cơ thể | Phản ứng vải cần tái tạo | Vùng dễ xuyên |
| --- | --- | --- | --- |
| Chân trước chạm đất | Trọng lượng bắt đầu chuyển sang chân trước; bàn chân chuyển vào pha trụ | Phần eo đi cùng thân; phần váy dưới tiếp tục chuyển động từ bước trước | Gấu gần bàn chân, đùi trước |
| Chân trụ nhận tải | Hông tiến qua chân trụ; gối điều chỉnh đỡ tải | Vải trượt theo tiếp xúc và ma sát, không khóa vào từng chân | Hai đùi và mặt trong váy |
| Chân sau đẩy đi | Gót nâng, bàn chân rời sàn | Vải quanh bắp chân được giải phóng rồi rơi; nếp căng thay đổi hướng | Gót và mặt sau gấu |
| Chân vung qua | Gối gập để tạo khoảng hở chân | Đùi/gối đẩy vải khi tiếp xúc; chuyển động truyền dần về gấu | Gối và khe hai chân |
| Chân vung duỗi ra | Chân chuẩn bị chạm đất kế tiếp | Mặt trước váy mở theo bước; mặt sau giữ trễ do quán tính | Đầu gối và mép xẻ |
| Quay người | Hông đổi hướng; vai và chân có chuyển tiếp | Gấu giữ hướng chuyển động cũ trong chốc lát, sau đó xoắn theo thân | Nếp gấp tự chạm, tay chạm eo |
| Giảm tốc và dừng | Hoàn thành bước cuối rồi chuyển sang tư thế trình diễn | Váy vượt nhẹ theo quán tính rồi tắt dần theo vật liệu/ma sát | Mặt trước–sau váy áp vào chân |

Váy ôm cần bước phù hợp độ mở hoặc đường xẻ. Nếu chuyển động chân đòi hỏi thể tích lớn hơn khoảng trống mà thiết kế cho phép, phải thay phom hoặc bước đi; tăng lực đẩy collider sẽ làm kéo giãn và rung.

Thân trên có cấu trúc được giữ bởi đường may/vai/eo. Phần rủ vẫn cần chuyển động riêng; không cố định toàn bộ lưng/ngực của mọi kiểu đầm. Gấu váy, tay áo và train có quán tính khác nhau, không nên dùng một damping chung để ép tất cả đứng yên.

## 3. Nguồn chuyển động thực nên dùng

Nghiên cứu runway năm 2024 phân tích video của 69 người mẫu và tìm được năm nhóm gait; mức bắt chéo chân giảm ở các show gần đây [S1]. Điều này hỗ trợ chọn kiểu bước tiết chế, nhưng không cung cấp một bộ góc xương duy nhất cho mọi chiếc đầm.

4D-DRESS của ETH Zürich có 64 outfit, 520 chuỗi chuyển động và 78 nghìn scan; gồm bốn đầm, mesh quần áo và body SMPL(-X) tương ứng [S3]. Đây là nguồn phù hợp để đo chuyển động tương đối và khoảng hở vải–cơ thể. Nó không phải một thư viện clip catwalk Unity có thể thả vào ngay. Chưa tải hay xử lý chuỗi scan trong lượt này.

Khi phân tích chuỗi thực, cần theo dõi:

- Thời điểm chân trái/phải tiếp đất, thời gian trụ và thời gian vung.
- Quỹ đạo bàn chân, đầu gối, hông và hướng vai.
- Độ dịch chuyển gấu sau khi loại bỏ chuyển động tịnh tiến/quay của cơ thể.
- Độ trễ giữa đỉnh vận tốc hông và đỉnh vận tốc gấu, tính theo frame và theo phần của một chu kỳ bước.
- Biên độ vung gấu chuẩn hóa theo chiều dài váy; thời gian tắt dao động khi dừng.
- Khoảng hở nhỏ nhất và vùng tiếp xúc tại ngực, hông, đùi, gối, mắt cá.

Không thể suy ra các đại lượng thời gian này từ một ảnh tĩnh. Video một góc còn bị che khuất bởi váy; pose 2D cần được retarget và hiệu chỉnh chân chạm sàn trước khi dùng cho mô phỏng 3D.

## 4. Những lỗi cụ thể thấy trong source

| Vị trí | Quan sát | Hệ quả |
| --- | --- | --- |
| `RunwayAvatar.Animate` | Hông, tay và chân phụ thuộc cùng sin; gối dùng nhánh Max thay chuyển tiếp liên tục | Thiếu phân pha nhận tải/đẩy chân/vung chân; cần kiểm tra giật tại chỗ đổi nhánh |
| `RunwayAvatar.Animate` | Bob hông là `.012*(1-cos(...))` | Khoảng biến thiên thực là 0–24 mm, không phải 12 mm như báo cáo trước ghi tắt |
| `RunwayAvatar.Animate` | Chưa có foot locking hoặc IK cho chân trụ | Có nguy cơ trượt hoặc nổi chân; dao động vải không khớp tiếp đất |
| `FittingCloth.Step` | Hai lần Step dùng cùng một cặp collider previous/current | Cả hai substep đều quét lại toàn bộ chuyển động thay vì hai nửa nối tiếp |
| `FittingCloth` và `FashionFitApp.ApplyPresentation` | Vải mô phỏng trong tọa độ mannequin, mannequin đổi transform toàn bộ | Tịnh tiến/quay bên ngoài chưa truyền đúng vận tốc/gia tốc tương đối vào vải |
| `FashionFitApp.Update` | Speed nhân vào thời gian tích lũy của cả motion và physics | Chỉnh nhịp đi cũng đổi tốc độ trọng lực/settle theo thời gian thực |
| `FashionFitApp.TogglePlay` | Cơ thể dừng ngay và vải chỉ được chạy thêm thời hạn cố định | Dừng không giống bước kết thúc; có thể đóng băng vải còn động |
| `FittingClothSolver.Step` | Điểm neo khối lượng nghịch đảo 0 được bỏ qua va chạm | Neo đặt trong cơ thể không được tự sửa; phải fit hợp lệ trước |
| `ProjectCollisions` | Proxy capsule/ellipsoid, không kiểm tra mesh cơ thể đang skin | Ngoài proxy vẫn có thể nằm trong tam giác cơ thể thật |
| `ProjectVirtualParticles` | Kiểm tra tâm tam giác luân phiên 2×2 | Có thể bỏ sót giao cắt ở cạnh/góc hoặc giữa các substep |
| `FittingCloth.BuildBodice` | Thân áo là skinned mesh riêng, không chạy bộ giải va chạm | Blend trọng số tốt hơn nhưng chưa chứng minh hết xuyên ngực/eo |

Smoke test 128 điểm chỉ đo điểm động nằm ngoài một ellipsoid. Nó không đo giao cắt tam giác với body render, tự va chạm hoặc xuyên trong khoảng thời gian giữa hai trạng thái. Ngưỡng giãn 1.65 trong fixture khởi tạo chồng collider là ngưỡng regression của fixture, không phải tiêu chuẩn độ giãn hợp lý của vải. Kết quả kiểm tra dài cũ không được dùng để chứng nhận solver sau thay đổi.

## 5. Cơ chế chống xuyên có cơ sở

C-IPC xử lý tiếp xúc của cấu trúc mỏng bằng rào cản khoảng cách, giới hạn strain và additive continuous collision detection (ACCD) để tìm giới hạn an toàn trước tiếp xúc [S4]. Mã nguồn chính thức `ipc-sim/Codim-IPC` có Apache-2.0 và hướng dẫn build Ubuntu/macOS [S5]. Đưa sang Unity Windows cần lớp tích hợp native và kiểm thử riêng.

OGC (SIGGRAPH 2025) xây hình học offset quanh mặt vải và giới hạn dịch chuyển từng đỉnh để giữ trạng thái không xuyên [S6]. Ví dụ tốc độ trên trang tác giả dùng RTX 4090; số đó không thể áp trực tiếp cho Radeon 860M của dự án. IPC Toolkit đã có triển khai OGC trong bản v1.5.0 [S7], phù hợp làm ứng viên thử nghiệm module tiếp xúc, chưa phải bằng chứng tích hợp Unity thành công.

Hai hướng này yêu cầu giữ toàn bộ điều kiện của thuật toán. Chỉ sao chép công thức lực đẩy, clamp điểm hoặc thêm vài collider không kế thừa bảo đảm của bài báo.

## 6. Kiến trúc đề xuất theo yêu cầu bắt buộc không xuyên

1. Fit trạng thái đầu không giao cắt, kiểm tra cả điểm neo và mép đường may. Pose/tay VR gây ràng buộc mâu thuẫn phải giới hạn trước khi solver nhận.
2. Cập nhật gait theo sự kiện chân, lấy pose body ở từng substep. Bước physics theo thời gian thực; Speed chỉ đổi tốc độ animation mục tiêu.
3. Truyền đúng vận tốc/quay của mannequin sang vải, dùng world space hoặc đổi cả trạng thái vị trí/vận tốc trong hệ quy chiếu chuyển động.
4. Dùng mesh physics thưa và broad phase BVH/spatial hash để tìm cặp có khả năng va chạm. Khi có đòi hỏi không xuyên, phải giữ kiểm tra liên tục toàn bộ cặp ứng viên liên quan, không bỏ xen kẽ theo frame.
5. Narrow phase phải xét đỉnh–mặt và cạnh–cạnh, gồm body–vải, vải–vải khác lớp và tự tiếp xúc của các phần không kề nhau. Dùng CCD an toàn hoặc đầy đủ displacement bounds của OGC.
6. Thử trạng thái body/vải mới trong bộ đệm riêng. Nếu không kiểm chứng được đường đi hợp lệ, giảm bước và thử lại; hết ngân sách thì giữ trạng thái hợp lệ gần nhất của cả body lẫn vải. Chỉ giữ vải mà tiếp tục cho body tiến vẫn có thể gây xuyên.
7. Mesh render chi tiết cần kiểm tra khoảng hở sau nội suy; mesh physics không xuyên không tự chứng minh mesh render không xuyên. Mọi blend giữa clip, mức chi tiết hoặc cache cũng phải qua cùng kiểm tra.
8. Chuyển sang nghỉ theo vận tốc/năng lượng và trạng thái tiếp xúc, đồng thời đánh thức khi body, tay VR hoặc collider đổi. Không ép nghỉ theo một thời hạn chung cho mọi chất liệu.

Để nhẹ khi trình diễn, có thể dùng clip vải đã mô phỏng và kiểm chứng trước; tương tác VR chạy phần động cục bộ. Cả cache nội suy lẫn đoạn nối sang physics vẫn phải qua kiểm tra đường chuyển động. Đây là đề xuất kiến trúc, chưa có số FPS chứng minh trên máy đích.

## 7. Tiêu chí nghiệm thu và ma trận thử

“Không xuyên” được định nghĩa là không có giao cắt cấm của mesh body/vải/sàn và giữ khoảng cách tiếp xúc đã khai báo trong miền chuyển động được hỗ trợ. Dung sai số được khai báo riêng để phân biệt tiếp xúc với xuyên; không dùng dung sai lớn để bỏ qua phần cơ thể lộ ra.

| Tình huống | Điều kiện thử đề xuất | Điều cần ghi |
| --- | --- | --- |
| Đi tại chỗ | Cả năm bộ; speed 0.25, 1.0, 1.5; tối thiểu 100 chu kỳ | Giao cắt, khoảng hở, độ giãn, chân trụ |
| Đi có root motion | Tới, lùi, ngang; tăng và giảm tốc | Độ trễ gấu và quán tính khi đổi hướng |
| Dừng | Dừng ở mọi pha; chuyển sang pose kết thúc | Không đóng băng khi chưa settle |
| Quay | 90°/180° hai hướng; quay liên tục | Tự giao cắt và tiếp xúc tay–eo |
| Kéo VR | Kéo ra, ép vào body, kéo xuyên qua phía bên kia, thả đột ngột | Giới hạn tay, đường đi hợp lệ, giãn đường may |
| Di chuyển mannequin | Nâng, xoay, thả xuống | Quán tính, contact sàn, trạng thái nghỉ |
| Frame chậm | Chèn gián đoạn 50/100/200 ms và thử ở 30/60/72/90 Hz render | Không bỏ lọt va chạm; số lần giữ trạng thái an toàn |
| Đổi trang phục/LOD | Đổi ở lúc đi và lúc quay | Neo/body/render đồng bộ, không giao cắt khi chuyển |

Điều kiện đạt: số giao cắt cấm bằng 0 theo phép kiểm tra độc lập, bao gồm đường đi giữa các trạng thái; không NaN; độ giãn trong giới hạn riêng từng vật liệu; thông số đẹp/settle đối chiếu chuỗi tham chiếu; frame time p50/p95/p99 đo trên máy và kính đích. 72 Hz có tổng ngân sách khoảng 13.89 ms/frame, 90 Hz khoảng 11.11 ms/frame; đây là ngân sách toàn ứng dụng, không phải riêng cloth. Chưa chạy các bài nghiệm thu này.

## Nguồn sơ cấp

- [S1 — Runway gait, Sensors 2024](https://www.mdpi.com/1424-8220/24/12/3865). Một số đường dẫn toàn văn trả lỗi truy cập; chỉ sử dụng kết luận/chi tiết đọc được từ kết quả chỉ mục, không trích góc xương chưa đo.
- [S2 — Motion Guided Deep Dynamic 3D Garments](https://arxiv.org/abs/2209.11449).
- [S3 — ETH Zürich 4D-DRESS](https://eth-ait.github.io/4d-dress/) và [repository chính thức](https://github.com/eth-ait/4d-dress).
- [S4 — C-IPC, tác giả/SIGGRAPH 2021](https://ipc-sim.github.io/C-IPC/).
- [S5 — C-IPC code chính thức](https://github.com/ipc-sim/Codim-IPC).
- [S6 — OGC, Utah Graphics/SIGGRAPH 2025](https://graphics.cs.utah.edu/research/projects/ogc/).
- [S7 — IPC Toolkit v1.5.0, triển khai OGC](https://github.com/ipc-sim/ipc-toolkit/discussions/211).
