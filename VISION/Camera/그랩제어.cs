using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BGAPI2;
using KimLib;
using Cognex.VisionPro;
using System.Diagnostics;
using Cognex.VisionPro.Display;
using Microsoft.VisualBasic;
using System.IO;
using Newtonsoft.Json;
using VISION.Cogs;
using VISION.Schemas;

namespace VISION.Camera
{

    public enum 카메라구분
    {
        None,
        Cam01 = 1,
        Cam02 = 2,
        Cam03 = 3,
    }

    public class 그랩제어 : Dictionary<카메라구분, 카메라장치>
    {
        readonly PGgloble Glob = PGgloble.getInstance;
        private string 저장파일 { get { return Path.Combine(Glob.기본경로, "Camera.json"); } }

        public 바우머GigE 카메라1 = null;
        public 바우머GigE 카메라2 = null;
        public 바우머GigE 카메라3 = null;

        public Boolean Init()
        {
            this.카메라1 = new 바우머GigE() { 구분 = 카메라구분.Cam01, 시리얼번호 = "8785031618" };
            this.카메라2 = new 바우머GigE() { 구분 = 카메라구분.Cam02, 시리얼번호 = "8782811518" };
            this.카메라3 = new 바우머GigE() { 구분 = 카메라구분.Cam02, 시리얼번호 = "8846662418" };

            this.Add(카메라구분.Cam01, this.카메라1);
            this.Add(카메라구분.Cam02, this.카메라2);
            this.Add(카메라구분.Cam03, this.카메라3);

            카메라장치 정보;
            List<카메라장치> 자료 = Load();
            if (자료 != null)
            {
                foreach (카메라장치 설정 in 자료)
                {
                    정보 = this.GetItem(설정.구분);
                    if (정보 == null) continue;
                    정보.Set(설정);
                }
            }

            DeviceList 카메라들 = 연결된카메라불러오기();

            if (카메라들 == null) return false;

            foreach (KeyValuePair<string, Device> 카메라 in 카메라들)
            {
                if (카메라.Value.IsOpen == false)
                {
                    카메라.Value.Open();
                }
                if (카메라.Value.SerialNumber == this.카메라1.시리얼번호)
                {
                    this.카메라1.Open(카메라들[카메라.Key]);
                }
                if (카메라.Value.SerialNumber == this.카메라2.시리얼번호)
                    this.카메라2.Open(카메라들[카메라.Key]);
                if (카메라.Value.SerialNumber == this.카메라3.시리얼번호)
                    this.카메라3.Open(카메라들[카메라.Key]);
            }
            GC.Collect();
            return true;
        }

        private List<카메라장치> Load()
        {
            if (!File.Exists(this.저장파일))
            {
                if (Utils.WriteAllText(저장파일, JsonConvert.SerializeObject(this.Values, Utils.JsonSetting())))
                {
                    Debug.WriteLine("기본파일 생성");
                }
            }
            return JsonConvert.DeserializeObject<List<카메라장치>>(File.ReadAllText(this.저장파일), Utils.JsonSetting());
        }

        public void Save()
        {
            if (!Utils.WriteAllText(저장파일, JsonConvert.SerializeObject(this.Values, Utils.JsonSetting())))
                Debug.WriteLine("카메라설정 저장 에러");
        }

        public void Close()
        {
            this.Save();
            foreach (카메라장치 장치 in this.Values)
                장치?.Close();
        }
       
        public void 그랩완료(카메라구분 카메라, CogImage8Grey 이미지, bool 라이브모드)
        {
            if (라이브모드)
            {
                //그냥 화면에 이미지 계속 표시해주기
            }
            else
            {
                //이미지 가지고 검사로직 돌리기.
            }
        }


        public 카메라장치 GetItem(카메라구분 구분)
        {
            if (this.ContainsKey(구분)) return this[구분];
            return null;
        }

        public DeviceList 연결된카메라불러오기()
        {
            try
            {
                SystemList.Instance.Refresh();
                DeviceList deviceList = new DeviceList();

                foreach (KeyValuePair<string, BGAPI2.System> sys_pair in SystemList.Instance)
                {
                    if (sys_pair.Value.IsOpen == false)
                    {
                        sys_pair.Value.Open();
                    }
                    InterfaceList interfaceList = sys_pair.Value.Interfaces;


                    interfaceList.Refresh(100);

                    if (interfaceList.Count <= 0)
                    {
                        sys_pair.Value.Close();
                        continue;
                    }
                    foreach (KeyValuePair<string, Interface> ifc_pair in interfaceList)
                    {
                        if (ifc_pair.Value.IsOpen == false)
                        {
                            ifc_pair.Value.Open();
                        }

                        deviceList = ifc_pair.Value.Devices;
                        deviceList.Refresh(100);

                        if (deviceList.Count <= 0)
                        {
                            ifc_pair.Value.Close();
                            continue;
                        }
                    }
                }
                return deviceList;
            }
            catch (Exception ee)
            {
                Debug.WriteLine($"카메라 리스트 불러오기 실패 : {ee.Message}");
                return null;
            }
        }
    }

    public class 바우머GigE : 카메라장치
    {
        public int maxBufferList = 10;
        public DataStreamList datastreamList = null;
        //public DataStream mDataStream = null;
        public BufferList bufferList = null;
        internal BGAPI2.Buffer mBuffer = null;
        string streamId = string.Empty;
        public IntPtr m_SnapBuffer;
        public bool 라이브모드 = false;

