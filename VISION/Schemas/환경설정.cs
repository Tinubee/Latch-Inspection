using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VISION.Cogs;
using VISION.Properties;

namespace VISION.Schemas
{
    public class 환경설정
    {
        private PGgloble Glob; //전역변수 
        private string 저장파일 { get { return Path.Combine(Glob.기본경로, "Setting.json"); } }
        public void Init()
        {
            Glob = PGgloble.getInstance;
            Glob.RunnModel = new Model(); //코그넥스 모델 확인.
            Directory.CreateDirectory(Glob.MODELROOT); // 프로그램 모델 루트 디렉토리 작성
            INIControl Config = new INIControl(Glob.CONFIGFILE);
            INIControl Modellist = new INIControl(Glob.MODELLIST);
            INIControl Setting = new INIControl(Glob.SETTING);
            string LastModel = Config.ReadData("LASTMODEL", "NAME"); //마지막 사용모델 확인.
            INIControl CamSet = new INIControl($"{Glob.MODELROOT}\\{LastModel}\\CamSet.ini");

            Glob.ImageSaveRoot = Setting.ReadData("SYSTEM", "Image Save Root"); //이미지 저장 경로
            Glob.DataSaveRoot = Setting.ReadData("SYSTEM", "Data Save Root"); //데이터 저장 경로
            Glob.InspectUsed = Setting.ReadData("SYSTEM", "Inspect Used Check", true) == "1" ? true : false;
            Glob.OKImageSave = Setting.ReadData("SYSTEM", "OK IMAGE SAVE", true) == "1" ? true : false;
            Glob.NGImageSave = Setting.ReadData("SYSTEM", "NG IMAGE SAVE", true) == "1" ? true : false;

            Glob.G_MainForm.log.InitializeLog($"{Glob.DataSaveRoot}\\Log");
            Glob.G_MainForm.log.OnLogEvent += Glob.G_MainForm.Log_OnLogEvent;

            for (int i = 0; i < PGgloble.CamCount; i++)
            {
                Glob.RunnModel.Loadmodel(LastModel, Glob.MODELROOT, i); //VISION TOOL LOAD
            }
            for (int i = 0; i < PGgloble.CamCount; i++)
            {
                //ExposureValue[i] = Convert.ToInt32(CamSet.ReadData($"Camera{i}", "Exposure"));
                //GainValue[i] = Convert.ToInt32(CamSet.ReadData($"Camera{i}", "Gain"));
            }
        }

        public class 환경설정변수
        {
            public string 이미지저장경로 { get; set; } = string.Empty;
            public string 데이터저장경로 { get; set; } = string.Empty;
            public bool 검사사용유무 { get; set; } = true;
            public bool 양품이미지저장 { get; set; } = false;
            public bool 불량이미지저장 { get; set; } = true;
            public string 모델이름 { get; set; } = string.Empty;
        }
    }
}
