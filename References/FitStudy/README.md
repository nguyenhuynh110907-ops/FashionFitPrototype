# Dữ liệu tham chiếu fit ôm, rủ và xòe

Thu thập và đối chiếu ngày 2026-10-27. Các ảnh nằm ngoài Assets, không được đóng vào scene hoặc dùng làm texture thương hiệu. Đây là nghiên cứu tham chiếu và hiệu chỉnh hình học, chưa huấn luyện mô hình AI.

## Tệp đã tải

| Tệp | Nguồn | Quyền/Phạm vi | Điều quan sát |
| --- | --- | --- | --- |
| [Met_85420.jpg](Images/Met_85420.jpg) | [House of Worth, Evening dress 1893–95](https://www.metmuseum.org/art/collection/search/85420) | Met API trả `isPublicDomain=true`; ảnh Open Access | Thân ôm eo, chân váy mở rộng; điểm tựa/đường nối rõ |
| [Met_90483.jpg](Images/Met_90483.jpg) | [Dress, 1805–10](https://www.metmuseum.org/art/collection/search/90483) | Met API trả `isPublicDomain=true`; ảnh Open Access | Độ rủ phụ thuộc nơi giữ vải; phom rủ không bám đều theo thân |
| [Met_98097.jpg](Images/Met_98097.jpg) | [Dress, ca. 1810](https://www.metmuseum.org/art/collection/search/98097) | Met API trả `isPublicDomain=true`; ảnh Open Access | Thân trên giữ phom, phần váy có độ dư tăng về gấu |
| [4D_DRESS_Web_Overview.png](Images/4D_DRESS_Web_Overview.png) | [Wang et al., ETH Zürich, CVPR 2024](https://eth-ait.github.io/4d-dress/) | Ảnh overview trên website ghi CC BY-SA 4.0; giữ nguyên và ghi nguồn | Người thật mặc trang phục chuyển động; phần rủ có khoảng hở và độ trễ so với body |

URL ảnh gốc:

- https://images.metmuseum.org/CRDImages/ci/web-large/C.I.54.4.1ab_F.jpg
- https://images.metmuseum.org/CRDImages/ci/web-large/C.I.39.13.105_F.jpg
- https://images.metmuseum.org/CRDImages/ci/web-large/11.60.805_F.JPG
- https://eth-ait.github.io/4d-dress/assets/images/4D-DRESS.png

Đây là ảnh tham chiếu, không phải pattern/mesh được sao chép vào game. Ảnh lịch sử hỗ trợ hiểu cấu trúc và điểm tựa; chúng không chứng minh một bộ thông số cho mọi mẫu đầm hiện đại.

## Dữ liệu 4D đầy đủ

Chưa tải dataset scan đầy đủ. [Trang đăng ký](https://4d-dress.ait.ethz.ch/) yêu cầu email cơ sở học thuật và quy định mục đích học thuật phi thương mại, hạn chế phân phối lại. Không tạo tài khoản hoặc nộp thông tin thay người dùng. Bản hiện tại chỉ dùng ảnh overview công khai làm tham chiếu thị giác.

## Số đo từ model của chính dự án

[Body_Profile.json](Body_Profile.json) chứa 38 tiết diện × 64 hướng từ Y=0,72 đến 1,46 m. Các tam giác chịu trọng số tay lớn bị loại; bao lồi tiết diện đóng các khoảng trống ở vai và giữa hai chân. Đây là đường bao phục vụ fit quần áo, không phải phép đo may mặc cơ thể người thật.

Mesh nguồn: `Assets/Resources/Fitting/female-mesh.bytes`.

SHA256: `390A75E3D0F72FD95488F8A6E956CEE7825A20137739C6EC505366A325A4CDCD`.

Các mốc dùng trong bản này:

- Eo: Y≈1,12 m, chu vi đường bao ≈70,5 cm.
- Hông dưới: Y≈0,84 m, chu vi đường bao ≈102,1 cm.
- Ngực: Y≈1,26 m, chu vi đường bao ≈90,9 cm.

Bảng runtime được sinh ở `Assets/FashionFit/Runtime/TechnicalBodyFitData.cs`; `BodyFitProfile.cs` nội suy số đo theo độ cao và góc. Cần đo/bake lại khi thay mesh kỹ thuật, không áp bảng này cho một avatar khác chưa hiệu chỉnh.

## Quy tắc đã chuyển vào trang phục

- Ôm: theo đường bao body, có độ dư dương và giới hạn contact riêng; không ép sát bằng cách thu nhỏ toàn bộ mesh đồng đều.
- Xòe: giữ eo/thân trên, tăng thể tích dưới hông và chỉnh độ giữ phom gấu riêng; không phóng to cả vùng ngực/vai.
- Rủ: đường rơi đi từ điểm tựa và phụ thuộc chất liệu; áo Ivory giữ vùng vai/ngực nhưng cho phần dưới chuyển động.
- Trước/sau: dùng tâm tiết diện thay đổi theo chiều cao; không coi độ sâu ngực và lưng là hai nửa ellipse đối xứng.
- Thân áo/chân váy dùng cùng đường eo. Bodice cần pháp tuyến hướng ra ngoài để nhận ánh sáng đúng.

Hướng dẫn đo đối chiếu: [Te Papa — Measuring a garment and selecting a form](https://www.tepapa.govt.nz/learn/for-museums-and-galleries/how-guides/collection-management/display-textile-garments/measuring). Các thông số ease và tethers trong code là lựa chọn thử nghiệm cho asset này, không phải số đo suy ra từ ảnh bảo tàng.

Ảnh kết quả Unity: [Verification/FitStudy](../../Verification/FitStudy). Các tệp `Before_` được chụp trước khi sửa phom; `After_` là bản fit mới với cùng camera/ánh sáng. Phiên bản sau có thêm phối áo Ivory sáng/chân váy tối để đọc rõ hai món.