        public Boolean Open(Device info)
        {
            this.디바이스 = info;
            datastreamList = 디바이스.DataStreams;
            datastreamList.Refresh();

            foreach (KeyValuePair<string, DataStream> dst_pair in datastreamList)
            {
                dst_pair.Value.Open();
                streamId = dst_pair.Key;
                break;
            }

            if (streamId == "") 디바이스.Close();
            else
            {
                this.데이터스트림 = this.datastreamList[streamId];
                this.데이터스트림.RegisterNewBufferEvent(BGAPI2.Events.EventMode.EVENT_HANDLER);
                this.데이터스트림.NewBufferEvent += new BGAPI2.Events.DataStreamEventControl.NewBufferEventHandler(카메라그랩이벤트);
            }

            this.bufferList = this.데이터스트림.BufferList;

            this.mBuffer = new BGAPI2.Buffer(this);
            bufferList.Add(this.mBuffer);
            this.mBuffer.QueueBuffer();

            string heightValue = this.디바이스.RemoteNodeList["Height"].Value.ToString();
            string widthValue = this.디바이스.RemoteNodeList["Width"].Value.ToString();

            if (int.TryParse(heightValue, out int h)) 세로 = h;
            if (int.TryParse(widthValue, out int w)) 가로 = w;

            디바이스.RemoteNodeList["AcquisitionStop"].Execute();
            데이터스트림.StopAcquisition();
            Thread.Sleep(200);
            디바이스.RemoteNodeList["ExposureTimeAbs"].Value = this.노출값;
            디바이스.RemoteNodeList["GainAbs"].Value = this.Gain;
            Thread.Sleep(50);
           
            return true;
        }
        public override Boolean Close()
        {
            this.디바이스.RemoteNodeList["AcquisitionStop"].Execute();
            this.데이터스트림.StopAcquisition();
            return true;
        }
        public override bool 라이브종료()
        {
            try
            {
                this.디바이스.RemoteNodeList["AcquisitionStop"].Execute();
                this.데이터스트림.StopAcquisition();
                this.라이브모드 = false;
                return true;
            }
            catch (BGAPI2.Exceptions.IException ee)
            {
                Debug.WriteLine(ee.Message);
                return false;
            }
        }

        public override bool 라이브시작()
        {
            try
            {
                if (this.데이터스트림.IsGrabbing)
                {
                    this.디바이스.RemoteNodeList["AcquisitionStop"].Execute();
                    this.데이터스트림.StopAcquisition();
                    return false;
                }
                this.라이브모드 = true;
                this.데이터스트림.StartAcquisition(1);
                this.디바이스.RemoteNodeList["AcquisitionStart"].Execute();

                return true;
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
                return false;
            }
        }


        public override Boolean 스냅샷()
        {
            try
            {
                if (this.데이터스트림.IsGrabbing)
                {
                    this.디바이스.RemoteNodeList["AcquisitionStop"].Execute();
                    this.데이터스트림.StopAcquisition();
                    return false;
                }
                this.라이브모드 = false;
                this.데이터스트림.StartAcquisition(1);
                this.디바이스.RemoteNodeList["AcquisitionStart"].Execute();

                return true;
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
                return false;
            }

        }

        void 카메라그랩이벤트(object sender, BGAPI2.Events.NewBufferEventArgs mDSEvent)
        {
            ImageProcessor imgProcessor = imgProcessor = ImageProcessor.Instance;
            BGAPI2.Buffer bufferFilled = null;
            bufferFilled = mDSEvent.BufferObj;
            int nBufferOffset = (int)bufferFilled.ImageOffset;
            if (bufferFilled == null)
            {
                //Debug..WriteLine("Error: Buffer Timeout after 1000 msec\r\n");
            }
            else if (bufferFilled != null)
            {
                Bitmap m_SnapImage;
                CogImage8Grey Monoimage; //흑백이미지
                m_SnapBuffer = bufferFilled.MemPtr + nBufferOffset;
                m_SnapImage = new Bitmap(this.가로, this.세로, this.가로, PixelFormat.Format8bppIndexed, bufferFilled.MemPtr + nBufferOffset);
                CreateGrayColorMap(ref m_SnapImage);
                Monoimage = new CogImage8Grey(m_SnapImage);

                PGgloble.getInstance.그랩제어.그랩완료(this.구분, Monoimage, this.라이브모드);
            }
        }

        internal void CreateGrayColorMap(ref Bitmap image)
        {
            ColorPalette pal = image.Palette;
            Color[] entries = pal.Entries;
            for (int i = 0; i < 256; i++)
            {
                entries[i] = Color.FromArgb(i, i, i);
            }
            image.Palette = pal;
        }
    }



    public class 카메라장치
    {
        public 카메라구분 구분 { get; set; } = 카메라구분.None;

        public Device 디바이스 { get; set; } = null;

        public DataStream 데이터스트림 { get; set; } = null;
        public string 시리얼번호 { get; set; } = string.Empty;
        public int 버퍼번호 { get; set; } = 0;
        internal int 가로 { get; set; } = 0;
        internal int 세로 { get; set; } = 0;
        public int 프레임 { get; set; } = 0;
        public double 노출값 { get; set; } = 0;
        public double Gain { get; set; } = 0;
        public bool 그랩상태 { get; set; } = false;

        public virtual void Set(카메라장치 장치)
        {
            if (장치 == null) return;
            this.노출값 = 장치.노출값;
            this.Gain = 장치.Gain;
            this.가로 = 장치.가로;
            this.세로 = 장치.세로;
            this.그랩상태 = 장치.그랩상태;
        }

        public virtual Boolean 스냅샷() => false;

        public virtual Boolean 라이브시작() => false;

        public virtual Boolean 라이브종료() => false;

        public virtual Boolean Close() => false;
    }
}
