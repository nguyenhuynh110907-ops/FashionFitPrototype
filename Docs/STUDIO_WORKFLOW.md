# FashionFit Studio — V092

[Nhận bài trang phục 3D theo body mẫu](STUDENT_GARMENT_GUIDE.md). Bộ full có thêm kiểm tra bài nộp 3D và tiếp xúc độc lập, tổng 20 bước. [Báo cáo](../Verification/StudentSubmission/REPORT.md).

[Sửa phom áo và ống quần V092](CLOSE_FIT_GUIDE.md). Bộ full có thêm hai bước kiểm tra phom, tổng 18 bước. [Ảnh và số đo](../Verification/CloseFit/REPORT.md).

[Grip khớp và thử mặc/cởi V091](CONTACT_DRESSING_GUIDE.md). Bộ full thêm 68 kiểm tra cho 34 khớp, tổng 16 bước. Bài vật lý tự do có báo cáo riêng và hiện còn INCOMPLETE.

[Tối ưu phản hồi và dọn tệp V090](RESPONSE_OPTIMIZATION_GUIDE.md). Các báo cáo V089 được giữ làm lịch sử.

Đây là bản áp dụng tinh gọn từ [Claude Code Game Studios](https://github.com/Donchitos/Claude-Code-Game-Studios/tree/05ad7c8a5b8aa2aa86bbd3439ae7cf7295cac418): **6 vai trò, 3 quy trình** viết riêng cho FashionFit. Không cài toàn bộ studio, không tự chạy 49 agent. Attribution và MIT ở `THIRD_PARTY/CCGS/`.

## Bạn mở gì?

- Chơi bình thường: **Desktop → FashionFit VR → 01 - VAO GAME VR**.
- Ghi một phiên trải nghiệm: **04 - GHI HIEU NANG VR** trong cùng thư mục, hoặc `Record_VR_Performance.bat` ở gốc project.
- Kiểm tra tự động: `Verify_FashionFit.bat` ở gốc project. Việc này cần Unity Editor đã đóng ở project được kiểm tra; mất vài phút và sẽ hiện cửa sổ thử Ivory/nón đỏ ngắn.
- Xem báo cáo lần tích hợp: [Verification/StudioIntegration/REPORT.md](../Verification/StudioIntegration/REPORT.md).

Source vẫn ở `C:\Users\HUYNH\OneDrive\Desktop\FashionFitPrototype`. Game mới nằm ở `C:\Users\HUYNH\Downloads\FashionFit_Run\Versions\V092_CloseFit\FashionFittingRoom.exe`. Các bản V089/V088 được giữ lại.

## Ghi thử bằng Quest Link và Touch thật

1. Cắm cáp, mở Meta Horizon Link trên PC, vào Quest Link trong kính. Tay cầm phải hoạt động trong môi trường Link trước.
2. Thoát game FashionFit cũ. Mở **04 - GHI HIEU NANG VR**. Công cụ sẽ từ chối mở thêm nếu game cũ còn chạy.
3. Trong game: **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**. Công cụ ghi không tự bấm nút hoặc tiêm chuyển động tay.
4. Thử khoảng 10 phút: đi quanh phòng; kéo áo Ivory ở chế độ **VẢI**; đổi sang **POSE** để chỉnh khớp; cầm nón đỏ bằng một tay rồi hai tay, kéo/nén, đội lên đầu, Grip thường nhấc ra và thả.
5. Có thể mở bảng số **Y → ĐÍCH MẶC / VẬT LIỆU → Căng / nén vật liệu → Xem rõ thông số**.
6. Công cụ tự kết thúc ghi sau 600 giây, game vẫn chạy. Thoát game bình thường nếu muốn lưu một phiên ngắn hơn; xem `durationCompleted=false`. Không End task nếu cần giữ số liệu.

Kết quả nằm ngoài OneDrive: `%LOCALAPPDATA%\FashionFit\Performance\<UTC timestamp>\`.
Mỗi lần có `frames.csv`, `performance.json` và `Player.log`. Năm giây đầu sau Ready là warmup, không đưa vào thống kê. Thời gian chuẩn bị/kết nối trước khi vào VR vẫn được ghi ở nhóm không có XR; cần lọc cột XR để đánh giá phần dùng kính.

## Đọc đúng số hiệu năng

| Trường | Ý nghĩa |
|---|---|
| `frames`, `capturedSeconds` | Số lượt cập nhật và thời gian thực sự thu thập; không phải thời lượng người dùng đã thao tác đủ bài |
| `renderedFrames` | Lượt có callback camera chính vẽ ở khung trước; giúp phát hiện cửa sổ bị ẩn/minimize không còn render |
| `wallFrameMs`, `renderedWallFrameMs` | Khoảng thời gian giữa các lượt cập nhật; gồm chờ/vSync/pacing, không chỉ chi phí solver |
| `cpuFrameIncludingWaitMs` | CPU frame timing của Unity, bao gồm thời gian chờ |
| `cpuMainThreadMs`, `gpuFrameMs` | Bộ đếm nền tảng nếu có; dữ liệu có độ trễ pipeline |
| `gcAllocatedBytes` | Bộ đếm GC nếu build/platform cung cấp. `NOT_ASSESSED` nghĩa chưa đo được; không có nghĩa không cấp phát |
| `uniquePoseSolveMs` / `uniquePosePublishMs` | Chi phí giải / xuất mesh, lấy một mẫu mỗi lượt solver hoàn tất |
| `lastPoseStepMs` | Giá trị lần chạy solver pose nữ gần nhất. Có thể lặp qua nhiều frame; không cộng thành tổng chi phí của mỗi frame |
| `xrActiveFrames`, các cột tracked | Quan sát XR/tracking thật từ thiết bị. Có tracking chưa chứng minh thao tác đã đúng hoặc thoải mái |
| `physicalInteractionVerified` | Luôn false trong báo cáo máy; chỉ người thử mới xác nhận được trải nghiệm |
| `durationCompleted`, `truncated` | Đã ghi đủ thời gian yêu cầu hay chạm dung lượng buffer |

Các metric có p50/p95/p99, trung bình và cực đại. Phải đọc `status` và `samples` trước các con số. Một metric không có dữ liệu vẫn có các trường số bằng 0 do JSON của Unity; đó **không phải số đo 0 ms**. CSV dùng -1 khi chưa lấy được giá trị; một số API nền tảng cũng trả 0 khi không hỗ trợ.

Mục tiêu đề xuất là **72 Hz, khoảng 13,89 ms/frame**. Bộ ghi không đổi refresh rate của kính, vSync, chất lượng hoặc tốc độ solver. App timing không đo được độ trễ toàn chuỗi Quest Link, ASW/reprojection hoặc số khung bị compositor bỏ. Chưa dùng nó để chứng nhận 72/90 Hz.

`python Tools/Studio/analyze_capture.py <thư_mục_capture>` kiểm tra chéo CSV/JSON và thống kê theo giai đoạn. Một bản ghi hợp lệ chưa có nghĩa đạt yêu cầu hiệu năng.

## Chạy kiểm tra chung

```bat
Verify_FashionFit.bat --suite trial
Verify_FashionFit.bat --suite full
```

`trial`: thêm 660 đối chiếu tìm grip với thuật toán gốc, 168 kiểm tra chỉ số vật liệu native ở 24 trạng thái; cấu hình VR 37 mục; hai tay 91; build Windows; surface/readout 28 và 8 trạng thái tiếp xúc độc lập; pose/nón 29; bài Ivory/nón đỏ chạy theo frame tự động 19 mục và capture.

`full`: thêm 5 bộ nam 55 mục/20 trạng thái body contact, áo dài cập nhật tự động 18 mục/5 trạng thái lớp. Đây là số tối thiểu của bộ hiện tại, không phải chỉ tiêu hình thức để tăng số lượng test.

Các bài cũ xuất file trung gian trong `Verification/` của Unity project; runner yêu cầu file mới hơn thời điểm chạy và chép JSON vào thư mục run mới. Không dùng file xanh cũ để che lần chạy lỗi. Dữ liệu raw `.bin` chỉ phục vụ kiểm tra độc lập, không phải runtime assets.

Mặc định output ở `%LOCALAPPDATA%\FashionFit\StudioRuns\<UTC timestamp>\`. Runner tạo một player riêng, không tự thay bản game bạn đang dùng. Có thể chỉ định:

```bat
Verify_FashionFit.bat --suite full --unity-project "D:\FashionFit_Test" --output "D:\FashionFit_QA" --qa-deps "D:\FashionFit_QA_Libs"
```

Project thử phải có đúng source/resources đã rà soát. Unity phải đúng 2022.3.62f1. Cần Python 3 và các thư viện độc lập numpy, trimesh, libigl, embreex. Máy hiện tại dùng bộ thư viện QA đã có; vị trí ở `Tools/Studio/studio.local.json`, được gitignore. Chuyển máy cần thiết lập lại. Công cụ không tự tải hoặc cài dependency qua mạng.

Trạng thái: `RUNNING` đang làm; `passed` đạt phạm vi tự động; `failed` có lỗi; `INCOMPLETE` thiếu điều kiện đánh giá. Thử kính thật, hiệu chuẩn vật liệu và chứng nhận va chạm liên tục vẫn được ghi **NOT_ASSESSED** riêng.

## Vai trò và quy trình

`AGENTS.md` dùng cho agent làm việc trong repo. `CLAUDE.md`, `.claude/agents/`, `.claude/skills/` là phần tùy chọn cho Claude Code. Công cụ kiểm tra Python/BAT không cần tài khoản Claude.

Sáu vai trò: Unity interaction, technical art, vật liệu, hiệu năng, QA, release. Mặc định xử lý tuần tự. Ba lệnh Claude: `/fashionfit-verify`, `/fashionfit-performance`, `/fashionfit-review`. Không nhập vào đây hooks Bash, gán model trả phí hoặc quy tắc bắt đổi pipeline/Addressables từ template gốc.

Quy trình một lỗi: mô tả thao tác → tái hiện → sửa nhỏ → chạy bài liên quan → kiểm tra ảnh và số đo → build cạnh bản cũ → báo cáo phạm vi đã thử. Chỉ đưa lên GitHub khi bạn yêu cầu.

## Vật liệu và thẩm mỹ còn cần phát triển

Các giới hạn Cotton 5 cm, Stretch 10 cm vẫn là **giới hạn lệnh tương tác minh họa**, không phải thuộc tính chung của mọi loại cotton/vải. E, lực ảo, hồi phục và ngưỡng strain chưa fit dữ liệu đo. Bộ studio không huấn luyện AI, không suy sức bền từ texture và không thay thế mô hình cơ học.

V090 thêm cache tiếp xúc trong một lượt giải, bộ lọc tìm grip và tính chỉ số vật liệu native; không giảm mật độ lưới hoặc số vòng tiếp xúc. Khi đang giữ áo/khớp, mô phỏng cập nhật theo frame render. Các trường `uniquePoseSolveMs` / `uniquePosePublishMs` lấy một mẫu mỗi lượt giải hoàn tất, tách khỏi `lastPoseStepMs` có thể lặp qua nhiều frame. Điểm còn lại của hình học áo/nón/tủ dựa trên V088. Chưa thay toàn bộ trang phục bằng rập may thật hay mô hình FEM mới. Hướng tiếp theo nên dựa vào đoạn ghi Quest Link thật: ưu tiên phần gây trễ đã đo, kiểm tra strain cực đại ở điểm kéo và so sánh hình dáng với mẫu có kích thước/đặc tính rõ ràng.

API đo được đối chiếu với [FrameTimingManager Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTimingManager.html), [FrameTiming](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTiming.html), [ProfilerRecorder](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html) và XML đi kèm Editor đã cài.
