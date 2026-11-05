# Collision update v0.8.0 — 2026-10-27

## Thay đổi trực tiếp

- Thân áo đầm không còn skin 100% vào xương hông. Trọng số được blend từ `Hips` qua `Spine` đến `Spine2` theo chiều cao.
- Biên dạng mặt trước thân áo có ease riêng tại ngực; không còn dùng một ống ellipse đối xứng trước/sau.
- Áo Ivory dùng sáu ellipsoid thân trên và bốn capsule tay. Váy dùng thêm proxy eo ngoài proxy hông và tám capsule chân.
- Mỗi điểm động có tether giới hạn khoảng cách đến điểm tựa hoạt hình. Các hàng đường may trên cùng vẫn là điểm cố định.
- Mỗi tam giác có mẫu va chạm ảo tại tâm để phát hiện collider lọt qua khoảng trống giữa các đỉnh. Lưới 2×2 được kiểm tra luân phiên qua bốn substep nhằm giới hạn chi phí CPU.
- Collider chuyển động được lấy mẫu tại 0%, 25%, 50%, 75% và 100% hành trình khung hình. Bước dịch chuyển mỗi điểm cũng được giới hạn để giảm tunnelling khi VR kéo nhanh.

## Kiểm tra đã chạy

- Runtime source compile thành công với compiler/reference assemblies của Unity 2022.3.62f1.
- `CollisionSmoke.cs`: 128 điểm, bắt đầu cắt qua ellipsoid chuyển động; tất cả điểm động kết thúc phía ngoài (`minOutside = 1.002421`), không NaN/Infinity và reset xác định.
- Bài numerical dài cũ vẫn nằm trong `Numerical_Checks.json`. Không ghi đè kết quả đó bằng lần chạy bị dừng giữa chừng trên Mono.

## Giới hạn cần nói rõ

Đây là solver PBD CPU với proxy giải phẫu, chưa phải va chạm tam giác–tam giác đầy đủ hay self-collision sản xuất. Cần dừng Play, chờ Unity compile, Play lại và quan sát cả năm bộ trong motion. Kiểm tra kính thật/FPS vẫn phải thực hiện trên thiết bị đích.

## Cơ sở kỹ thuật tham khảo

- Unity Cloth: continuous collision, capsule/sphere colliders, tether anchors, self/inter-collision và virtual particles.
- Unity `Cloth.useVirtualParticles`: thêm mẫu va chạm trong tam giác để tăng độ ổn định mà không buộc tăng độ phân giải mesh.
- Các solver XPBD mã nguồn mở dùng chu trình predict → solve constraints → solve collisions → cập nhật vận tốc; bản này giữ kiến trúc PBD CPU hiện có để không đưa dependency/compute shader chưa kiểm chứng vào project.
