"""Create a report from this run's actual results; do not substitute old results."""
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parent


def read(name):return json.loads((ROOT/name).read_text(encoding='utf-8'))


def run():
    motion=read('unity_motion.json');close=read('CloseViews/results.json')
    slow=read('SlowPresentation/results.json');contact=read('SlowPresentation/source_body_contact.json')
    models=read('model_reevaluation.json');data=read('data_verification.json');intake=read('intake_rerun.json')
    assert 'UNITY_RUNWAY_TRIAL_OK' in (ROOT/'motion_close.log').read_text(encoding='utf-8',errors='replace')
    assert 'CLOSE_VIEW_AFTER_OK' in (ROOT/'motion_close.log').read_text(encoding='utf-8',errors='replace')
    assert 'SLOW_PRESENTATION_OK' in (ROOT/'slow_presentation.log').read_text(encoding='utf-8',errors='replace')
    assert len(contact['poses'])==15 and len(slow['looks'])==5 and len(close['items'])==16
    unslept=[r['name'] for r in slow['looks'] if not r['clothSleepingAfterStop']]
    missed=[r['case'] for r in intake['results'] if r.get('invalid_input_undetected')]
    summary=dict(status='not_ready_for_student_asset_or_headset_release',
        generated_utc=datetime.now(timezone.utc).isoformat(),
        engine='Unity 2022.3.62f1, D3D11, AMD Radeon 860M, isolated batch-mode project',
        runtime_identical_to_main=True,student_asset_retarget_implemented=False,headset_tested=False,
        motion=dict(looks=motion['looks'],steps=motion['physicsSteps'],proxy_violations=motion['proxyViolations'],max_stretch=motion['maxStretch']),
        close_views=dict(count=len(close['items']),missing_upper_views=sum(r['pinkPixels']<200 for r in close['items']),
            outside_bounds_vertices=sum(r['verticesOutsideBounds'] for r in close['items']),shirt_collar_anchor_within_2mm=True),
        slow_motion=dict(steps=sum(r['steps'] for r in slow['looks']),
            all_stopped=all(r['stopped'] for r in slow['looks']),
            proxy_violations=sum(r['proxyViolations'] for r in slow['looks']),
            not_sleeping_after_six_seconds=unslept,
            mean_cloth_ms_min=min(r['meanClothMilliseconds'] for r in slow['looks']),
            mean_cloth_ms_max=max(r['meanClothMilliseconds'] for r in slow['looks']),
            timing_scope='CPU cloth update in Unity Editor batch mode; not total frame time or headset FPS'),
        original_body_contact=dict(status=contact['status'],poses=len(contact['poses']),
            samples=sum(r['samples'] for r in contact['poses']),
            max_sampled_penetration_mm=contact['max_sampled_penetration_mm'],
            samples_inside_over_1mm=contact['samples_inside_over_1mm'],
            masked_body_used=contact['masked_body_used'],continuous_triangle_certified=False),
        model_reevaluation=models,source_data_status=data['status'],intake_missed_cases=missed,
        trained_or_changed_weights=False)
    (ROOT/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    table='\n'.join(f'| {r["name"]} | {r["meanClothMilliseconds"]:.2f} | {r["maximumClothMilliseconds"]:.2f} | {r["maxStretch"]:.3f} | {"Có" if r["clothSleepingAfterStop"] else "Chưa"} |' for r in slow['looks'])
    text=f'''# Kết quả chạy thử thực tế — 28/10/2026

Đã chạy Unity và suy luận AI trên máy, không chỉ đọc báo cáo cũ. Kết quả: các mẫu procedural vượt qua kiểm tra hiển thị/chuyển động/proxy trong phạm vi thử, nhưng **chưa đạt để triển khai nhận đồ sinh viên hoặc thực nghiệm headset**. Hai bộ còn mô phỏng sau dừng, chi phí vải còn cao, công cụ kiểm tra file vẫn bỏ lọt lỗi và AI vùng cổ chưa đủ chất lượng.

## Môi trường và phạm vi

- Unity **2022.3.62f1**, Direct3D 11, **AMD Radeon 860M**, batch mode trong `Verification/CloseViewProject`.
- Đã đối chiếu hash toàn bộ runtime C# giữa bản thử và dự án chính; không thay runtime hoặc checkpoint. Hash nguồn tại [runtime_sha256.json](runtime_sha256.json).
- Dùng năm trang phục procedural sẵn có. Chưa có viewer/retarget GLB mới và chưa thử file trang phục của sinh viên. Bài này không chứng minh giữ đúng hình thêu của một asset sinh viên.
- Các phép chạy Unity kết thúc trước suy luận AI và kiểm tra khoảng cách offline; không chạy train AI song song khi ghi số đo Unity.
- Chưa dùng headset, chưa đo GPU/whole-frame/dropped frames trong standalone player.

## Kết quả đã chạy

| Bài thử | Kết quả mới | Phạm vi |
|---|---|---|
| Kiểm tra dữ liệu | {data['status']}; 200 graph, 1.002 ảnh hoa văn, 90 asset PBR/599 map | Hash, split, nhãn chưa biết, dữ liệu đầu vào; không phải chất lượng mặc đồ |
| Rig chuyển động và năm bộ đồ | {motion['physicsSteps']} bước, {motion['proxyViolations']} vi phạm proxy | Kiểm tra thực trong Unity, có chuyển/đổi hướng rig; chưa phải CCD tam giác |
| Camera gần | {len(close['items'])}/16 góc đạt, không mất thân trên, không đỉnh ngoài bounds | Bốn bộ có thân trên, mỗi bộ bốn hướng; chỉnh hai cực rộng/dài áo giữ cổ trong 2 mm |
| Đi chậm–dừng | {sum(r['steps'] for r in slow['looks'])} bước đi trên năm bộ; cả năm dừng gait được; 0 vi phạm proxy | Mỗi bộ có warm-up, 540 bước đi và 360 bước sau lệnh dừng |
| Vải về trạng thái nghỉ | 3/5 bộ đạt sau 6 giây; Sapphire và Ivory chưa đạt | Batch hiện có không assert cờ ngủ này, nên marker `SLOW_PRESENTATION_OK` không có nghĩa toàn bộ mục tiêu ổn định đã đạt |
| Tiếp xúc với body gốc | {len(contact['poses'])} pose / {summary['original_body_contact']['samples']:,} mẫu; sâu nhất {contact['max_sampled_penetration_mm']:.3f} mm | Lấy đỉnh được tham chiếu và tâm mặt; không dùng body che da; không chứng nhận toàn tam giác/chuyển động liên tục |
| AI hoa văn | Suy luận lại từ **101 ảnh nguồn**: accuracy {models['pattern_test']['accuracy']*100:.2f}%, macro-F1 {models['pattern_test']['macro_f1']:.3f} | Đọc archive, kiểm tra SHA ảnh, tính lại DINO; feature khớp cache tối đa {models['pattern_test']['largest_absolute_feature_difference_from_training_cache']:.1g}. Đây là test cũ, không phải holdout mới |
| AI vùng cấu tạo 3D | Nạp checkpoint và chạy lại 28 graph test: macro-F1 {models['region_test']['macro_f1_supported']:.3f}; cổ áo F1 {models['region_test']['labels']['collar_panel']['f1']:.3f} | Chỉ một thiết kế test có panel cổ; chưa dùng tự sửa/fitting |
| File đầu vào lỗi | Vẫn bỏ lọt {len(missed)} trường hợp | Material index, texture index, UV accessor và buffer length sai; hai đối chứng thiếu UV/file buffer bị phát hiện; có thêm đối chứng hình học hợp lệ |

## Thời gian xử lý vải và độ ổn định

Số dưới đây đo CPU trong Unity Editor batch mode, **không phải FPS headset hoặc toàn bộ frame**. Không có p95/p99 vì bài thử hiện chỉ lưu trung bình/cực đại.

| Trang phục | Trung bình ms/bước | Cao nhất ms/bước | Giãn cạnh lớn nhất khi đi chậm | Đã ngủ sau 6 giây dừng |
|---|---:|---:|---:|---|
{table}

Ngân sách toàn frame ở 72 Hz là 13,89 ms. Riêng cloth update trung bình đã khoảng **{summary['slow_motion']['mean_cloth_ms_min']:.1f}–{summary['slow_motion']['mean_cloth_ms_max']:.1f} ms**, do đó số đo này chưa hỗ trợ kết luận đáp ứng VR 72 Hz. Cần profiling standalone trên headset/phần cứng đích để định lượng chính xác. Spike Sapphire được ghi lại, chưa xác định nguyên nhân bằng trace.

Max stretch là tỉ lệ của **cạnh xấu nhất** với chiều dài nghỉ; không phải toàn bộ vải giãn cùng mức. Chuyển động rig nhanh hơn ở bài riêng có max stretch {motion['maxStretch']:.3f}. Đây chưa phải kiểm định thông số cơ học cho vải thật.

## Quan sát ảnh mới

Đã xem ảnh [Emerald nhìn gần](CloseViews/Look2_View0.png) và [Ivory nhìn bên](CloseViews/Look5_View1.png). Thân trên hiện diện đúng, nhưng cổ/mép/lỗ tay vẫn có dáng hình học thô và chưa có đường chỉ, nút hay thêu chi tiết của tài sản 3D thật. Kết quả camera/bounds đạt không đồng nghĩa đã đạt yêu cầu thẩm mỹ.

Ảnh đứng/đi mới của cả năm bộ nằm trong [SlowPresentation](SlowPresentation). Xem thêm [ảnh Emerald toàn thân khi đi](SlowPresentation/Look2_WalkingA_Front.png).

## Việc nên sửa sau bài thử

1. Hoàn thiện validator và importer giữ UV/material/normal; kiểm chứng trên khoảng 10 asset 3D thực trước fitting.
2. Điều tra Sapphire/Ivory chưa ngủ sau dừng, thêm assertion trạng thái ổn định và telemetry vận tốc; tránh coi `stopped` của gait là cloth đã đứng yên.
3. Profile chi phí physics/publish/normals, thử tách nhịp render–physics có nội suy, LOD và tránh publish nhiều lần/frame. Đối chiếu va chạm sau từng tối ưu.
4. Mở rộng đánh giá cổ áo bằng nhiều thiết kế độc lập; AI hiện chỉ gợi ý/nhận dạng offline. Giữ nguyên UV/texture gốc là cơ sở bảo toàn hoa văn.

## Bằng chứng

- [Tổng hợp máy đọc được](summary.json)
- [Log Unity chuyển động và camera](motion_close.log), [log đi chậm](slow_presentation.log)
- [Kết quả camera](CloseViews/results.json), [kết quả đi chậm](SlowPresentation/results.json)
- [Khoảng cách body gốc tại 15 pose](SlowPresentation/source_body_contact.json)
- [Suy luận lại AI từ nguồn](model_reevaluation.json), [tái hiện file lỗi](intake_rerun.json), [QA dữ liệu](data_verification.json)

Đợt này chạy kiểm chứng và báo cáo; không huấn luyện lại, không tự thay solver/importer hoặc công bố hết xuyên trong mọi chuyển động.
'''
    (ROOT/'REPORT.md').write_text(text,encoding='utf-8')
    print(json.dumps({k:v for k,v in summary.items() if k!='model_reevaluation'},ensure_ascii=False),flush=True)


if __name__=='__main__':run()
