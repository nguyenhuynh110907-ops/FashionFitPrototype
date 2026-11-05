# Meshy 2D → 3D garment pipeline

Fashion Unity có một cầu nối prototype tới Meshy Image-to-3D.

## Dùng trong ứng dụng

1. Tạo API key và credit trong tài khoản Meshy.
2. Mở tab `TRANG PHỤC` → `ẢNH 2D → MESH 3D · MESHY`.
3. Nhập key, chọn ảnh PNG/JPG rõ nền, tốt nhất thấy trọn trang phục ở chính diện.
4. Bấm `Tạo smart-topology OBJ và nhập`.
5. Khi tác vụ hoàn tất, OBJ được lưu trong `Application.persistentDataPath/MeshyGarments` và mở ở màn hình kiểm tra đơn vị/mốc của project.

Có thể đặt biến môi trường `MESHY_API_KEY` trước khi mở Unity. Key chỉ giữ trong RAM, không ghi vào project hay PlayerPrefs.

## Cấu hình request

- Endpoint: `POST https://api.meshy.ai/openapi/v1/image-to-3d`
- Model: `meshy-t2.5`
- Mode: `smart-topology`
- Mục tiêu: 30.000 polygon, texture/PBR bật, xuất OBJ.

## Giới hạn quan trọng

Image-to-3D tạo một vật thể 3D từ hình dáng quan sát được; nó không tự biết pattern may, độ dày, đường may, kích thước cơ thể hay rig của ma-nơ-canh. OBJ tải về vì vậy chỉ đi qua bước geometric preview/fit hiện có. Muốn dùng như trang phục sản xuất cần:

1. Kiểm tra mặt trước/sau và sửa phần AI đoán sai trong Blender.
2. Retopology thành quad sạch, tách lớp trong/ngoài và sửa normal.
3. Chuẩn hóa Y-up, +Z-front, mét, pivot và mốc eo.
4. Skin/weight theo rig hoặc chuyển về pattern trong CLO/Marvelous Designer.
5. Tạo proxy cloth riêng, kiểm tra va chạm và self-collision.
6. Xuất FBX cho Unity; OBJ runtime hiện chỉ là preview hình học.

Không nên gửi ảnh riêng tư hoặc tài sản không có quyền sử dụng. Việc tạo model dùng quota/credit của tài khoản Meshy.
