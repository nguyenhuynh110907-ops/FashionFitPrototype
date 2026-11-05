using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace FashionFit
{
    public static class RunwayFiles
    {
        public static string Pick(string title,string extensions)
        {
            #if UNITY_EDITOR
            return UnityEditor.EditorUtility.OpenFilePanelWithFilters(title,"",new[]{"Supported files",extensions});
            #elif UNITY_STANDALONE_WIN
            var data=new OpenFileName();data.structSize=Marshal.SizeOf(data);data.file=new StringBuilder(32768);data.maxFile=data.file.Capacity;data.title=title;data.filter="Supported files\0*."+extensions.Replace(",",";*.")+"\0\0";data.flags=0x00001000|0x00080000|0x00000008;
            return GetOpenFileName(data)?data.file.ToString():null;
            #else
            throw new NotSupportedException("Nhập tệp trên máy tính Windows hoặc trong Unity Editor. Kính dùng các trang phục đã nạp sẵn.");
            #endif
        }
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]sealed class OpenFileName{
            public int structSize;public IntPtr owner,instance;public string filter;public IntPtr customFilter;public int maxCustomFilter,filterIndex;public StringBuilder file;public int maxFile;public IntPtr fileTitle;public int maxFileTitle;public string initialDir,title;public int flags;public short fileOffset,fileExtension;public string defaultExt;public IntPtr customData,hook;public string templateName;public IntPtr reserved;public int reservedInt,flagsEx;
        }
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool GetOpenFileName([In,Out]OpenFileName data);
        #endif
        public static void CheckImageSize(byte[] b)
        {
            int width=0,height=0;
            if(b.Length>24&&b[0]==137&&b[1]==80&&b[2]==78&&b[3]==71){width=BE(b,16);height=BE(b,20);}
            else if(b.Length>4&&b[0]==255&&b[1]==216){
                int i=2;while(i+8<b.Length){if(b[i++]!=255)continue;int marker=b[i++];if(marker==255)continue;if(marker==217||marker==218)break;if(marker==1||(marker>=208&&marker<=215))continue;
                    int length=(b[i]<<8)|b[i+1];if(length<2||i+length>b.Length)break;
                    if((marker>=192&&marker<=195)||(marker>=197&&marker<=199)||(marker>=201&&marker<=203)||(marker>=205&&marker<=207)){height=(b[i+3]<<8)|b[i+4];width=(b[i+5]<<8)|b[i+6];break;}i+=length;
                }
            }
            if(width<=0||height<=0||width>4096||height>4096)throw new InvalidDataException("Dùng PNG/JPEG có kích thước tối đa 4096 × 4096.");
        }
        static int BE(byte[] b,int i)=>(b[i]<<24)|(b[i+1]<<16)|(b[i+2]<<8)|b[i+3];
    }
}
