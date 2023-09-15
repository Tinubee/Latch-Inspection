using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using BGAPI2;
using Cognex.VisionPro;
using System.Runtime.InteropServices;
using Cognex.VisionPro.Display;
using System.IO.Ports;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.Dimensioning;
using System.Drawing.Imaging;
using System.Reflection;
using KimLib;
using VISION.Camera;
using VISION.Schemas;

namespace VISION
{
    public delegate void EventCallBack(Bitmap bmp);
    public partial class Frm_Main : Form
    {
        public Log log = new Log();
        public SubForm.SubForm SubForm;
        public Thread SubTread;
        internal Frm_ToolSetUp frm_toolsetup; //툴셋업창 화면
        internal Frm_AnalyzeResult frm_analyzeresult;

        private CogImage8Grey Fiximage; //PMAlign툴의 결과이미지(픽스쳐이미지)
        private string FimageSpace; //PMAlign툴 SpaceName(보정하기위해)

        private PGgloble Glob; //전역변수

        public bool LightStats = false; //조명 상태
        //public bool[] InspectResult = new bool[6]; //검사결과.

        public Stopwatch[] InspectTime = new Stopwatch[3]; //검사시간
        public double[] OK_Count = new double[3]; //양품개수
        public double[] NG_Count = new double[3]; //NG품개수
        public double[] TOTAL_Count = new double[3]; //총개수
        public double[] NG_Rate = new double[3]; //총개수

        public bool[] InspectFlag = new bool[3]; //검사 플래그.

        #region ADLINK DIO
        //PLC <-> PC 통신 시 I/O 확인하는 변수들
        public short m_dev;
        bool[] gbool_di = new bool[16];
        bool[] re_gbool_di = new bool[16];
        bool[] gbool_do = new bool[16];
        ushort[] didata = new ushort[16];
        #endregion 
        
        public Frm_Main()
        {
            Glob = PGgloble.getInstance; //전역변수 사용
            Glob.G_MainForm = this;
            InitializeComponent();
          
        }

        public void SubFormRun()
        {
            Application.Run(SubForm);
        }

        public void SubFromStart(string title, string ver)
        {
            SubForm = new SubForm.SubForm();
            SubTread = new Thread(new ThreadStart(SubFormRun));
            SubTread.Start();
            SubForm.제목 = title;
            SubForm.프로그램버전 = $"Ver. {ver}";
        }

        public void SubFromClose()
        {
            SubForm.Close();
            //SubTread.Abort();
        }
        public void Log_OnLogEvent(object sender, LogItem e)
        {
            logControl1.ManageLog(e);
        }

        private void Frm_Main_Load(object sender, EventArgs e)
        {
            
            Glob.환경설정 = new 환경설정();
            Glob.검사도구 = new 검사도구();
            Glob.그랩제어 = new 그랩제어();
            Glob.비전제어 = new Inspection();

            Glob.환경설정.Init();
            Glob.검사도구.Init();
            Glob.비전제어.Init();

            if (!Glob.그랩제어.Init()) Debug.WriteLine("카메라 초기화 실패");

            lb_Ver.Text = $"Ver. {Glob.PROGRAM_VERSION}";

            timer_Setting.Start(); //타이머에서 계속해서 확인하는 것들
            InitializeDIO(); //IO보드 통신연결.
            log.AddLogMessage(LogType.Infomation, 0, "Vision Program Start");
        }

        //********************실질적으로 카메라가 촬영하는 부분********************//
        //#region 카메라 이미지 그랩이벤트 CAM0
        //void mDataStream_NewBufferEvent1(object sender, BGAPI2.Events.NewBufferEventArgs mDSEvent)
        //{
        //    try
        //    {
        //        if (frm_toolsetup == null) //메인화면일때.
        //        {
        //            //log.AddLogMessage(LogType.Infomation, 0, "cam1 shot start");
        //            InspectFlag[0] = true;
        //            OutPutSignal_Off(1);
        //            OutPutSignal_Off(2);
        //        }
        //        ImageProcessor imgProcessor = imgProcessor = ImageProcessor.Instance;
        //        BGAPI2.Buffer bufferFilled = null;
        //        bufferFilled = mDSEvent.BufferObj;
        //        int nBufferOffset = (int)bufferFilled.ImageOffset;
        //        if (bufferFilled == null)
        //        {
        //            //Console.Write("Error: Buffer Timeout after 1000 msec\r\n");
        //        }
        //        else if (bufferFilled != null)
        //        {
        //            m_SnapBuffer[0] = bufferFilled.MemPtr + nBufferOffset;
        //            m_SnapImage[0] = new Bitmap(m_nCamWidth[0], m_nCamHeight[0], m_nCamWidth[0], PixelFormat.Format8bppIndexed, bufferFilled.MemPtr + nBufferOffset);
        //            CreateGrayColorMap(ref m_SnapImage[0]);
        //            Monoimage[0] = new CogImage8Grey(m_SnapImage[0]);

        //            if (frm_toolsetup != null)
        //            {
        //                frm_toolsetup.cdyDisplay.Image = Monoimage[0];
        //                if (m_bSingleSnap[0] == true)
        //                {
        //                    frm_toolsetup.cdyDisplay.Fit();
        //                }
        //            }
        //            else
        //            {
        //                InspectTime[0] = new Stopwatch();
        //                InspectTime[0].Reset();
        //                InspectTime[0].Start();

        //                cdyDisplay.Image = Monoimage[0];
        //                if (m_bSingleSnap[0] == true)
        //                    cdyDisplay.Fit();
        //                if (Inspect_Cam0(cdyDisplay) == true) // 검사 결과
        //                {
        //                    //검사 결과 OK
        //                    BeginInvoke((Action)delegate
        //                    {
        //                        //DgvResult(dgv_Line1, 0, 1); //-추가된함수
        //                        lb_Cam1_Result.BackColor = Color.Lime;
        //                        lb_Cam1_Result.Text = "O K";
        //                        OK_Count[0]++;
        //                        if (Glob.OKImageSave)
        //                            ImageSave1("OK", 1, cdyDisplay);
        //                    });
        //                    OutPutSignal_On(1);
        //                }
        //                else
        //                {
        //                    BeginInvoke((Action)delegate
        //                    {
        //                        //DgvResult(dgv_Line1, 0, 1); //-추가된함수
        //                        lb_Cam1_Result.BackColor = Color.Red;
        //                        lb_Cam1_Result.Text = "N G";
        //                        NG_Count[0]++;
        //                        if (Glob.NGImageSave)
        //                            ImageSave1("NG", 1, cdyDisplay);
        //                    });
        //                    //검사 결과 NG
        //                    OutPutSignal_On(2);
        //                }

