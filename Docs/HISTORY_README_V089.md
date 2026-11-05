# README trước khi làm gọn — V089

Lưu đầy đủ hướng dẫn/nghiên cứu cũ; đường dẫn tương đối được điều chỉnh theo vị trí trong Docs. Điểm bắt đầu hiện tại: [README](../README.md).

Bản mới V089: [quy trình Studio, kiểm tra chung và ghi hiệu năng Quest Link](../Docs/STUDIO_WORKFLOW.md).

Bản mới V088: [sửa mép áo/nón/chân tủ và xem rõ thông số kéo giãn](../Docs/SURFACE_READOUT_VR_GUIDE.md).

Bản mới V087: [5 bộ Hollister nam tham chiếu, gồm các sửa pose/nón V086](../Docs/HOLLISTER_MENS_VR_GUIDE.md).

Cập nhật V086: [Pose, nhấc nón bằng Grip thường, kéo/nén hai tay](../Docs/POSE_BALANCE_VR_GUIDE.md).

[Mới: áo dài có tà/lớp riêng, sửa Grip áo và nón đang đội](../Docs/LAYERED_AODAI_VR_GUIDE.md).

[Mới: mannequin pose mặc đồ, đội nón và kéo/nén vật liệu bằng hai tay VR](../Docs/POSE_MATERIAL_VR_GUIDE.md).

BẮT ĐẦU: mở thư mục **FashionFit VR** trên Desktop → **01 - VAO GAME VR**. [Mục lục tài liệu](../Docs/INDEX.md). Công cụ thu thập/huấn luyện nằm trong `Tools/Launchers`.

[Mới: mở nút và cởi blouse bằng hai tay VR](../Docs/VR_BUTTON_BLOUSE_GUIDE.md).

[Cập nhật hai tay cùng giữ/kéo vải, bệ pose và nhịp đi bộ mới](../Docs/VR_TWO_HANDS_GUIDE.md).

VR hai tay và Grip gần: [hướng dẫn dùng](../Docs/VR_TWO_HANDS_GUIDE.md). Texture web: mở `Open_Texture_Web.bat`.

Mở nhanh mô hình ngoài OneDrive: **Open_FashionFit_Model.bat**. [Hướng dẫn và lỗi đã sửa](../Docs/MO_MO_HINH.md).

Dữ liệu texture và cơ học vải mới: [nguồn tải, kiểm tra nhãn và cách dùng](../AI/MaterialLab/README.md). Giữ ngân sách dữ liệu dưới 30 GB.

Chuyển trang phục 3D có sẵn (FBX/GLB/OBJ): [công cụ, body mẫu và kết quả kiểm tra](../Docs/GARMENT_3D_TRANSFER_GUIDE.md). Baseline hình học đã chạy; chưa phải mạng AI mới huấn luyện.

Vào lại VR: [mở phiên thử Quest Link và làm ba bước](../Docs/VR_REENTRY_GUIDE.md).

Đã kiểm tra đứng yên rồi tăng nhịp 0,35× / 0,50×: [cách thử và phạm vi đo](../Docs/SPEED_CONTACT_GUIDE.md).

Hai model nam/nữ và Grip chỉnh khớp: [hướng dẫn Pose Studio](../Docs/POSE_STUDIO_GUIDE.md), [nguồn GitHub và báo cáo](../Verification/PoseStudio/REPORT.md).

Tủ và menu VR mới: [cách thử trên desktop/kính Meta](../Docs/ATELIER_VR_GUIDE.md), [ảnh và báo cáo rà soát](../Verification/AtelierPolish/REPORT.md). Y trái mở/ẩn menu, X trái đưa về trước.

Di chuyển tự do trong kính Meta: [cần trái đi, cần phải quay và room-scale](../Docs/VR_FREE_MOVEMENT.md), [kết quả kiểm tra](../Verification/VRLocomotion/REPORT.md).

Bộ nón theo ảnh mới: [10 mẫu, ảnh từng nón và kết quả kiểm thử](../Verification/ReferenceHats/REPORT.md). Kết nối kính: [Quest Link từng bước, chia rõ PC/kính/Unity](../Docs/KET_NOI_QUEST_LINK.md).

Bàn nón và tủ đồ VR: [bắt đầu với Meta Quest Link, Grip và giả lập phím E](../Docs/VR_SHOWROOM_QUICK_START.md), [ảnh và kết quả kiểm thử bản đầu](../Verification/Showroom/REPORT.md). Trong Game mở **VR / CAMERA → Xem bàn nón và tủ đồ**.

