# Tài liệu FashionFit

Bản hiện tại **V090**: [tối ưu grip/pose và dọn bản build trùng](RESPONSE_OPTIMIZATION_GUIDE.md).

- [Quy trình Studio và ghi hiệu năng](STUDIO_WORKFLOW.md)
- [Bảng thông số kéo/nén](SURFACE_READOUT_VR_GUIDE.md)
- [5 bộ nam](HOLLISTER_MENS_VR_GUIDE.md)
- [Lịch sử README và nghiên cứu cũ](HISTORY_README_V089.md)

## Chơi ngay bằng kính Meta

Mở **FashionFit VR** trên Desktop → **01 - VAO GAME VR**. Đây là game đầy đủ, không mở Unity Editor. Cắm kính và vào Quest Link trong kính trước, rồi trong game chọn **VR / CAMERA → Chuẩn bị phiên VR → Vào VR**.

- [Kết nối và vào lại VR](VR_REENTRY_GUIDE.md)
- [Đi trong phòng bằng kính](VR_FREE_MOVEMENT.md)
- [Hai tay Grip, mannequin pose và nhịp đi bộ](VR_TWO_HANDS_GUIDE.md)
- [Tủ đồ và menu trong kính](ATELIER_VR_GUIDE.md)
- [Thử mở nút và cởi blouse](VR_BUTTON_BLOUSE_GUIDE.md)

- [Mannequin pose mặc đồ, nón đàn hồi và đo độ giãn](POSE_MATERIAL_VR_GUIDE.md)

- [Mới: áo dài, chọn lớp, Grip áo/nón đang mặc](LAYERED_AODAI_VR_GUIDE.md)

## Chỉnh sửa và phát triển

- Project Unity: thư mục cha của `Docs`; mở [Open_FashionFit.bat](../Open_FashionFit.bat).
- [Chuyển trang phục 3D có sẵn](GARMENT_3D_TRANSFER_GUIDE.md)
- [Chất liệu và thông số cơ học](MATERIAL_PHYSICS_GUIDE.md)
- [Thư viện texture](../AI/MaterialLab/README.md)
- [Dữ liệu huấn luyện](../AI/ClothData/README.md)
- Công cụ thu thập và huấn luyện: [Tools/Launchers](../Tools/Launchers).
- [Báo cáo triển khai áo cởi](../Verification/GarmentDoffing/IMPLEMENTATION_REPORT.md)

## Phân biệt các thư mục

`Downloads/FashionFit_Run` là bản game chạy được. `Desktop/FashionFitPrototype` là source Unity đang phát triển. `Downloads/Fashion_Fitting_Room_v0.5.2/_Luu_tru/Phien_ban_cu` chứa ZIP v0.6/v0.7 và source v0.6 cũ để lưu lại, không phải bản nên mở chơi. Source v0.7 đang được ứng dụng giữ nên vẫn giữ nguyên vị trí cũ.

Không tách EXE khỏi thư mục `_Data`, DLL và `MonoBleedingEdge`. Không di chuyển riêng Assets, Packages hay ProjectSettings của Unity.


## V091

- [Grip khớp và thử mặc/cởi vật lý](CONTACT_DRESSING_GUIDE.md)
- [Báo cáo và giới hạn V091](../Verification/ContactDressing/REPORT.md)