        //                InspectTime[0].Stop();
        //                InspectFlag[0] = false;

        //                BeginInvoke((Action)delegate { lb_Cam1_InsTime.Text = InspectTime[0].ElapsedMilliseconds.ToString() + "msec"; });
        //                Thread.Sleep(100);
        //            }

        //            // queue buffer again
        //            bufferFilled.QueueBuffer();
        //            if (m_bSingleSnap[0])
        //            {
        //                m_bSingleSnap[0] = false;
        //                mDevice[0].RemoteNodeList["AcquisitionStop"].Execute();
        //                mDataStream[0].StopAcquisition();
        //            }
        //        }
        //        else if (bufferFilled.IsIncomplete == true)
        //        {
        //            bufferFilled.QueueBuffer();
        //        }

        //        frames[0]++;
        //        Monoimage[0] = null;
        //        m_SnapImage[0] = null;
        //        if (frames[0] >= 2)
        //        {
        //            frames[0] = 0;
        //            GC.Collect();
        //        }
        //        return;

        //    }
        //    catch (BGAPI2.Exceptions.IException ex)
        //    {
        //        //Log(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Console.WriteLine(ex.Message);

        //    }
        //}
        //#endregion

        //#region 카메라 이미지 그랩이벤트 CAM1
        //void mDataStream_NewBufferEvent2(object sender, BGAPI2.Events.NewBufferEventArgs mDSEvent)
        //{
        //    try
        //    {
        //        if (frm_toolsetup == null)
        //        {
        //            InspectFlag[1] = true;
        //            OutPutSignal_Off(4);
        //            OutPutSignal_Off(5);
        //        }
        //        ImageProcessor imgProcessor = imgProcessor = ImageProcessor.Instance;
        //        BGAPI2.Buffer bufferFilled = null;
        //        bufferFilled = mDSEvent.BufferObj;
        //        int nBufferOffset = (int)bufferFilled.ImageOffset;
        //        if (bufferFilled == null)
        //        {
        //            //System.Console.Write("Error: Buffer Timeout after 1000 msec\r\n");
        //        }
        //        else if (bufferFilled != null)
        //        {

        //            m_SnapBuffer[1] = bufferFilled.MemPtr + nBufferOffset;
        //            m_SnapImage[1] = new Bitmap(m_nCamWidth[1], m_nCamHeight[1], m_nCamWidth[1], PixelFormat.Format8bppIndexed, bufferFilled.MemPtr + nBufferOffset);
        //            CreateGrayColorMap(ref m_SnapImage[1]);
        //            Monoimage[1] = new CogImage8Grey(m_SnapImage[1]);

        //            if (frm_toolsetup != null)
        //            {
        //                frm_toolsetup.cdyDisplay.Image = Monoimage[1];
        //                if (m_bSingleSnap[1] == true)
        //                {
        //                    frm_toolsetup.cdyDisplay.Fit();
        //                }
        //            }
        //            else
        //            {
        //                InspectTime[1] = new Stopwatch();
        //                InspectTime[1].Reset();
        //                InspectTime[1].Start();

        //                cdyDisplay2.Image = Monoimage[1];
        //                if (m_bSingleSnap[1] == true)
        //                    cdyDisplay2.Fit();
        //                if (Inspect_Cam1(cdyDisplay2) == true) // 검사 결과
        //                {
        //                    //검사 결과 OK

        //                    BeginInvoke((Action)delegate
        //                    {
        //                        lb_Cam2_Result.BackColor = Color.Lime;
        //                        lb_Cam2_Result.Text = "O K";
        //                        OK_Count[1]++;
        //                        if (Glob.OKImageSave)
        //                            ImageSave2("OK", 2, cdyDisplay2);
        //                    });
        //                    OutPutSignal_On(4);
        //                }
        //                else
        //                {
        //                    //검사 결과 NG

        //                    BeginInvoke((Action)delegate
        //                    {
        //                        lb_Cam2_Result.BackColor = Color.Red;
        //                        lb_Cam2_Result.Text = "N G";
        //                        NG_Count[1]++;
        //                        if (Glob.NGImageSave)
        //                            ImageSave2("NG", 2, cdyDisplay2);
        //                    });
        //                    OutPutSignal_On(5);
        //                }
        //                InspectTime[1].Stop();
        //                InspectFlag[1] = false;
        //                BeginInvoke((Action)delegate { lb_Cam2_InsTime.Text = InspectTime[1].ElapsedMilliseconds.ToString() + "msec"; });
        //                Thread.Sleep(100);
        //            }

        //            // queue buffer again
        //            bufferFilled.QueueBuffer();
        //            if (m_bSingleSnap[1])
        //            {
        //                m_bSingleSnap[1] = false;
        //                mDevice[1].RemoteNodeList["AcquisitionStop"].Execute();
        //                mDataStream[1].StopAcquisition();
        //            }
        //        }
        //        else if (bufferFilled.IsIncomplete == true)
        //        {
        //            System.Console.Write("Error: Image is incomplete\r\n");
        //            bufferFilled.QueueBuffer();
        //        }

        //        frames[1]++;
        //        Monoimage[1] = null;
        //        m_SnapImage[1] = null;
        //        if (frames[1] >= 2)
        //        {
        //            frames[1] = 0;
        //            GC.Collect();
        //        }
        //        return;
        //    }
        //    catch (BGAPI2.Exceptions.IException ex)
        //    {
        //        //Log(ex.Message);
        //    }