Cập nhật mới: [lụa/cotton/len, chuyển động vải và cách thử](../Docs/MATERIAL_PHYSICS_GUIDE.md), [kết quả đo hiệu năng/tiếp xúc](../Verification/MaterialMotion/REPORT.md), [sửa nách mẫu 06](../Verification/UnderarmFix/REPORT.md). Đây là hiệu chỉnh vật lý có tham khảo số đo, không phải AI mới được huấn luyện.

Cập nhật phom và bề mặt: [sửa nách, cổ, eo và vải dệt; ảnh trước–sau](../Verification/RealismPass/REPORT.md).

Cập nhật hình thể: [mannequin tròn/mềm hơn, áo ôm vừa và nút xem phom](../Verification/BodySilhouette/REPORT.md).

Sửa mới: [phom ngực áo tròn hơn, chuyển tiếp mềm và căn lại nẹp/nút](../Verification/PoloRoundness/REPORT.md).

Thử mới: [đầm tham chiếu Gucci 864361, cổ áo/nẹp/nút 3D và váy kín](../Docs/GUCCI_DETAIL_TRIAL.md). Mẫu 06 được chọn sẵn khi bấm Play.

# Fashion Unity Fitting Room v0.8.3 Detail Study

Bản dùng thử mới: [mẫu vải PBR và AI nhận diện hoa văn trong game](../Docs/FABRIC_TRIAL_GUIDE.md). Vào Play → VẢI / AI để kiểm tra.

Sửa góc nhìn gần: [báo cáo lỗi mất thân đầm / áo Ivory](../Verification/CLOSE_VIEW_FIX_REPORT.md). Đã sửa bounds theo xương gốc, tắt che da mặc định và chỉnh cổ/lỗ tay áo; Stop rồi Play lại sau khi compile. Chuẩn bị học chi tiết đầm: [SurfaceDetails](../AI/GarmentDetail/SURFACE_DETAIL_DATA.md).

Ưu tiên chống xuyên: [hướng dẫn đứng yên / trình diễn chậm](../Docs/SLOW_PRESENTATION_GUIDE.md). Khởi động mặc định đứng yên; bấm **Trình diễn chậm · bước ngắn 0,18×** để xem chuyển động. Đã bổ sung bao tiếp xúc eo–hông/thân áo, xử lý va chạm điểm neo và sửa mép nách ba thân đầm. Dừng Play rồi Play lại để nhận mặc định mới.

Học chi tiết trang phục theo rập: [GarmentDetail · cách dùng](../AI/GarmentDetail/README.md), [kết quả pilot và giới hạn](../AI/GarmentDetail/TRAINING_PREPARATION_REPORT.md). Đã chuẩn bị 6.304 ứng viên có body mesh, chuyển đổi 24 graph và train thử 8 epoch; chưa áp trọng số vào game. Trong Unity chọn `Fashion Fit > AI > Inspect Detailed Garment` để xem từng mảnh rập, đường may và đối chiếu mesh trước/sau drape.

Dữ liệu học 3D/tiếp xúc mới: [ClothData · kết quả thu thập](../AI/ClothData/COLLECTION_REPORT.md), [bộ đọc và cách dùng](../AI/ClothData/README.md). Dữ liệu được lưu ngoài `Assets`, hạn mức đợt đầu 30 GB; chưa thay mô hình vật lý trong game.

AI thử nghiệm đã huấn luyện: [FitVision · cách dùng](../AI/FitVision/README.md), [kết quả và giới hạn](../AI/FitVision/TRAINING_REPORT.md). Trong Unity mở `Fashion Fit > AI > Analyze Garment Photo`, hoặc chạy `Analyze_Garment_AI.bat`. Mô hình hiện đạt 60% nhận diện loại đồ và 41,2% nhóm phom trên tập test nhỏ; chưa tự chỉnh mesh hoặc mô phỏng vải.

Nghiên cứu mới: [100 ảnh người mặc trang phục và kết quả phân tích](../References/ModelWear100/REPORT.md). Mở [thư viện ảnh ngoại tuyến](../References/ModelWear100/index.html) bằng Chrome/Edge để lọc đầm, chân váy, áo, quần và xem ghi chú từng ảnh.

