using UnityEngine;
namespace FashionFit {
 public static class MaterialReadout {
  public static string Format(MembraneMaterial m,MembraneReading reading,ElasticSurface surface,LayeredPanel panel){
   if(m==null)return "Chưa chọn mô hình vật liệu có E.\nChọn Cotton / Stretch ở trang vật liệu.\nKhông suy lực hoặc sức bền từ texture.";
   string travel=panel?"Nâng/xếp tà: "+(panel.GripTravelM*100).ToString("F1")+" cm (không phải giãn sợi)":surface?"Dịch tay / lệnh: "+(surface.RequestedTravelM*100).ToString("F1")+" / "+(surface.AppliedTravelM*100).ToString("F1")+" cm":"Chưa có điểm Grip trên bề mặt này";
   string gauge=panel?"Tà tự do: giới hạn theo chiều dài và điểm treo":surface&&surface.GrabCount==2?"L0 hai điểm neo: "+(surface.GaugeLengthM*100).ToString("F1")+" cm; giới hạn lệnh "+(m.pullTravelM*100).ToString("F1")+" cm":"Giới hạn lệnh: "+(m.pullTravelM*100).ToString("F1")+" cm; L0 quy ước "+(m.referenceGaugeM*100).ToString("F0")+" cm";
   float peak=reading.rms?reading.peakTensileStrain:reading.tensileStrain;
   string strain="ε "+(reading.rms?"RMS":"đỉnh")+" kéo/nén: "+(reading.tensileStrain*100).ToString("F1")+" / "+(reading.compressiveStrain*100).ToString("F1")+"%; đỉnh kéo "+(peak*100).ToString("F1")+"%";
   string tension="T màng ≈ "+reading.tensionNm.ToString("F1")+" N/m; nén ≈ "+reading.compressionNm.ToString("F1")+" N/m";
   string force=surface?"F lệnh ảo điểm 1/2: "+surface.HandForce0.magnitude.ToString("F1")+" / "+surface.HandForce1.magnitude.ToString("F1")+" N":"Không đo lực Touch; N/m ở trên không phải N";
   string recovery="Hồi τ = "+m.recoverySeconds.ToString("F1")+" s; ~95% sau "+(m.recoverySeconds*3).ToString("F1")+" s*";
   string stiffness="E dọc/ngang = "+(m.youngWarpPa/1000).ToString("F0")+"/"+(m.youngWeftPa/1000).ToString("F0")+" kPa; h = "+(m.thicknessM*1000).ToString("F2")+" mm";
   string mass="Khối lượng mặt = "+(m.arealMass*1000).ToString("F0")+" g/m²; g = "+Physics.gravity.magnitude.ToString("F2")+" m/s²";
   return travel+"\n"+gauge+"\n"+strain+"\n"+tension+"\n"+force+"\n"+recovery+"\n"+stiffness+"\n"+mass+"\n"+(peak>m.maxStrain?"Vượt ngưỡng "+(m.maxStrain*100).ToString("F0")+"% giả định · CHƯA HIỆU CHUẨN":"ẢO, CHƯA HIỆU CHUẨN · *hồi về cân bằng có tải");
  }
 }
}