        //}
        //#endregion

        //#region 카메라 이미지 그랩이벤트 CAM2
        //void mDataStream_NewBufferEvent3(object sender, BGAPI2.Events.NewBufferEventArgs mDSEvent)
        //{
        //    try
        //    {
        //        if (frm_toolsetup == null)
        //        {
        //            InspectFlag[2] = true;
        //            OutPutSignal_Off(7);
        //            OutPutSignal_Off(8);
        //        }

        //        ImageProcessor imgProcessor = imgProcessor = ImageProcessor.Instance;
        //        BGAPI2.Buffer bufferFilled = null;
        //        bufferFilled = mDSEvent.BufferObj;
        //        int nBufferOffset = (int)bufferFilled.ImageOffset;
        //        if (bufferFilled == null)
        //        {
        //            Console.Write("Error: Buffer Timeout after 1000 msec\r\n");
        //        }
        //        else if (bufferFilled != null)
        //        {

        //            m_SnapBuffer[2] = bufferFilled.MemPtr + nBufferOffset;
        //            m_SnapImage[2] = new Bitmap(m_nCamWidth[2], m_nCamHeight[2], m_nCamWidth[2], PixelFormat.Format8bppIndexed, bufferFilled.MemPtr + nBufferOffset);
        //            CreateGrayColorMap(ref m_SnapImage[2]);

        //            Monoimage[2] = new CogImage8Grey(m_SnapImage[2]);

        //            if (frm_toolsetup != null)
        //            {
        //                frm_toolsetup.cdyDisplay.Image = Monoimage[2];
        //                if (m_bSingleSnap[2] == true)
        //                {
        //                    frm_toolsetup.cdyDisplay.Fit();
        //                }
        //            }
        //            else
        //            {
        //                InspectTime[2] = new Stopwatch();
        //                InspectTime[2].Reset();
        //                InspectTime[2].Start();

        //                cdyDisplay3.Image = Monoimage[2];
        //                if (m_bSingleSnap[2] == true)
        //                    cdyDisplay3.Fit();
        //                if (Inspect_Cam2(cdyDisplay3) == true) // 검사 결과
        //                {
        //                    //검사 결과 OK
        //                    BeginInvoke((Action)delegate
        //                    {
        //                        lb_Cam3_Result.BackColor = Color.Lime;
        //                        lb_Cam3_Result.Text = "O K";
        //                        OK_Count[2]++;
        //                        if (Glob.OKImageSave)
        //                            ImageSave3("OK", 3, cdyDisplay3);
        //                    });
        //                    OutPutSignal_On(7);
        //                }
        //                else
        //                {
        //                    //검사 결과 NG
        //                    BeginInvoke((Action)delegate
        //                    {
        //                        lb_Cam3_Result.BackColor = Color.Red;
        //                        lb_Cam3_Result.Text = "N G";
        //                        NG_Count[2]++;
        //                        if (Glob.NGImageSave)
        //                            ImageSave3("NG", 3, cdyDisplay3);
        //                    });
        //                    OutPutSignal_On(8);
        //                }
        //                InspectTime[2].Stop();
        //                InspectFlag[2] = false;
        //                BeginInvoke((Action)delegate { lb_Cam3_InsTime.Text = InspectTime[2].ElapsedMilliseconds.ToString() + "msec"; });
        //                Thread.Sleep(100);
        //            }

        //            // queue buffer again
        //            bufferFilled.QueueBuffer();
        //            if (m_bSingleSnap[2])
        //            {
        //                m_bSingleSnap[2] = false;
        //                mDevice[2].RemoteNodeList["AcquisitionStop"].Execute();
        //                mDataStream[2].StopAcquisition();
        //            }
        //        }
        //        else if (bufferFilled.IsIncomplete == true)
        //        {
        //            Console.Write("Error: Image is incomplete\r\n");
        //            bufferFilled.QueueBuffer();
        //        }

        //        frames[2]++;
        //        Monoimage[2] = null;
        //        m_SnapImage[2] = null;
        //        if (frames[2] >= 2)
        //        {
        //            frames[2] = 0;
        //            GC.Collect();
        //        }
        //        return;
        //    }
        //    catch (BGAPI2.Exceptions.IException ex)
        //    {
        //        //Log(ex.Message);
        //    }
        //}
        //#endregion

        //여러개의 카메라 동작시 겹치지 않는지...? 이벤트 중복으로 프로그램 뻗을수있을꺼같으니, 테스트 후 문제가 될 시에 이벤트 나누어주기.
        #region 카메라 동작 
        public void SnapShot(int camnumber)
        {
            Glob.그랩제어.GetItem((카메라구분)camnumber).스냅샷();
        }

        #endregion
        private void InitializeDIO()
        {
            try
            {
                //카드번호 입력.
                m_dev = DASK.Register_Card(DASK.PCI_7230, 0);
                if (m_dev < 0)
                {
                    log.AddLogMessage(LogType.Error, 0, "Register_Card error!");
                }
                else
                {
                    ushort i;
                    short result;
                    for (i = 0; i < 16; i++)
                    {
                        result = DASK.DI_ReadLine((ushort)m_dev, 0, i, out didata[i]); //InPut 읽음 (카드넘버,포트0번,In단자번호,버퍼메모리(In단자1일때 1,In단자0일때 0) 
                        if (didata[i] == 1)
                        {
                            gbool_di[i] = true;
                        }
                        else
                        {
                            gbool_di[i] = false;
                        }
                    }
                    bk_IO.RunWorkerAsync(); // IO 백그라운드 스타트
                }
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
            }
        }

        private void btn_ToolSetUp_Click(object sender, EventArgs e)
        {
            frm_toolsetup = new Frm_ToolSetUp(this);
            frm_toolsetup.Show();
        }
        private void btn_Exit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("프로그램을 종료 하시겠습니까?", "EXIT", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                return;

            INIControl setting = new INIControl(Glob.SETTING);
            DateTime dt = DateTime.Now;
            setting.WriteData("Exit Date", "Date", dt.ToString("yyyyMMdd"));
            Application.Exit();
        }

