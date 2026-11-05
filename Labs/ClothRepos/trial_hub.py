"""Local review UI for real upstream GarmentCode CPU outputs."""
from pathlib import Path
import asyncio, json, math, time, sys
from nicegui import ui, app
ROOT = Path(__file__).resolve().parent
OUT = ROOT/'Outputs'
app.add_static_files('/results',OUT)
JOB = {'running':False}

@app.get('/api/health')
def health():
    return {'application':'FashionFit Repo Trials','version':1}

@ui.page('/')
def index():
    ui.colors(primary='#406b85',secondary='#c4a66a')
    ui.add_head_html('<style>body{background:#f1f0ec;color:#24343e}.q-card{border-radius:16px!important}.q-btn{text-transform:none} .nicegui-content{max-width:1500px;margin:auto;width:100%}</style>')
    ui.label('FASHIONFIT / REPO LAB').classes('text-sm tracking-widest text-gray-500 mt-4')
    ui.label('Thử rập may và độ rủ vải').classes('text-3xl font-bold')
    ui.label('GarmentCode · 492 sao tại 03/11/2026 · Mô phỏng CPU đã chạy trên máy này').classes('text-gray-600')
    ui.label('Đây là bản thử trên web. Chưa kết nối grip VR hoặc thay solver trong game Unity.').classes('text-sm text-gray-600')
    with ui.row().classes('w-full items-start gap-5 flex-wrap'):
        with ui.card().classes('w-80 shrink-0 p-5 gap-3'):
            ui.label('1 · Chọn mẫu').classes('text-lg font-bold')
            selection = ui.select({'tee':'Áo thun đã kiểm tra','skirt':'Váy xòe đã kiểm tra',
                                   'custom':'Áo với cổ tự chỉnh'},value='tee').classes('w-full')
            ui.label('2 · Quan sát').classes('text-lg font-bold mt-2')
            ui.label('Kéo chuột để xoay · Cuộn để phóng to · Chuột phải để dịch góc nhìn.').classes('text-sm')
            ui.button('Góc trước',on_click=lambda:scene.move_camera(x=0,y=-3.1,z=1.3,look_at_x=0,look_at_y=0,look_at_z=1.0)).classes('w-full')
            ui.button('Góc sau',on_click=lambda:scene.move_camera(x=0,y=3.1,z=1.3,look_at_x=0,look_at_y=0,look_at_z=1.0)).classes('w-full')
            ui.button('Cận cổ áo',on_click=lambda:scene.move_camera(x=.48,y=-.9,z=1.5,look_at_x=0,look_at_y=0,look_at_z=1.37)).classes('w-full')
            ui.separator()
            ui.label('3 · Thay cổ và mô phỏng lại').classes('text-lg font-bold')
            ui.label('Các số dưới là tỷ lệ thiết kế của repo, không phải cm.').classes('text-xs text-gray-500')
            width=ui.number('Rộng cổ',value=.2,min=-.2,max=.5,step=.05).classes('w-full')
            front=ui.number('Sâu cổ trước',value=.4,min=.3,max=1,step=.05).classes('w-full')
            back=ui.number('Sâu cổ sau',value=0,min=0,max=.5,step=.05).classes('w-full')
            run_button=ui.button('May và mô phỏng cổ mới',on_click=lambda:run_custom()).classes('w-full')
            progress=ui.label('Mỗi lần chạy CPU khoảng 1–3 phút.').classes('text-xs text-gray-500')
            ui.link('Mở bộ chỉnh rập gốc GarmentCode','http://127.0.0.1:8766',new_tab=True)
            ui.link('Tải báo cáo kiểm tra','/results/REPORT.md',new_tab=True)
        with ui.column().classes('flex-1 min-w-80 gap-4'):
            status=ui.label().classes('text-sm font-medium')
            with ui.scene(width=850,height=620,grid=False,background_color='#deded8').classes('w-full rounded-2xl') as scene:
                displayed=scene.gltf('/results/tee.glb').rotate(math.pi/2,0,0)
            scene.move_camera(x=0,y=-3.1,z=1.3,look_at_x=0,look_at_y=0,look_at_z=1.0,duration=0)
            with ui.row().classes('w-full gap-3'):
                front_image=ui.image('/results/Draped/tee/tee_render_front.png').classes('w-52 rounded-xl')
                back_image=ui.image('/results/Draped/tee/tee_render_back.png').classes('w-52 rounded-xl')
            download=ui.link('Tải GLB gồm body và trang phục','/results/tee.glb',new_tab=True)
            ui.label('Màu xanh trong cửa sổ 3D dùng để nhìn phom. Ảnh phía dưới tô màu theo từng mảnh rập.').classes('text-xs text-gray-500')
            ui.label('0 giao cắt là kết quả bộ đếm của repo tại trạng thái tĩnh này. Chưa chứng minh không xuyên vải khi đi bộ, đổi cơ thể hoặc kéo bằng VR.').classes('text-sm text-gray-600 max-w-3xl')
    def refresh():
        nonlocal displayed
        name=selection.value
        path=OUT/f'{name}_result.json'
        if not path.exists():
            status.set_text('Chưa có mẫu tự chỉnh. Hãy bấm “May và mô phỏng cổ mới”.')
            return
        result=json.loads(path.read_text())
        stats=result['sim']['stats']
        errors=stats.get('fails',{})
        status.set_text(f"{result['vertices']:,} đỉnh · {result['triangles']:,} tam giác · {result['seconds']:.1f} giây CPU · Giao cắt body: {stats['body_collisions'].get(name,'?')} · Tự giao: {stats['self_collisions'].get(name,'?')}"+(' · Cần xem lỗi: '+str(errors) if errors else ''))
        displayed.delete()
        with scene:
            displayed=scene.gltf(f'/results/{name}.glb?v={int(time.time())}').rotate(math.pi/2,0,0)
        front_image.set_source(f'/results/Draped/{name}/{name}_render_front.png?v={int(time.time())}')
        back_image.set_source(f'/results/Draped/{name}/{name}_render_back.png?v={int(time.time())}')
        download.props(f'href=/results/{name}.glb')
    async def run_custom():
        if JOB['running']:
            ui.notify('Đang có một mô phỏng chạy. Hãy chờ xong.',type='warning')
            return
        JOB['running']=True
        run_button.disable()
        progress.set_text('Đang may và mô phỏng trên CPU… giữ trang này mở.')
        try:
            with (ROOT/'CustomTrial.log').open('w',encoding='utf-8') as log:
                process=await asyncio.create_subprocess_exec(sys.executable,'-u','-X','utf8',str(ROOT/'run_garment_trial.py'),
                    'custom','--width',str(width.value),'--front',str(front.value),'--back',str(back.value),
                    stdout=log,stderr=log)
                code=await process.wait()
            if code:
                raise RuntimeError('Mô phỏng chưa thành công. Xem CustomTrial.log trong thư mục Repo_Trials.')
            selection.set_value('custom')
            refresh()
            progress.set_text('Đã hoàn tất. Xoay mô hình để kiểm tra cổ trước, cổ sau và nách.')
        except Exception as exc:
            progress.set_text(str(exc))
        finally:
            JOB['running']=False
            run_button.enable()
    selection.on_value_change(lambda _:refresh())
    refresh()

ui.run(host='127.0.0.1',port=8765,reload=False,show=False,title='FashionFit Repo Lab')
