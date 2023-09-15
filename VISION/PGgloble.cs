using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using VISION.Cogs;
using VISION.Camera;
using VISION.Schemas;

namespace VISION
{
    public class PGgloble
    {
        #region "DO NOT TOUCH"
        private static PGgloble instance = null;
        private static readonly object Lock = new object();

        private PGgloble()
        {

        }

        public static PGgloble getInstance
        {
            get
            {
                lock (Lock)
                {
                    if (instance == null)
                    {
                        instance = new PGgloble();
                    }
                    return instance;
                }
            }
        }
        #endregion

        public readonly string PROGRAMROOT = Application.StartupPath;

        public readonly string 기본경로 = Application.StartupPath + "\\Config";

        public readonly string MODELROOT = Application.StartupPath + "\\Models"; //모델저장경로.
        public readonly string MODELLIST = Application.StartupPath + "\\Config\\ModelList.ini"; //모델리스트 ini파일

        public readonly string MODELCONFIGFILE = "\\Modelcfg.ini"; //모델 사용유무 저장.

        public readonly string CONFIGFILE = Application.StartupPath + "\\Config\\config.ini";
        public readonly string SETTING = Application.StartupPath + "\\Config\\setting.ini"; //setting값 저장


        public readonly string PROGRAM_VERSION = "1.0.0"; //Program Version

        // 시스템
        public Model RunnModel = null;
        public Frm_Main G_MainForm;

        public static int CamCount { get { return 3; } }

        public string CurruntModelName;
        public string ImageSaveRoot; // 이미지 저장 경로
        public int ImageSaveDay; //이미지 보관일수.
        public string DataSaveRoot; // 검사 결과 저장 경로
        public string LineName; // 프로그램 메인 화면 중앙 상단에 적힐 무언가.

        public string Camera_SerialNumber; //카메라 시리얼번호.
        public CamSets CameraOption; //카메라 옵션 클래스

        // Light controller
        public string PortName; // 포트 번호
        public string Parity; // 패리티 비트
        public string StopBits; // 스톱비트
        public string DataBit; // 데이터 비트
        public string BaudRate; // 보오 레이트

        public bool InspectUsed; //검사 사용
        public string LightCH1; //조명컨트롤러 채널1
        public string LightCH2; //조명컨트롤러 채널2

        public string ImageFilePath; //이미지파일경로.

        public int CamNumber; //사용카메라번호

        public int RegionAreaInfo; //패턴툴 영역 정보.
        public int Line1_OK;
        public int Line1_NG;
        public int Line2_OK;
        public int Line2_NG;

        public bool OKImageSave = true;
        public bool NGImageSave = true;

        public double[,] InsPat_Result = new double[3, 30];
        public double[,] MultiInsPat_Result = new double[3, 30];

        public bool[] InspectResult = new bool[3];
        public bool[] PatternResult = new bool[3];
        public bool[] BlobResult = new bool[3];
        public bool[] CaliperResult = new bool[3];
        public bool[] MeasureResult = new bool[3];

        public double StandPoint_X;
        public double StandPoint_Y;

        public double[,] MultiPatternResultData = new double[6, 30];

        public 코그넥스파일 코그넥스파일;

        public 환경설정 환경설정;
        public 검사도구 검사도구;
        public 그랩제어 그랩제어;
        public Inspection 비전제어;
    }
    public struct CamSets
    {
        public double[] Exposure; //조리개값
        public double[] Gain;
        public int DelayTime; //지연시간
    }

    public struct 코그넥스파일
    {
        public Model 모델;
        //public Camera[] 카메라;
        //public Mask[,] 마스크툴;
        public Blob[,] 블롭툴;
        public MultiPMAlign[,] 패턴툴;
        public Line[,] 라인툴;
        public Circle[,] 써클툴;
        public Distance[,] 거리측정툴;
        public Caliper[,] 캘리퍼툴;

        public bool[,] 라인툴사용여부;
        public bool[,] 블롭툴사용여부;
        //public bool[,] 블롭툴역검사;
        public bool[,] 패턴툴사용여부;
        public bool[,] 거리측정툴사용여부;
        public bool[,] 써클툴사용여부;
        public bool[,] 캘리퍼툴사용여부;

        public int[,] 블롭툴양품갯수;
        public int[,] 블롭툴픽스쳐번호;
        //public int[,] 패턴툴검사순서번호;

        public double[,] 보정값;
        public double[,] 최소값;
        public double[,] 최대값;
    }
}