Prototype phòng thử đồ Unity 2022.3.62f1 với mô phỏng vải PBD, điều khiển desktop/PCVR, kéo-thả váy hoặc ma-nơ-canh và AI Fabric Studio dùng Gemini/Nano Banana.

Bản fit trang phục mới: [BODY_FIT_GUIDE.md](../Docs/BODY_FIT_GUIDE.md). Bản này dùng 38 tiết diện đo từ mesh kỹ thuật để phân biệt phom ôm/xòe/rủ, sửa đường eo và thân áo. Dáng đi vẫn dùng đường cong pha chân và IK của [Runway Trial](../Docs/RUNWAY_TRIAL_GUIDE.md), chưa phải clip mocap hay mô hình AI đã huấn luyện. Các phương pháp C-IPC/OGC trong báo cáo nghiên cứu chưa được tích hợp đầy đủ.

## Mở dự án

1. Đóng Unity Editor cũ nếu đang mở bản khác.
2. Nhấp đúp `Open_FashionFit.bat`.
3. Chờ Unity import/compile. Lần đầu có thể mất vài phút.
4. Nếu Game không tự chạy, chọn menu `Fashion Fit > Open and Run Prototype`.

Cách khác: Unity Hub > Add project from disk > chọn chính thư mục `FashionFitPrototype`, mở bằng Unity `2022.3.62f1`.

## Giao diện mới: bắt đầu trong 1 phút

1. Mở tab `BẮT ĐẦU` và bấm `1 · Chọn trang phục`.
2. Trong tab `TRANG PHỤC`, chọn một trong năm mẫu; dấu `✓` cho biết mẫu đang mặc.
3. Quay lại `BẮT ĐẦU`, bấm **Trình diễn chậm · bước ngắn 0,18×** để xem vải chuyển động và bấm dừng để vải rơi/ổn định theo trọng lực.
4. Khi dùng kính, bấm `3 · Chuẩn bị VR`, sau đó `Kiểm tra cấu hình OpenXR` và `Vào VR`.

Các phần Meshy, nhập OBJ và AI nâng cao được thu gọn mặc định để các thao tác thử đồ chính luôn dễ nhìn. Trong kính VR có tám nút lớn: `MOTION`, `FIT`, `RESET`, `EXIT`, `OUTFIT -`, `OUTFIT +`, `VIEW`, `QUALITY`.

## Điều khiển desktop

- `W/S`: tiến–lùi; `A/D`: di chuyển vòng quanh ma-nơ-canh như chế độ cũ. Camera tự nhìn lại mannequin và có vùng chặn không đi xuyên cơ thể.
- Giữ chuột phải + rê: quay camera; con lăn: chỉnh tốc độ.
- `R`: đưa ma-nơ-canh và camera về vị trí trình diễn.
- Nút trên bảng điều khiển: chạy/dừng motion, fitting pose, đổi váy, chỉnh vật liệu và nhập OBJ.
- Khi dừng motion, mannequin hoàn tất bước cuối, chuyển dần về tư thế đứng và vải tiếp tục settle. Bộ giải chỉ nghỉ khi chuyển động vải đủ nhỏ trong một khoảng liên tục.

## Điều khiển VR/OpenXR

- Cần trái: di chuyển theo hướng đầu nhìn.
- Cần phải: smooth turn và quay toàn bộ góc người chơi/camera.
- Trigger phải: bấm nút VR.
- Grip phải: kéo vải hoặc kéo/di chuyển ma-nơ-canh; thả grip để vải tiếp tục rơi và ổn định theo trọng lực.
- Bảng VR có `MOTION`, `FIT`, `VIEW`, `RESET`, `EXIT`.

Đọc `VR_QUICK_START.txt` để kết nối nhanh và `VR_EXPERIMENT_SETUP.md` trước khi thử nghiệm đầy đủ trên kính thật.

## Năm thiết kế có sẵn

- Atelier Midi.
- Emerald Column: đầm dài nguyên bản, có xẻ trước vật lý để bước chân không kéo giãn toàn bộ thân váy.
- Rose Couture: đầm xòe nguyên bản.
- Sapphire Cocktail: đầm cocktail xanh sapphire.
- Ivory Blouse Set: áo lụa tương tác VR và chân váy bút chì.