        private void btn_SystemSetup_Click(object sender, EventArgs e)
        {
            Frm_SystemSetUp FrmSystemSetUp = new Frm_SystemSetUp(this);
            FrmSystemSetUp.Show();
        }

        private void timer_Setting_Tick(object sender, EventArgs e)
        {
            DateTime dt = DateTime.Now;
            lb_Time.Text = dt.ToString("yyyy년 MM월 dd일 HH:mm:ss"); //현재날짜
            lb_CurruntModelName.Text = Glob.RunnModel.Modelname(); //현재사용중인 모델명 체크
            Glob.CurruntModelName = Glob.RunnModel.Modelname();
        }
        //Output 신호들
        public void OutPutSignal_On(int jobNo)
        {
            short ret;
            ret = DASK.DO_WriteLine((ushort)m_dev, 0, (ushort)jobNo, 1);
        }

        public void OutPutSignal_Off(int jobNo)
        {
            short ret;
            ret = DASK.DO_WriteLine((ushort)m_dev, 0, (ushort)jobNo, 0);
        }

        private void OUTPUT_Click(object sender, EventArgs e)
        {
            try
            {
                short ret;
                int jobNo = Convert.ToInt16((sender as Button).Tag);
                ret = DASK.DO_WriteLine((ushort)m_dev, 0, (ushort)jobNo, 1);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
            }
        }
        private void OUTPUTOFF_Click(object sender, EventArgs e)
        {
            try
            {
                short ret;
                int jobNo = Convert.ToInt16((sender as Button).Tag);
                ret = DASK.DO_WriteLine((ushort)m_dev, 0, (ushort)jobNo, 0);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
            }
        }
        private void bk_IO_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(200);
                if (bk_IO.CancellationPending == true) //취소요청이 들어오면 return
                {
                    return;
                }
                ushort i;
                short result;

                for (i = 0; i < 16; i++)
                {
                    re_gbool_di[i] = gbool_di[i];
                }

