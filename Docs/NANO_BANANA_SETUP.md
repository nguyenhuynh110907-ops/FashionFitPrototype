# Nano Banana 2.1 × Unity

Project tích hợp `gemini-nano-banana-2.1` qua Gemini Generate Content REST API để tạo swatch vải 1K rồi áp vào UV của bộ váy đang mặc.

## Cách dùng

1. Tạo Gemini API key trong Google AI Studio và kiểm tra quota/chi phí của tài khoản.
2. Mở project, chạy prototype.
3. Trong `AI FABRIC STUDIO`, nhập API key vào ô mật khẩu.
4. Nhập mô tả, ví dụ: `lụa jacquard xanh ngọc, họa tiết hình học tối giản, sợi dệt mịn, cao cấp`.
5. Bấm nút tạo và áp mẫu vải.

Có thể đặt biến môi trường trước khi mở Unity:

```powershell
$env:GEMINI_API_KEY = "API_KEY_CUA_BAN"
```

Unity gửi prompt chữ đến endpoint:

`https://generativelanguage.googleapis.com/v1/models/gemini-nano-banana-2.1:generateContent`

Ảnh nhận về được lưu trong `Application.persistentDataPath/NanoBananaFabrics` và được áp làm base-color texture. Mesh, rig, fitting và solver không được gửi lên API.

## Bảo mật và giới hạn

- Source/ZIP không chứa API key; component không lưu key vào PlayerPrefs.
- Gọi API trực tiếp từ client chỉ phù hợp prototype cá nhân. Bản phát hành nên dùng backend có xác thực, quota và rate limit.
- Việc tạo ảnh có thể dùng quota/phát sinh phí. Không thể thử gọi thật khi không có key của người dùng.
- Texture AI không tạo mesh 3D và không sửa va chạm vật lý.
- Yêu cầu seamless trong prompt không bảo đảm tile hoàn hảo; có thể phải tạo lại.
- Ảnh Nano Banana có SynthID theo tài liệu Google.

Tài liệu chính thức: https://ai.google.dev/gemini-api/docs/image-generation