Các mẫu là thiết kế procedural nguyên bản lấy cảm hứng từ thẩm mỹ runway cao cấp; project không chứa asset/logo/mẫu sao chép của Gucci hay thương hiệu khác. Với bộ Ivory, giữ Grip phải ở phần ngực áo rồi kéo ngang/dọc hoặc xoay cổ tay để chỉnh áo; phần chỉnh được giữ sau khi thả.

## Ổn định vải

- Cơ thể kỹ thuật có 53.514 đỉnh; mỗi váy có 672 điểm PBD và áo Ivory có 384 điểm PBD. Mesh hiển thị được chia nhỏ riêng và kiểm tra tiếp xúc với proxy ở các điểm hiển thị.
- Áo Ivory dùng trọng lực, sáu proxy thân trên (eo, sườn, hai vùng ngực, xương đòn, cổ), bốn capsule tay và kéo điểm trực tiếp bằng VR thay vì chỉ scale một khối cứng.
- Mesh động tái sử dụng buffer; không cấp phát mảng mới mỗi frame.
- Fixed-step 60 Hz, một substep thường và hai substep khi kéo VR/collider di chuyển nhanh, giới hạn catch-up để tránh vòng xoáy tụt FPS.
- Mỗi điểm vải có neo mềm theo hàng vào tư thế cơ thể; điểm đầu đường may được cố định, phần còn lại vẫn chịu trọng lực trong giới hạn tether tăng dần.
- Va chạm swept capsule chia nhỏ 8 đoạn chân, hai proxy hông/eo, khoảng hở riêng theo silhouette, sàn và giảm vận tốc tiếp xúc.
- Xử lý va chạm trước/sau constraint, tâm các tam giác ở mỗi substep và tự tiếp xúc điểm–điểm bằng spatial hash. Kiểm tra đường đi điểm vải với ellipsoid/capsule giảm tunnelling; chưa phải CCD tam giác đầy đủ.
- Thân áo đầm blend theo `Hips → Spine → Spine2` và có khoảng hở giải phẫu phía trước, thay vì gắn cứng toàn bộ vào hông.
- Kéo vải có điểm giữ và các đỉnh lân cận; khi thả, quán tính/trọng lực tiếp tục hoạt động.

Đây vẫn là prototype: chưa có self-collision đầy đủ, chưa mô phỏng pattern/đường may theo thông số vật liệu đo thực và OBJ nhập ngoài chỉ là geometric preview. Cần kiểm thử từng animation và từng kính thật trước khi dùng sản xuất.

## Nano Banana

`NANO_BANANA_SETUP.md` hướng dẫn nhập Gemini API key và tạo swatch vải. Chỉ prompt chữ được gửi lên API; mesh, rig và solver vẫn chạy cục bộ. Không có API key trong source/ZIP.

## Meshy Image-to-3D

Tab Trang phục có cầu nối tùy chọn tới Meshy: chọn PNG/JPG, nhập key chỉ giữ trong RAM, tạo smart-topology OBJ khoảng 30.000 polygon rồi đưa kết quả vào màn hình kiểm tra đơn vị/mốc. Đọc `MESHY_2D_TO_3D_PIPELINE.md`. Ảnh chỉ được tải lên khi người dùng bấm tạo; tác vụ dùng credit Meshy. Mesh AI vẫn cần retopology/rig/pattern trước khi được coi là trang phục mô phỏng sản xuất.

## Kiểm tra

`Verification/Numerical_Checks.json` ghi bài kiểm tra dài năm preset. `Verification/Source/CollisionSmoke.cs` là regression test nhanh cho va chạm thân, neo mềm, tính hữu hạn và reset xác định. Mã runtime cũng được compile bằng bộ thư viện Unity 2022.3 cài trên máy.

Kết quả dành riêng bản thử 0.8.1: `Verification/Runway_Trial_Checks.json` và `Verification/Unity_Runway_Trial.json` khi bài kiểm tra tương ứng hoàn tất. Kết quả cũ không chứng nhận bản mới hết xuyên.

Bản 0.8.2: ảnh trước/sau và số đo fit ở `Verification/FitStudy`, log kiểm tra Unity cuối ở `Verification/Garment-Fit-After.log`. JSON thử số cũ của 0.8.1 được giữ làm lịch sử, không dùng để chứng nhận phom mới.

Không thể coi bài kiểm tra số là kiểm thử kính thật. Hãy theo checklist VR rồi ghi FPS, thiết bị và lỗi Console trong buổi thử nghiệm.