                for (i = 0; i < 16; i++)
                {
                    result = DASK.DI_ReadLine((ushort)m_dev, 0, i, out didata[i]); //InPut 읽음 (카드넘버,포트0번,In단자번호,버퍼메모리(In단자1일때 1,In단자0일때 0) 
                    if (didata[i] == 1)
                    {
                        gbool_di[i] = true;
                    }
                    else
                    {
                        gbool_di[i] = false;
                    }
                }
                //IO CHECK - DISPLAY 표시 부분
                BeginInvoke((Action)delegate { IOCHECK(); });
                for (i = 0; i < 16; i++)
                {
                    if (gbool_di[i] != re_gbool_di[i] && gbool_di[i] == true)
                    {
                        log.AddLogMessage(LogType.Infomation, 0, i.ToString());
                        switch (i)
                        {
                            case 0: //Line1
                                OutPutSignal_Off(1);
                                OutPutSignal_Off(2);
                                InspectFlag[0] = false;
                                log.AddLogMessage(LogType.Infomation, 0, "LINE #1 검사 결과 초기화");
                                break;
                            case 1: //Line2
                                OutPutSignal_Off(4);
                                OutPutSignal_Off(5);
                                InspectFlag[1] = false;
                                log.AddLogMessage(LogType.Infomation, 0, "LINE #2 검사 결과 초기화");
                                break;
                            case 2: //Line3
                                OutPutSignal_Off(7);
                                OutPutSignal_Off(8);
                                InspectFlag[2] = false;
                                log.AddLogMessage(LogType.Infomation, 0, "LINE #3 검사 결과 초기화");
                                break;
                            case 7:
                                log.AddLogMessage(LogType.Infomation, 0, "Line #1 Vision Trigger");
                                if (InspectFlag[0] == false)
                                {
                                    //cdyDisplay.Image = null;
                                    //cdyDisplay.InteractiveGraphics.Clear();
                                    //cdyDisplay.StaticGraphics.Clear();
                                    //BeginInvoke((Action)delegate
                                    //{
                                    //    lb_Cam1_Result.Text = "Result";
                                    //    lb_Cam1_Result.BackColor = SystemColors.Control;
                                    //});

                                    Task.Run(() => { Glob.그랩제어.GetItem(카메라구분.Cam01).스냅샷(); });
                                }
                                else
                                {
                                    OutPutSignal_Off(1);
                                    OutPutSignal_Off(2);
                                    InspectFlag[0] = false;
                                    log.AddLogMessage(LogType.Infomation, 0, "LINE #1 검사 결과 초기화");
                                }
                                break;
                            case 10:
                                OutPutSignal_Off(4);
                                OutPutSignal_Off(5);
                                log.AddLogMessage(LogType.Infomation, 0, "Line #2 Vision Trigger");
                                if (InspectFlag[1] == false)
                                {
                                    //cdyDisplay2.Image = null;
                                    //cdyDisplay2.InteractiveGraphics.Clear();
                                    //cdyDisplay2.StaticGraphics.Clear();
                                    //BeginInvoke((Action)delegate
                                    //{
                                    //    lb_Cam2_Result.Text = "Result";
                                    //    lb_Cam2_Result.BackColor = SystemColors.Control;
                                    //});
                                    Task.Run(() => { Glob.그랩제어.GetItem(카메라구분.Cam02).스냅샷(); });
                                }
                                else
                                {
                                    OutPutSignal_Off(4);
                                    OutPutSignal_Off(5);
                                    InspectFlag[1] = false;
                                    log.AddLogMessage(LogType.Infomation, 0, "LINE #2 검사 결과 초기화");
                                }
                                break;
                            case 13:
                                OutPutSignal_Off(7);
                                OutPutSignal_Off(8);
                                log.AddLogMessage(LogType.Infomation, 0, "Line #3 Vision Trigger");
                                if (InspectFlag[2] == false)
                                {
                                    //cdyDisplay3.Image = null;
                                    //cdyDisplay3.InteractiveGraphics.Clear();
                                    //cdyDisplay3.StaticGraphics.Clear();
                                    //BeginInvoke((Action)delegate
                                    //{
                                    //    lb_Cam3_Result.Text = "Result";
                                    //    lb_Cam3_Result.BackColor = SystemColors.Control;
                                    //});
                                    Task.Run(() => { Glob.그랩제어.GetItem(카메라구분.Cam03).스냅샷(); });
                                }
                                else
                                {
                                    OutPutSignal_Off(7);
                                    OutPutSignal_Off(8);
                                    InspectFlag[2] = false;
                                    log.AddLogMessage(LogType.Infomation, 0, "LINE #3 검사 결과 초기화");
                                }
                                break;
                        }
                    }
                }
            }
        }
        private void IOCHECK()
        {
            btn_INPUT7.BackColor = gbool_di[7] == true ? Color.Lime : SystemColors.Control;
            btn_INPUT8.BackColor = gbool_di[8] == true ? Color.Lime : SystemColors.Control;
            btn_INPUT9.BackColor = gbool_di[9] == true ? Color.Lime : SystemColors.Control;
            btn_INPUT10.BackColor = gbool_di[10] == true ? Color.Lime : SystemColors.Control;
            btn_INPUT11.BackColor = gbool_di[11] == true ? Color.Lime : SystemColors.Control;
            btn_INPUT12.BackColor = gbool_di[12] == true ? Color.Lime : SystemColors.Control;
            btn_INPUT13.BackColor = gbool_di[13] == true ? Color.Lime : SystemColors.Control;
            btn_INPUT14.BackColor = gbool_di[14] == true ? Color.Lime : SystemColors.Control;
        }
        private void Frm_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bk_IO.IsBusy == true)
            {
                bk_IO.CancelAsync();
            }
            if (LightStats == true)
            {
                LightOFF();
            }
            try
            {
                //for (int i = 0; i < camcount; i++)
                //{
                //    if (mDevice[i] != null)
                //    {
                //        mDevice[i].RemoteNodeList["AcquisitionStop"].Execute();
                //        mDataStream[i].StopAcquisition();
                //    }
                //}
            }
            catch (BGAPI2.Exceptions.IException ex)
            {
                Console.Write("ExceptionType:    {0} \r\n", ex.GetType());
                Console.Write("ErrorDescription: {0} \r\n", ex.GetErrorDescription());
                Console.Write("in function:      {0} \r\n", ex.GetFunctionName());
            }
        }

        private void btn_Status_Click(object sender, EventArgs e)
        {
            log.AddLogMessage(LogType.Infomation, 0, "AUTO MODE START");
            btn_Status.Enabled = false;
            btn_ToolSetUp.Enabled = false;
            btn_Model.Enabled = false;
            btn_SystemSetup.Enabled = false;
            btn_Stop.Enabled = true;
            OutPutSignal_On(0); //LINE #1 VISION READY ON 
            OutPutSignal_On(3); //LINE #2 VISION READY ON 
            OutPutSignal_On(6); //LINE #3 VISION READY ON 
            for (int num = 1; num < 3; num++)
            {
                //LINE #1 검사 결과 초기화
                OutPutSignal_Off(num);
            }
            for (int num = 4; num < 6; num++)
            {
                //LINE #2 검사 결과 초기화
                OutPutSignal_Off(num);
            }
            for (int num = 7; num < 9; num++)
            {
                //LINE #3 검사 결과 초기화
                OutPutSignal_Off(num);
            }
            if (bk_IO.IsBusy == false) //I/O 백그라운드가 돌고있지 않으면.
            {
                //I/O 백드라운드 동작 시작.
                bk_IO.RunWorkerAsync();
            }
        }
        public void DisplayLabelShow(CogGraphicCollection Collection, CogDisplay cog, int X, int Y, string Text)
        {
            CogCreateGraphicLabelTool Label = new CogCreateGraphicLabelTool();
            Label.InputGraphicLabel.Color = CogColorConstants.Green;
            Label.InputImage = cog.Image;
            Label.InputGraphicLabel.X = X;
            Label.InputGraphicLabel.Y = Y;
            Label.InputGraphicLabel.Text = Text;
            Label.Run();
            Collection.Add(Label.GetOutputGraphicLabel());
        }
        public void Bolb_Train(CogDisplay cdy, int CameraNumber, int toolnumber)
        {
            if (Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].Run((CogImage8Grey)cdy.Image) == true)
            {
                int usePatternNumber = Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].HighestResultToolNumber();
                Fiximage = Glob.코그넥스파일.모델.Blob_FixtureImage1((CogImage8Grey)cdy.Image, Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].ResultPoint(usePatternNumber), Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].ToolName(), CameraNumber, toolnumber, out FimageSpace, usePatternNumber);
            }
        }

        public void DgvResult(DataGridView dgv, int camnumber, int cellnumber)
        {
            if (frm_toolsetup != null)
            {
                for (int i = 0; i < 30; i++)
                {
                    int patternIndex = Glob.코그넥스파일.블롭툴픽스쳐번호[camnumber, i];
                    if (Glob.코그넥스파일.블롭툴사용여부[camnumber, i] == true)
                    {
                        if (Glob.코그넥스파일.블롭툴[camnumber, i].ResultBlobCount() != Glob.코그넥스파일.블롭툴양품갯수[camnumber, i]) // - 검사결과 NG
                        {
                            dgv.Rows[i].Cells[3].Value = $"{Glob.코그넥스파일.블롭툴[camnumber, i].ResultBlobCount()}-({Glob.코그넥스파일.블롭툴양품갯수[camnumber, i]})";
                            dgv.Rows[i].Cells[3].Style.BackColor = Color.Red;
                        }
                        else // - 검사결과 OK
                        {
                            dgv.Rows[i].Cells[3].Value = $"{Glob.코그넥스파일.블롭툴[camnumber, i].ResultBlobCount()}-({Glob.코그넥스파일.블롭툴양품갯수[camnumber, i]})";
                            dgv.Rows[i].Cells[3].Style.BackColor = Color.Lime;
                        }
                    }
                    else
                    {
                        dgv.Rows[i].Cells[3].Value = "NOT USED";
                        dgv.Rows[i].Cells[3].Style.BackColor = SystemColors.Control;
                    }
                }
                for (int i = 0; i < 30; i++)
                {
                    if (Glob.코그넥스파일.패턴툴사용여부[camnumber, i] == true)
                    {
                        if (Glob.코그넥스파일.패턴툴[camnumber, i].ResultPoint(Glob.코그넥스파일.패턴툴[camnumber, i].HighestResultToolNumber()) != null)
                        {
                            if (Glob.MultiInsPat_Result[camnumber, i] > Glob.코그넥스파일.패턴툴[camnumber, i].Threshold() * 100)
                            {
                                dgv.Rows[i].Cells[cellnumber].Value = Glob.MultiInsPat_Result[camnumber, i].ToString("F2");
                                dgv.Rows[i].Cells[cellnumber].Style.BackColor = Color.Lime;
                            }
                            else
                            {
                                dgv.Rows[i].Cells[cellnumber].Value = Glob.MultiInsPat_Result[camnumber, i].ToString("F2");
                                dgv.Rows[i].Cells[cellnumber].Style.BackColor = Color.Red;
                                Glob.InspectResult[camnumber] = false;
                            }
                        }
                        else
                        {
                            dgv.Rows[i].Cells[cellnumber].Value = "NG";
                            dgv.Rows[i].Cells[cellnumber].Style.BackColor = Color.Red;
                            Glob.InspectResult[camnumber] = false;
                        }
                    }
                    else
                    {
                        dgv.Rows[i].Cells[cellnumber].Value = "NOT USED";
                        dgv.Rows[i].Cells[cellnumber].Style.BackColor = SystemColors.Control;
                    }
                }
            }
            else
            {

            }

        }

        public void ImageSave1(string Result, int CamNumber, CogDisplay cog)
        {
            //NG 이미지와 OK 이미지 구별이 필요할 것 같음 - 따로 요청이 없어서 구별해놓진 않음
            try
            {
                CogImageFileJPEG ImageSave = new CogImageFileJPEG();
                DateTime dt = DateTime.Now;
                string Root = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}";
                string Root2 = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}Display";

                if (!Directory.Exists(Root))
                {
                    Directory.CreateDirectory(Root);
                }
                if (!Directory.Exists(Root2))
                {
                    Directory.CreateDirectory(Root2);
                }
                ImageSave.Open(Root + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}" + ".jpg", CogImageFileModeConstants.Write);
                ImageSave.Append(cog.Image);
                ImageSave.Close();

                cog.CreateContentBitmap(CogDisplayContentBitmapConstants.Custom).Save(Root2 + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}", ImageFormat.Jpeg);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
                //cm.info(ee.Message);
            }
        }
        public void ImageSave2(string Result, int CamNumber, CogDisplay cog)
        {
            //NG 이미지와 OK 이미지 구별이 필요할 것 같음 - 따로 요청이 없어서 구별해놓진 않음
            try
            {
                CogImageFileJPEG ImageSave = new Cognex.VisionPro.ImageFile.CogImageFileJPEG();
                DateTime dt = DateTime.Now;
                string Root = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}";
                string Root2 = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}Display";

                if (!Directory.Exists(Root))
                {
                    Directory.CreateDirectory(Root);
                }
                if (!Directory.Exists(Root2))
                {
                    Directory.CreateDirectory(Root2);
                }
                ImageSave.Open(Root + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}" + ".jpg", CogImageFileModeConstants.Write);
                ImageSave.Append(cog.Image);
                ImageSave.Close();

                cog.CreateContentBitmap(CogDisplayContentBitmapConstants.Custom).Save(Root2 + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}", ImageFormat.Jpeg);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
                //cm.info(ee.Message);
            }
        }
        public void ImageSave3(string Result, int CamNumber, CogDisplay cog)
        {
            //NG 이미지와 OK 이미지 구별이 필요할 것 같음 - 따로 요청이 없어서 구별해놓진 않음
            try
            {
                CogImageFileJPEG ImageSave = new Cognex.VisionPro.ImageFile.CogImageFileJPEG();
                DateTime dt = DateTime.Now;
                string Root = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}";
                string Root2 = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}Display";

                if (!Directory.Exists(Root))
                {
                    Directory.CreateDirectory(Root);
                }
                if (!Directory.Exists(Root2))
                {
                    Directory.CreateDirectory(Root2);
                }
                ImageSave.Open(Root + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}" + ".jpg", CogImageFileModeConstants.Write);
                ImageSave.Append(cog.Image);
                ImageSave.Close();

                cog.CreateContentBitmap(CogDisplayContentBitmapConstants.Custom).Save(Root2 + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}", ImageFormat.Jpeg);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
                //cm.info(ee.Message);
            }
        }
        public void ImageSave4(string Result, int CamNumber, CogDisplay cog)
        {
            //NG 이미지와 OK 이미지 구별이 필요할 것 같음 - 따로 요청이 없어서 구별해놓진 않음
            try
            {
                CogImageFileJPEG ImageSave = new Cognex.VisionPro.ImageFile.CogImageFileJPEG();
                DateTime dt = DateTime.Now;
                string Root = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}";
                string Root2 = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}Display";

                if (!Directory.Exists(Root))
                {
                    Directory.CreateDirectory(Root);
                }
                if (!Directory.Exists(Root2))
                {
                    Directory.CreateDirectory(Root2);
                }
                ImageSave.Open(Root + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}" + ".jpg", CogImageFileModeConstants.Write);
                ImageSave.Append(cog.Image);
                ImageSave.Close();

                cog.CreateContentBitmap(CogDisplayContentBitmapConstants.Custom).Save(Root2 + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}", ImageFormat.Jpeg);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
                //cm.info(ee.Message);
            }
        }
        public void ImageSave5(string Result, int CamNumber, CogDisplay cog)
        {
            //NG 이미지와 OK 이미지 구별이 필요할 것 같음 - 따로 요청이 없어서 구별해놓진 않음
            try
            {
                CogImageFileJPEG ImageSave = new Cognex.VisionPro.ImageFile.CogImageFileJPEG();
                DateTime dt = DateTime.Now;
                string Root = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}";
                string Root2 = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}Display";

                if (!Directory.Exists(Root))
                {
                    Directory.CreateDirectory(Root);
                }
                if (!Directory.Exists(Root2))
                {
                    Directory.CreateDirectory(Root2);
                }
                ImageSave.Open(Root + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}" + ".jpg", CogImageFileModeConstants.Write);
                ImageSave.Append(cog.Image);
                ImageSave.Close();

                cog.CreateContentBitmap(CogDisplayContentBitmapConstants.Custom).Save(Root2 + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}", ImageFormat.Jpeg);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
                //cm.info(ee.Message);
            }
        }
        public void ImageSave6(string Result, int CamNumber, CogDisplay cog)
        {
            //NG 이미지와 OK 이미지 구별이 필요할 것 같음 - 따로 요청이 없어서 구별해놓진 않음
            try
            {
                CogImageFileJPEG ImageSave = new Cognex.VisionPro.ImageFile.CogImageFileJPEG();
                DateTime dt = DateTime.Now;
                string Root = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}";
                string Root2 = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}Display";

                if (!Directory.Exists(Root))
                {
                    Directory.CreateDirectory(Root);
                }
                if (!Directory.Exists(Root2))
                {
                    Directory.CreateDirectory(Root2);
                }
                ImageSave.Open(Root + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}" + ".jpg", CogImageFileModeConstants.Write);
                ImageSave.Append(cog.Image);
                ImageSave.Close();

                cog.CreateContentBitmap(CogDisplayContentBitmapConstants.Custom).Save(Root2 + $@"\{dt.ToString("yyyyMMdd-HH mm ss")}" + $"_{Result}", ImageFormat.Jpeg);
            }
            catch (Exception ee)
            {
                log.AddLogMessage(LogType.Error, 0, $"{ee.Message}");
                //cm.info(ee.Message);
            }
        }

        public void DataSave1(string Time, int CamNumber)
        {
            //DATA 저장부분 TEST 후 적용 시키기.
            DateTime dt = DateTime.Now;
            string Root = Glob.DataSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}";
            StreamWriter Writer;
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
            Root += $@"\Data_{dt.ToString("yyyyMMdd-HH")}.csv";
            Writer = new StreamWriter(Root, true);
            Writer.WriteLine($"Time,{Time}");
            //Writer.WriteLine($"Time,{dt.ToString("yyyyMMdd_HH mm ss")},CAM1,Point2.4,{Glob.CAM_Point1Value[0]},{Glob.CAM_Point2Value[0]},Point3.3,{Glob.CAM_Point3Value[0]},CAM3,Point2.4,{Glob.CAM_Point1Value[2]},{Glob.CAM_Point2Value[2]},Point3.3,{Glob.CAM_Point3Value[2]}");
            Writer.Close();
        }
        public void DataSave2(string Time, int CamNumber)
        {
            //DATA 저장부분 TEST 후 적용 시키기.
            DateTime dt = DateTime.Now;
            string Root = Glob.DataSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}";
            StreamWriter Writer;
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
            Root += $@"\Data_{dt.ToString("yyyyMMdd-HH")}.csv";
            Writer = new StreamWriter(Root, true);
            Writer.WriteLine($"Time,{Time}");
            //Writer.WriteLine($"Time,{dt.ToString("yyyyMMdd_HH mm ss")},CAM1,Point2.4,{Glob.CAM_Point1Value[0]},{Glob.CAM_Point2Value[0]},Point3.3,{Glob.CAM_Point3Value[0]},CAM3,Point2.4,{Glob.CAM_Point1Value[2]},{Glob.CAM_Point2Value[2]},Point3.3,{Glob.CAM_Point3Value[2]}");
            Writer.Close();
        }
        public void DataSave3(string Time, int CamNumber)
        {
            //DATA 저장부분 TEST 후 적용 시키기.
            DateTime dt = DateTime.Now;
            string Root = Glob.DataSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}";
            StreamWriter Writer;
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
            Root += $@"\Data_{dt.ToString("yyyyMMdd-HH")}.csv";
            Writer = new StreamWriter(Root, true);
            Writer.WriteLine($"Time,{Time}");
            //Writer.WriteLine($"Time,{dt.ToString("yyyyMMdd_HH mm ss")},CAM1,Point2.4,{Glob.CAM_Point1Value[0]},{Glob.CAM_Point2Value[0]},Point3.3,{Glob.CAM_Point3Value[0]},CAM3,Point2.4,{Glob.CAM_Point1Value[2]},{Glob.CAM_Point2Value[2]},Point3.3,{Glob.CAM_Point3Value[2]}");
            Writer.Close();
        }
        public void DataSave4(string Time, int CamNumber)
        {
            //DATA 저장부분 TEST 후 적용 시키기.
            DateTime dt = DateTime.Now;
            string Root = Glob.DataSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}";
            StreamWriter Writer;
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
            Root += $@"\Data_{dt.ToString("yyyyMMdd-HH")}.csv";
            Writer = new StreamWriter(Root, true);
            Writer.WriteLine($"Time,{Time}");
            //Writer.WriteLine($"Time,{dt.ToString("yyyyMMdd_HH mm ss")},CAM1,Point2.4,{Glob.CAM_Point1Value[0]},{Glob.CAM_Point2Value[0]},Point3.3,{Glob.CAM_Point3Value[0]},CAM3,Point2.4,{Glob.CAM_Point1Value[2]},{Glob.CAM_Point2Value[2]},Point3.3,{Glob.CAM_Point3Value[2]}");
            Writer.Close();
        }
        public void DataSave5(string Time, int CamNumber)
        {
            //DATA 저장부분 TEST 후 적용 시키기.
            DateTime dt = DateTime.Now;
            string Root = Glob.DataSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}";
            StreamWriter Writer;
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
            Root += $@"\Data_{dt.ToString("yyyyMMdd-HH")}.csv";
            Writer = new StreamWriter(Root, true);
            Writer.WriteLine($"Time,{Time}");
            //Writer.WriteLine($"Time,{dt.ToString("yyyyMMdd_HH mm ss")},CAM1,Point2.4,{Glob.CAM_Point1Value[0]},{Glob.CAM_Point2Value[0]},Point3.3,{Glob.CAM_Point3Value[0]},CAM3,Point2.4,{Glob.CAM_Point1Value[2]},{Glob.CAM_Point2Value[2]},Point3.3,{Glob.CAM_Point3Value[2]}");
            Writer.Close();
        }
        public void DataSave6(string Time, int CamNumber)
        {
            //DATA 저장부분 TEST 후 적용 시키기.
            DateTime dt = DateTime.Now;
            string Root = Glob.DataSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}";
            StreamWriter Writer;
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
            Root += $@"\Data_{dt.ToString("yyyyMMdd-HH")}.csv";
            Writer = new StreamWriter(Root, true);
            Writer.WriteLine($"Time,{Time}");
            //Writer.WriteLine($"Time,{dt.ToString("yyyyMMdd_HH mm ss")},CAM1,Point2.4,{Glob.CAM_Point1Value[0]},{Glob.CAM_Point2Value[0]},Point3.3,{Glob.CAM_Point3Value[0]},CAM3,Point2.4,{Glob.CAM_Point1Value[2]},{Glob.CAM_Point2Value[2]},Point3.3,{Glob.CAM_Point3Value[2]}");
            Writer.Close();
        }
        public void ErrorLogSave()
        {
            DateTime dt = DateTime.Now;
            string Root = Glob.DataSaveRoot;
            StreamWriter Writer;
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
            Root += $@"\ErrorLog_{dt.ToString("yyyyMMdd-HH")}.csv";
            Writer = new StreamWriter(Root, true);
            Writer.WriteLine($"Time,{dt.ToString("yyyyMMdd_HH mm ss")}");
            Writer.Close();
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            OutPutSignal_Off(0);
            OutPutSignal_Off(3);
            OutPutSignal_Off(6);
            btn_Stop.Enabled = false;
            btn_ToolSetUp.Enabled = true;
            btn_Model.Enabled = true;
            btn_SystemSetup.Enabled = true;
            btn_Status.Enabled = true;
        }

        private void btn_Model_Click(object sender, EventArgs e)
        {
            //MODEL FORM 열기.
            Frm_Model frm_model = new Frm_Model(Glob.RunnModel.Modelname(), this);
            frm_model.Show();
        }

        public void LightON()
        {
            if (LightControl.IsOpen == false)
            {
                return;
            }
            LightStats = true;
            string LightValue = string.Format("{0:D3}", 255);
            string LightValue2 = string.Format("{0:D3}", 255);
            LightValue = ":L" + 1 + LightValue + "\r\n";
            LightValue2 = ":L" + 2 + LightValue2 + "\r\n";
            LightControl.Write(LightValue.ToCharArray(), 0, LightValue.ToCharArray().Length);
            LightControl.Write(LightValue2.ToCharArray(), 0, LightValue2.ToCharArray().Length);
        }
        public void LightOFF()
        {
            if (LightControl.IsOpen == false)
            {
                return;
            }
            LightStats = false;
            string LightValue = string.Format("{0:D3}", 0);
            string LightValue2 = string.Format("{0:D3}", 0);
            LightValue = ":L" + 1 + LightValue + "\r\n";
            LightValue2 = ":L" + 2 + LightValue2 + "\r\n";
            LightControl.Write(LightValue.ToCharArray(), 0, LightValue.ToCharArray().Length);
            LightControl.Write(LightValue2.ToCharArray(), 0, LightValue2.ToCharArray().Length);
        }

        private void Frm_Main_KeyDown(object sender, KeyEventArgs e)
        {
            //****************************단축키 모음****************************//
            if (e.Control && e.KeyCode == Keys.T) //ctrl + t : 툴셋팅창 열기
                btn_ToolSetUp.PerformClick();
            if (e.Control && e.KeyCode == Keys.M) //ctrl + m : 모델창 열기
                btn_Model.PerformClick();
            if (e.KeyCode == Keys.Escape) // esc : 프로그램 종료
                btn_Exit.PerformClick();
        }

        private void btn_Analyze_Click(object sender, EventArgs e)
        {
            frm_analyzeresult = new Frm_AnalyzeResult(this);
            frm_analyzeresult.Show();
        }

        private void bk_AutoDelete_DoWork(object sender, DoWorkEventArgs e)
        {
            //*************************************************************Image저장경로*************************************************************//
            //string Root = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}";
            //string Root2 = Glob.ImageSaveRoot + $@"\{Glob.CurruntModelName}\{dt.ToString("yyyyMMdd")}\CAM{CamNumber}\{Result}Display";
            //******************************************************************************************************************************************//

            while (true)
            {
                try
                {
                    if (bk_AutoDelete.CancellationPending)
                        return;

                    DirectoryInfo di = new DirectoryInfo(Glob.ImageSaveRoot);
                    if (di.Exists)
                    {
                        DirectoryInfo[] dirInfo = di.GetDirectories();
                        string Date = DateTime.Now.AddDays(-Glob.ImageSaveDay).ToString("yyyyMMdd");
                        foreach (DirectoryInfo dir in dirInfo)
                        {
                            if (Date.CompareTo(dir.CreationTime.ToString("yyyyMMdd")) > 0)
                            {
                                string DeleteName = dir.Name;
                                dir.Attributes = FileAttributes.Normal;
                                dir.Delete(true);
                                log.AddLogMessage(LogType.Infomation, 0, $"{DeleteName} IMAGE 폴더 삭제 완료");
                            }
                        }
                    }
                }
                catch (Exception ee)
                {
                    log.AddLogMessage(LogType.Error, 0, ee.Message);
                }
            }
        }
    }

    public static class ExtensionMethods
    {
        public static void DoubleBuffered(this DataGridView dgv, bool setting)
        {
            Type dgvtype = dgv.GetType();
            PropertyInfo pi = dgvtype.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dgv, setting, null);
        }
    }
}
