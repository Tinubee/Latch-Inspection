using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VISION.Cogs;

namespace VISION.Schemas
{
    public class 검사도구
    {
        private PGgloble Glob; //전역변수 
        public void Init()
        {
            Glob = PGgloble.getInstance;
            Glob.코그넥스파일.모델 = Glob.RunnModel;
            Glob.코그넥스파일.패턴툴 = Glob.코그넥스파일.모델.MultiPatterns();
            Glob.코그넥스파일.블롭툴 = Glob.코그넥스파일.모델.Blob();
            Glob.코그넥스파일.거리측정툴 = Glob.코그넥스파일.모델.Distancess();
            Glob.코그넥스파일.라인툴 = Glob.코그넥스파일.모델.Line();
            Glob.코그넥스파일.써클툴 = Glob.코그넥스파일.모델.Circle();
            Glob.코그넥스파일.캘리퍼툴 = Glob.코그넥스파일.모델.Calipes();
            Glob.코그넥스파일.패턴툴사용여부 = Glob.코그넥스파일.모델.MultiPatternEnables();

            Glob.코그넥스파일.블롭툴사용여부 = Glob.코그넥스파일.모델.BlobEnables();
            Glob.코그넥스파일.블롭툴양품갯수 = Glob.코그넥스파일.모델.BlobOKCounts();
            Glob.코그넥스파일.블롭툴픽스쳐번호 = Glob.코그넥스파일.모델.BlobFixPatternNumbers();
            Glob.코그넥스파일.거리측정툴사용여부 = Glob.코그넥스파일.모델.DistanceEnables();
            Glob.코그넥스파일.라인툴사용여부 = Glob.코그넥스파일.모델.LineEnables();
            Glob.코그넥스파일.써클툴사용여부 = Glob.코그넥스파일.모델.CircleEnables();
            Glob.코그넥스파일.캘리퍼툴사용여부 = Glob.코그넥스파일.모델.CaliperEnables();

            Glob.코그넥스파일.보정값 = Glob.코그넥스파일.모델.Distance_CalibrationValues();
            Glob.코그넥스파일.최소값 = Glob.코그넥스파일.모델.Distance_LowValues();
            Glob.코그넥스파일.최대값 = Glob.코그넥스파일.모델.Distance_HighValues();
        }

        public Boolean 검사도구저장()
        {
            INIControl CamSet = new INIControl($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\CamSet.ini");
            INIControl CalibrationValue = new INIControl($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\CalibrationValue.ini");
            if (MessageBox.Show("셋팅값을 저장 하시겠습니까?", "Save", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                return false;

            //Glob.G_MainForm.SubFromStart("툴 설정값을 저장중 입니다.", Glob.PROGRAM_VERSION);

            Glob.RunnModel.Line(Glob.코그넥스파일.라인툴); //라인툴저장
            Glob.RunnModel.LineEnables(Glob.코그넥스파일.라인툴사용여부); //라인툴사용여부
            Glob.RunnModel.Blob(Glob.코그넥스파일.블롭툴); //블롭툴저장
            Glob.RunnModel.BlobEnables(Glob.코그넥스파일.블롭툴사용여부); //블롭툴사용여부
            Glob.RunnModel.BlobOKCounts(Glob.코그넥스파일.블롭툴양품갯수); //블롭카운트
            Glob.RunnModel.BlobFixPatternNumbers(Glob.코그넥스파일.블롭툴픽스쳐번호);
            Glob.RunnModel.MultiPatterns(Glob.코그넥스파일.패턴툴); // 멀티패턴툴저장
            Glob.RunnModel.MultiPatternEnables(Glob.코그넥스파일.패턴툴사용여부); // 멀티패턴툴사용여부젖장
            Glob.RunnModel.Distancess(Glob.코그넥스파일.거리측정툴);
            Glob.RunnModel.DistanceEnables(Glob.코그넥스파일.거리측정툴사용여부);
            Glob.RunnModel.Distance_UseTool1_Numbers(Glob.코그넥스파일.모델.Distance_UseTool1_Numbers());
            Glob.RunnModel.Distance_UseTool2_Numbers(Glob.코그넥스파일.모델.Distance_UseTool2_Numbers());

            Glob.RunnModel.SaveModel(Glob.MODELROOT + "\\" + Glob.RunnModel.Modelname() + "\\" + $"Cam{Glob.CamNumber}", Glob.CamNumber); //모델명
            //CamSet.WriteData($"Camera{Glob.CamNumber}", "Exposure", num_Exposure.Value.ToString()); //카메라 노출값
            //CamSet.WriteData($"Camera{Glob.CamNumber}", "Gain", num_Gain.Value.ToString()); //카메라 Gain값
            CamSet.WriteData($"LightControl_Cam{Glob.CamNumber}", "CH1", Glob.LightCH1); //카메라별 조명값 1
            CamSet.WriteData($"LightControl_Cam{Glob.CamNumber}", "CH2", Glob.LightCH2); //카메라별 조명값 2

            //Glob.G_MainForm.SubFromClose();
            MessageBox.Show("저장 완료", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }
    }
}
