﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Cognex.VisionPro;
using System.Threading;
using System.Diagnostics;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.Dimensioning;
using Cognex.VisionPro.ImageProcessing;
using VISION.Camera;

namespace VISION
{
    public partial class Frm_ToolSetUp : Form
    {
        internal MultiPatternResult frm_MultiPatternResult; //캘리브레이션 화면
        private Frm_Main Main;
        private PGgloble Glob;

        private CogImage8Grey Monoimage = new CogImage8Grey();
        private CogImage8Grey Fiximage;
        private string FimageSpace;

        private string[,] TempDistance_Tool1_Number;
        private string[,] TempDistance_Tool2_Number;

        private bool Dataset = false;
        public bool liveflag = false;
        bool ImageDelete = false;
        int SelectNumber;
        //int ImagePathNumber;

        public Frm_ToolSetUp(Frm_Main main)
        {
            InitializeComponent();
            //gainvalue = new double[main.camcount];
            //exposurevalue = new double[main.camcount];
            btn_Livestop.Enabled = false;
            Dataset = true;
            Main = main;
            Glob = PGgloble.getInstance;

            TempDistance_Tool1_Number = Glob.코그넥스파일.모델.Distance_UseTool1_Numbers();
            TempDistance_Tool2_Number = Glob.코그넥스파일.모델.Distance_UseTool2_Numbers();

            string[] Polarty = { "White to Black", "Black to  White", "Don't Care" };
            string[] Blob = { "White Blob", "Black Blob" };
            string[] Direction = { "Inward", "Outward" };
            string[] AreaShape = { "CogCircle", "CogEllipse", "CogRectangleAffine", "CogCircularAnnulusSection" };
            cb_BlobPolarty.Items.AddRange(Blob);
            for (int i = 0; i < 30; i++)
            {
                cb_MultiPatternName.Items.Add(Glob.코그넥스파일.패턴툴[Glob.CamNumber, i].ToolName());
                Num1_DimensionTool.Items.Add(Glob.코그넥스파일.라인툴[Glob.CamNumber, i].ToolName());
                Num2_DimensionTool.Items.Add(Glob.코그넥스파일.라인툴[Glob.CamNumber, i].ToolName());
            }
            for (int i = 0; i < 9; i++)
            {
                Num1_DimensionTool.Items.Add(Glob.코그넥스파일.써클툴[Glob.CamNumber, i].ToolName());
                Num2_DimensionTool.Items.Add(Glob.코그넥스파일.써클툴[Glob.CamNumber, i].ToolName());
            }
            for (int i = 0; i < 9; i++)
            {
                Num1_DimensionTool.Items.Add(Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, i].ToolName());
                Num2_DimensionTool.Items.Add(Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, i].ToolName());
            }
            num_BlobToolNum.Value = 0;
            num_MultiPatternToolNumber.Value = 0;
            num_DimensionToolNum.Value = 1;

            ChangeBlobToolNumber();
            ChangeMultiPatternToolNumber();
            ChangeDistanceToolNumber();
            lb_CurruntModelName.Text = Glob.RunnModel.Modelname(); //현재사용중인 모델명 체크

            Dataset = false;
        }

        private void Frm_ToolSetUp_Load(object sender, EventArgs e)
        {
            //FormLoad = true;
            Glob.CamNumber = 0;
            LoadSetup();
            //CameraSet(); //카메라 Exposure 및 Gain Set Up - 20201215 김형민 ( Main 쪽 Load 할때도 적용 해야되는지 확인하기.)
            UpdateCamStats();
            dgv_ToolSetUp.DoubleBuffered(true);
            DGVUpadte();
            //CalibrationDataLoad();
        }

        private void DGVUpadte()
        {
            dgv_ToolSetUp.Rows.Clear();
            for (int i = 0; i < 30; i++)
            {
                dgv_ToolSetUp.Rows.Add(Glob.RunnModel.MultiPatterns()[Glob.CamNumber, i].ToolName());
                dgv_ToolSetUp.Rows[i].Cells[2].Value = Glob.RunnModel.Blob()[Glob.CamNumber, i].ToolName();
            }
        }
        private void LoadSetup()
        {
            try
            {
                INIControl CamSet = new INIControl($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\CamSet.ini");
                INIControl CalibrationValue = new INIControl($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\CalibrationValue.ini");
                num_Exposure.Value = Convert.ToDecimal(CamSet.ReadData($"Camera{Glob.CamNumber}", "Exposure"));
                num_Gain.Value = Convert.ToDecimal(CamSet.ReadData($"Camera{Glob.CamNumber}", "Gain"));

                //for (int i = 0; i < Main.camcount; i++)
                //{
                //    gainvalue[i] = Convert.ToDouble(CamSet.ReadData($"Camera{i}", "Exposure"));
                //    exposurevalue[i] = Convert.ToDouble(CamSet.ReadData($"Camera{i}", "Gain"));
                //}
                //FormLoad = false;
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
                //cm.info(ee.Message);
            }
        }
        private void btn_ImageOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ImageDelete = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                //Glob.ImageFilePath = ofd.FileName.Substring(0, ofd.FileName.Length - ofd.SafeFileName.Length);
                Glob.ImageFilePath = ofd.FileName;
                string type = Path.GetExtension(ofd.FileName);
                string[] ImageFileName = ofd.FileNames;
                if (type == ".bmp")
                {
                    CogImageFileBMP Imageopen = new CogImageFileBMP();
                    Imageopen.Open(ofd.FileName, CogImageFileModeConstants.Read);
                    Imageopen.Close();
                }
                else
                {
                    CogImageFileJPEG Imageopen2 = new CogImageFileJPEG();
                    Imageopen2.Open(ofd.FileName, CogImageFileModeConstants.Read);
                    CogImageConvertTool ImageConvert = new CogImageConvertTool();
                    ImageConvert.InputImage = Imageopen2[0];
                    ImageConvert.RunParams.RunMode = CogImageConvertRunModeConstants.Plane2;
                    Monoimage = (CogImage8Grey)ImageConvert.OutputImage;
                    Imageopen2.Close();
                }
                for (int i = 0; i < ImageFileName.Count(); i++)
                {
                    ImageList.Items.Add(ImageFileName[i]);
                }
                ImageList.SelectedIndex = ImageList.Items.Count - 1;
                cdyDisplay.InteractiveGraphics.Clear();
                cdyDisplay.StaticGraphics.Clear();
            }
        }

        private void btn_Exit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("셋팅창을 종료 하시겠습니까?", "EXIT", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                return;

            try
            {
                //for (int i = 0; i < Main.camcount; i++)
                //{
                //    Glob.RunnModel.Loadmodel(Glob.RunnModel.Modelname(), Glob.MODELROOT, i);
                //}

                if (Main.frm_toolsetup != null)
                {
                    Main.frm_toolsetup = null;
                }
                Glob.그랩제어.GetItem(카메라구분.Cam01).Close();
                Glob.그랩제어.GetItem(카메라구분.Cam02).Close();
                Glob.그랩제어.GetItem(카메라구분.Cam03).Close();

                Main.LightOFF();
                GC.Collect();
                Dispose();
                Close();
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
                Main.LightOFF();
                GC.Collect();
                Dispose();
                Close();
            }
        }

        private void BlobEnableChange(int toolnumber)
        {
            cb_BlobToolUsed.Text = cb_BlobToolUsed.Checked == true ? "USE" : "UNUSED";
            cb_BlobToolUsed.ForeColor = cb_BlobToolUsed.Checked == true ? Color.Lime : Color.Red;
        }

        public void Pattern_Train()
        {
            if (Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].Run((CogImage8Grey)cdyDisplay.Image) == true)
            {
                int usePatternNumber = Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].HighestResultToolNumber();
                Fiximage = Glob.코그넥스파일.모델.FixtureImage((CogImage8Grey)cdyDisplay.Image, Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].ResultPoint(usePatternNumber), Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].ToolName(), Glob.CamNumber, out FimageSpace, usePatternNumber);
            }
        }
        public void Bolb_Train(int toolnumber)
        {
            if (Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnumber].Run((CogImage8Grey)cdyDisplay.Image) == true)
            {
                int usePatternNumber = Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnumber].HighestResultToolNumber();
                Fiximage = Glob.코그넥스파일.모델.Blob_FixtureImage((CogImage8Grey)cdyDisplay.Image, Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnumber].ResultPoint(usePatternNumber), Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnumber].ToolName(), Glob.CamNumber, toolnumber, out FimageSpace, usePatternNumber);
            }
        }
        //public void Line_Train(int toolnumber)
        //{
        //    if (TempMulti[Glob.CamNumber, toolnumber].Run((CogImage8Grey)cdyDisplay.Image) == true)
        //    {
        //        Fiximage = TempModel.LINE_FixtureImage((CogImage8Grey)cdyDisplay.Image, TempMulti[Glob.CamNumber, toolnumber].ResultPoint(TempMulti[Glob.CamNumber, toolnumber].HighestResultToolNumber()), TempMulti[Glob.CamNumber, toolnumber].ToolName(), Glob.CamNumber, toolnumber, out FimageSpace, TempMulti[Glob.CamNumber, toolnumber].HighestResultToolNumber());
        //        //cdyDisplay.Image = Fiximage;
        //    }
        //}

        private void ImageClear()
        {
            cdyDisplay.StaticGraphics.Clear();
            cdyDisplay.InteractiveGraphics.Clear();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            INIControl CamSet = new INIControl($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\CamSet.ini");
            INIControl CalibrationValue = new INIControl($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\CalibrationValue.ini");
            if (MessageBox.Show("셋팅값을 저장 하시겠습니까?", "Save", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                return;

            Glob.G_MainForm.SubFromStart("툴 설정값을 저장중 입니다.", Glob.PROGRAM_VERSION);

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
            CamSet.WriteData($"Camera{Glob.CamNumber}", "Exposure", num_Exposure.Value.ToString()); //카메라 노출값
            CamSet.WriteData($"Camera{Glob.CamNumber}", "Gain", num_Gain.Value.ToString()); //카메라 Gain값
            CamSet.WriteData($"LightControl_Cam{Glob.CamNumber}", "CH1", Glob.LightCH1); //카메라별 조명값 1
            CamSet.WriteData($"LightControl_Cam{Glob.CamNumber}", "CH2", Glob.LightCH2); //카메라별 조명값 2

            //Glob.G_MainForm.SubFromClose();
            MessageBox.Show("저장 완료", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ChangeBlobToolNumber()
        {
            Dataset = true;
            int Toolnumber = (int)num_BlobToolNum.Value;

            cb_BlobPolarty.SelectedIndex = Glob.코그넥스파일.블롭툴[Glob.CamNumber, Toolnumber].Polarity();
            cb_MultiPatternName.SelectedIndex = Glob.코그넥스파일.블롭툴[Glob.CamNumber, Toolnumber].SpaceName();
            num_BlobMinipixel.Value = Glob.코그넥스파일.블롭툴[Glob.CamNumber, Toolnumber].Minipixel();
            num_BlobThreshold.Value = Glob.코그넥스파일.블롭툴[Glob.CamNumber, Toolnumber].Threshold();
            num_AreaPoinrNumber.Value = Glob.코그넥스파일.블롭툴[Glob.CamNumber, Toolnumber].PointNumber();
            num_BlobMaxCout.Value = Glob.코그넥스파일.블롭툴양품갯수[Glob.CamNumber, Toolnumber];

            cb_BlobToolUsed.Checked = Glob.코그넥스파일.블롭툴사용여부[Glob.CamNumber, Toolnumber];

            BlobEnableChange(Toolnumber);
            Dataset = false;
        }
        private void btn_ToolRun_Click(object sender, EventArgs e)
        {
            if (cdyDisplay.Image == null)
                return;
            INIControl setting = new INIControl(Glob.SETTING);
            ImageClear();
            // Pattern_Train();
            switch (Glob.CamNumber)
            {
                case 0:
                    lb_Tool_InspectResult.Text = Glob.비전제어.Run(cdyDisplay, 카메라구분.Cam01) ? "O K" : "N G";
                    lb_Tool_InspectResult.BackColor = lb_Tool_InspectResult.Text == "O K" ? Color.Lime : Color.Red;
                    break;
                case 1:
                    lb_Tool_InspectResult.Text = Glob.비전제어.Run(cdyDisplay, 카메라구분.Cam02) ? "O K" : "N G";
                    lb_Tool_InspectResult.BackColor = lb_Tool_InspectResult.Text == "O K" ? Color.Lime : Color.Red;
                    break;
                case 2:
                    lb_Tool_InspectResult.Text = Glob.비전제어.Run(cdyDisplay, 카메라구분.Cam03) ? "O K" : "N G";
                    lb_Tool_InspectResult.BackColor = lb_Tool_InspectResult.Text == "O K" ? Color.Lime : Color.Red;
                    break;
            }
            Invoke(new Action(delegate ()
            {
                Main.DgvResult(dgv_ToolSetUp, Glob.CamNumber, 1); //-추가된함수
            }));
        }

        private void btn_OneShot_Click(object sender, EventArgs e)
        {
            try
            {
                cdyDisplay.Image = null;
                cdyDisplay.InteractiveGraphics.Clear();
                cdyDisplay.StaticGraphics.Clear();
                Main.SnapShot(Glob.CamNumber);
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
                //cm.info(ee.Message);
            }
        }

        private void btn_Live_Click(object sender, EventArgs e)
        {
            try
            {
                if (liveflag == false)
                {
                    cdyDisplay.InteractiveGraphics.Clear();
                    cdyDisplay.StaticGraphics.Clear();
                    cdyDisplay.Fit();
                    bool 결과 = Glob.그랩제어.GetItem((카메라구분)Glob.CamNumber).라이브시작();
                    if (결과)
                    {
                        liveflag = true;
                        btn_OneShot.Enabled = false;
                        btn_Live.Enabled = false;
                        num_Exposure.Enabled = true;
                        num_Gain.Enabled = true;
                        btn_Livestop.Enabled = true;
                        btn_Exit.Enabled = false;
                    }
                }
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
                return;
            }
        }
        private void CameraSet()
        {
            try
            {
                //for (int i = 0; i < Main.mDevice.Count(); i++)
                //{
                //    if (Main.mDevice[i] != null)
                //    {
                //        Main.mDevice[i].RemoteNodeList["AcquisitionStop"].Execute();
                //        Main.mDataStream[i].StopAcquisition();
                //        Thread.Sleep(200);
                //        //exposurevalue[i] = (double)num_Exposure.Value;
                //        Main.mDevice[i].RemoteNodeList["ExposureTime"].Value = exposurevalue[i];
                //        Thread.Sleep(50);
                //        if (FormLoad == false)
                //        {
                //            Main.mDataStream[i].StartAcquisition();
                //            Main.mDevice[i].RemoteNodeList["AcquisitionStart"].Execute();
                //        }
                //    }
                //}

            }
            catch (BGAPI2.Exceptions.IException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private void num_Exposure_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (num_Exposure.Enabled == true)
                {
                    카메라장치 장치 = new 카메라장치();
                    장치.노출값 = (double)num_Exposure.Value;
                    Glob.그랩제어.GetItem((카메라구분)Glob.CamNumber + 1).Set(장치);
                    //if (Main.mDevice[Glob.CamNumber] != null)
                    //{
                    //    Main.mDevice[Glob.CamNumber].RemoteNodeList["AcquisitionStop"].Execute();
                    //    Main.mDataStream[Glob.CamNumber].StopAcquisition();
                    //    Thread.Sleep(200);
                    //    exposurevalue[Glob.CamNumber] = (double)num_Exposure.Value;
                    //    Main.mDevice[Glob.CamNumber].RemoteNodeList["ExposureTimeAbs"].Value = (double)num_Exposure.Value;
                    //    Thread.Sleep(50);
                    //    if (FormLoad == false)
                    //    {
                    //        Main.mDataStream[Glob.CamNumber].StartAcquisition();
                    //        Main.mDevice[Glob.CamNumber].RemoteNodeList["AcquisitionStart"].Execute();
                    //    }
                    //}
                }
            }
            catch (BGAPI2.Exceptions.IException ex)
            {
                Console.Write("ExceptionType:    {0} \r\n", ex.GetType());
                Console.Write("ErrorDescription: {0} \r\n", ex.GetErrorDescription());
                Console.Write("in function:      {0} \r\n", ex.GetFunctionName());
            }
        }

        private void btn_Livestop_Click(object sender, EventArgs e)
        {
            try
            {
                liveflag = false;
                cdyDisplay.StaticGraphics.Clear();
                cdyDisplay.InteractiveGraphics.Clear();
                bool 결과 = Glob.그랩제어.GetItem((카메라구분)Glob.CamNumber).라이브종료();
                if (결과)
                {
                    liveflag = false;
                    btn_OneShot.Enabled = true;
                    btn_Live.Enabled = true;
                    num_Exposure.Enabled = false;
                    num_Gain.Enabled = false;
                    btn_Livestop.Enabled = false;
                    btn_Exit.Enabled = true;
                }
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
            }
        }

        private void num_Gain_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (num_Gain.Enabled == true)
                {
                    //if (Main.mDevice[Glob.CamNumber] != null)
                    //{
                    //    Main.mDevice[Glob.CamNumber].RemoteNodeList["AcquisitionStop"].Execute();
                    //    Main.mDataStream[Glob.CamNumber].StopAcquisition();
                    //    Thread.Sleep(200);
                    //    gainvalue[Glob.CamNumber] = (double)num_Gain.Value;
                    //    Main.mDevice[Glob.CamNumber].RemoteNodeList["GainAbs"].Value = (double)num_Gain.Value;
                    //    Thread.Sleep(50);
                    //    if (FormLoad == false)
                    //    {
                    //        Main.mDataStream[Glob.CamNumber].StartAcquisition();
                    //        Main.mDevice[Glob.CamNumber].RemoteNodeList["AcquisitionStart"].Execute();
                    //    }
                    //}
                }
            }
            catch (BGAPI2.Exceptions.IException ex)
            {
                Console.Write("ExceptionType:    {0} \r\n", ex.GetType());
                Console.Write("ErrorDescription: {0} \r\n", ex.GetErrorDescription());
                Console.Write("in function:      {0} \r\n", ex.GetFunctionName());
            }
        }

        private void btn_ImageSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                CogImageFileBMP ImageSave = new CogImageFileBMP();
                ImageSave.Open(sfd.FileName + ".bmp", CogImageFileModeConstants.Write);
                ImageSave.Append(cdyDisplay.Image);
                ImageSave.Close();
            }
        }
        private void num_BlobToolNum_ValueChanged(object sender, EventArgs e)
        {
            ChangeBlobToolNumber();
        }

        private void cb_BlobToolUsed_CheckedChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
            {
                Glob.코그넥스파일.블롭툴사용여부[Glob.CamNumber, (int)num_BlobToolNum.Value] = cb_BlobToolUsed.Checked;
                BlobEnableChange((int)num_BlobToolNum.Value);
            }
        }

        private void cb_BlobPolarty_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
                Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].Polarity(cb_BlobPolarty.SelectedIndex);
        }

        private void num_BlobMinipixel_ValueChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
                Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].Minipixel((int)num_BlobMinipixel.Value);
        }

        private void num_BlobThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
                Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].Threshold((int)num_BlobThreshold.Value);
        }

        private void btn_BlobInspectionArea_Click(object sender, EventArgs e)
        {
            ImageClear();
            Bolb_Train((int)num_BlobToolNum.Value);
            Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].Area_Affine(ref cdyDisplay, (CogImage8Grey)cdyDisplay.Image, cb_MultiPatternName.SelectedIndex.ToString());
        }

        private void btn_BlobInspection_Click(object sender, EventArgs e)
        {
            ImageClear();
            Bolb_Train((int)num_BlobToolNum.Value);
            CogGraphicCollection Collection = new CogGraphicCollection();

            Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].Run((CogImage8Grey)cdyDisplay.Image);
            if (Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].ResultBlobCount() != Glob.코그넥스파일.블롭툴양품갯수[Glob.CamNumber, (int)num_BlobToolNum.Value]) //BlobTool 실행.
            {
                Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].ResultAllBlobDisplayPLT(Collection, false);
            }
            else
            {
                Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].ResultAllBlobDisplayPLT(Collection, true);
            }

            cdyDisplay.StaticGraphics.AddList(Collection, "");
            tb_BlobCount.Text = Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].ResultBlobCount().ToString();
        }

        private void BLOBINSPECTION_DoubleClick_1(object sender, EventArgs e)
        {
            Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].ToolSetup();
        }

        private void num_BlobMaxCout_ValueChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
                Glob.코그넥스파일.블롭툴양품갯수[Glob.CamNumber, (int)num_BlobToolNum.Value] = (int)num_BlobMaxCout.Value;
        }

        private void ImageList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ImageDelete == false)
            {
                string ImageType;
                ListBox temp_lbx = (ListBox)sender;
                //string FilePath = Glob.ImageFilePath;
                //string ImageName = FilePath + temp_lbx.SelectedItem.ToString();
                string ImageName = temp_lbx.SelectedItem.ToString();

                cdyDisplay.InteractiveGraphics.Clear();
                cdyDisplay.StaticGraphics.Clear();

                CogImageFileTool curimage = new CogImageFileTool();
                curimage.Operator.Open(ImageName, CogImageFileModeConstants.Read);
                curimage.Run();
                ImageType = curimage.OutputImage.GetType().ToString();
                if (ImageType.Contains("CogImage24PlanarColor"))
                {
                    CogImageConvertTool imageconvert = new CogImageConvertTool();
                    imageconvert.InputImage = curimage.OutputImage;
                    imageconvert.RunParams.RunMode = CogImageConvertRunModeConstants.Plane2;
                    imageconvert.Run();
                    cdyDisplay.Image = (CogImage8Grey)imageconvert.OutputImage;
                }
                else
                {
                    cdyDisplay.Image = (CogImage8Grey)curimage.OutputImage; //JPG 파일
                }
                //cdyDisplay.Image = (CogImage8Grey)curimage.OutputImage;
                cdyDisplay.Fit();
                GC.Collect();
            }
            else
            {
                if (ImageList.Items.Count == 0)
                {
                    ImageClear();
                    cdyDisplay.Image = null;
                }
                else
                {
                    ImageDelete = false;
                    ImageList.SelectedIndex = SelectNumber == 0 ? SelectNumber : SelectNumber - 1;
                }
            }
        }

        private void btn_AllClear_Click(object sender, EventArgs e)
        {
            ImageList.Items.Clear();
            ImageClear();
            cdyDisplay.Image = null;
        }

        private void btn_DeleteImage_Click(object sender, EventArgs e)
        {
            if (ImageList.Items.Count == 0)
                return;
            ImageDelete = true;
            SelectNumber = ImageList.SelectedIndex;
            ImageList.Items.RemoveAt(ImageList.SelectedIndex);
        }

        private void Frm_ToolSetUp_KeyDown(object sender, KeyEventArgs e)
        {
            //************************************단축키 모음************************************//
            if (e.Control && e.KeyCode == Keys.S) //ctrl + s : 셋팅값 저장.
                btn_Save_Click(sender, e);
            if (e.Control && e.KeyCode == Keys.O) //ctrl + o : 이미지 열기. 
                btn_ImageOpen_Click(sender, e);
            if (e.Control && e.KeyCode == Keys.H) //ctrl + h : 카메라 1회촬영.
                btn_OneShot_Click(sender, e);
            if (e.Control && e.KeyCode == Keys.L) //ctrl + l : 카메라 라이브 모드
                btn_Live_Click(sender, e);
            //if (e.KeyCode == Keys.Escape) // esc : 셋팅창 종료
            //    btn_Exit_Click(sender, e);
            if (e.Control && e.KeyCode == Keys.D1) //ctrl + 1 : 1번카메라 화면.
                btn_Cam1.PerformClick();
            if (e.Control && e.KeyCode == Keys.D2) //ctrl + 2 : 2번카메라 화면.
                btn_Cam2.PerformClick();
            if (e.Control && e.KeyCode == Keys.D3) //ctrl + 3 : 3번카메라 화면.
                btn_Cam3.PerformClick();
        }

        private void ImageList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                btn_DeleteImage_Click(sender, e);
        }
        private void UpdateCamStats()
        {
            btn_Cam1.BackColor = Glob.CamNumber == 0 ? Color.Lime : Color.Silver;
            btn_Cam2.BackColor = Glob.CamNumber == 1 ? Color.Lime : Color.Silver;
            btn_Cam3.BackColor = Glob.CamNumber == 2 ? Color.Lime : Color.Silver;
        }
        private void UpdateCameraSet()
        {
            try
            {
                INIControl CamSet = new INIControl($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\CamSet.ini");
                //카메라 및 조명 setting값 ini파일에 저장. - 카메라별로
                //변경될수도있는 사항 : 모델별로 각각 카메라 setting값을 따로 가져가야 될 수도있을꺼 같음. - 191231 김형민 
                // --> 모델별로 카메라값 가져가도록 변경완료 - 200122 김형민
                num_Exposure.Value = Convert.ToDecimal(CamSet.ReadData($"Camera{Glob.CamNumber}", "Exposure"));
                num_Gain.Value = Convert.ToDecimal(CamSet.ReadData($"Camera{Glob.CamNumber}", "Gain"));
                //FormLoad = false;
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
            }
        }
        private void ReLoadVisionTool()
        {
            try
            {
                PGgloble gls = PGgloble.getInstance;
                if (gls.RunnModel.Loadmodel(Glob.RunnModel.Modelname(), gls.MODELROOT, Glob.CamNumber) == true)
                {
                    Debug.WriteLine("Cognex VisionTool ReLoad 완료.");
                }
            }
            catch (Exception ee)
            {
                Debug.WriteLine($"{ee.Message}");
            }
        }
        private void btn_Cam1_Click(object sender, EventArgs e)
        {
            try
            {
                //CAM1부터있는 모든 버튼들이 하나의 이벤트로 묶어져있다.(참조확인)
                //if (Program.CameraList.Count() == 0)
                //    return;

                //FormLoad = true;
                int job = Convert.ToInt32((sender as Button).Tag); //클리한 버튼의 TAG값 받아오기.
                Glob.CamNumber = job; // Convert.ToInt32(Program.CameraList[job].Number);

                cdyDisplay.InteractiveGraphics.Clear();
                cdyDisplay.StaticGraphics.Clear();
                cdyDisplay.Image = null;

                UpdateCamStats(); //카메라 상태표시 변경
                UpdateCameraSet(); //카메라 셋팅 변경
                ReLoadVisionTool(); //카메라별로 Vision Tool 이 저장되어있기 때문에 CamNumber가 바뀔때 마다 불러와준다.

                ChangeMultiPatternToolNumber();
                ChangeBlobToolNumber();
                ChangeDistanceToolNumber();
                //LineChangeToolNumber();
                //CircleChangeToolNumber();
                DGVUpadte();
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
            }

        }

        private void num_AreaPoinrNumber_ValueChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
                Glob.코그넥스파일.블롭툴[Glob.CamNumber, (int)num_BlobToolNum.Value].AreaPointNumber((int)num_AreaPoinrNumber.Value);
        }

        private void label27_DoubleClick(object sender, EventArgs e)
        {
            Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].InputImage((CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ToolSetup();
        }
        private void num_TrainImageNumber_ValueChanged(object sender, EventArgs e)
        {
            int Toolnumber = (int)num_MultiPatternToolNumber.Value;
            cdyMultiPTTrained.Image = Glob.코그넥스파일.패턴툴[Glob.CamNumber, Toolnumber].TrainedImage((int)num_TrainImageNumber.Value);
        }

        private void num_MultiPatternToolNumber_ValueChanged(object sender, EventArgs e)
        {
            ChangeMultiPatternToolNumber();
            CheckMultiPatternStatus();
        }

        private void ChangeMultiPatternToolNumber()
        {
            Dataset = true;
            int Toolnumber = (int)num_MultiPatternToolNumber.Value;
            cb_MultiPatternToolUsed.Checked = Glob.코그넥스파일.패턴툴사용여부[Glob.CamNumber, Toolnumber];
            cdyMultiPTTrained.Image = Glob.코그넥스파일.패턴툴[Glob.CamNumber, Toolnumber].TrainedImage(0);
            num_MultiPatternScore.Value = Convert.ToDecimal(Glob.코그넥스파일.패턴툴[Glob.CamNumber, Toolnumber].Threshold() * 100);
            lb_PatternCount.Text = $"등록된 패턴 수 : {Glob.코그넥스파일.패턴툴[Glob.CamNumber, Toolnumber].PatternCount()}개";
            lb_FindPatternCount.Text = $"찾은 패턴 수 : {Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultCount()}개";
            num_TrainImageNumber.Maximum = Glob.코그넥스파일.패턴툴[Glob.CamNumber, Toolnumber].PatternCount() - 1;
            num_TrainImageNumber.Minimum = 0;
            MultiPatternEnableChange(Toolnumber);
            Dataset = false;
        }
        private void MultiPatternEnableChange(int toolnumber)
        {
            cb_MultiPatternToolUsed.Text = cb_MultiPatternToolUsed.Checked == true ? "USE" : "UNUSED";
            cb_MultiPatternToolUsed.ForeColor = cb_MultiPatternToolUsed.Checked == true ? Color.Lime : Color.Red;
        }
        private void CheckMultiPatternStatus()
        {
            if (Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].istrain((int)num_MultiPatternToolNumber.Value) == true)
            {
                cdyMultiPTTrained.Image = Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].TrainedImage(0);
            }
            else
            {
                cdyMultiPTTrained.Image = null;
            }
        }

        private void cb_MultiPatternToolUsed_CheckedChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
            {
                Glob.코그넥스파일.패턴툴사용여부[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value] = cb_MultiPatternToolUsed.Checked;
                MultiPatternEnableChange((int)num_MultiPatternToolNumber.Value);
            }
        }

        private void btn_MultiPatternToolRun_Click(object sender, EventArgs e)
        {
            ImageClear();
            Pattern_Train();
            if (Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].Run((CogImage8Grey)cdyDisplay.Image) == true)
            {
                for (int i = 0; i < Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultCount(); i++)
                {
                    Glob.MultiPatternResultData[Glob.CamNumber, i] = Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultScore(i);
                }

                Glob.MultiInsPat_Result[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value] = Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultScore(Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].HighestResultToolNumber());
                Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultDisplay(ref cdyDisplay, Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].HighestResultToolNumber());
                lb_FindPatternCount.Text = $"찾은 패턴 수 : {Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultCount()}개";
                lb_MultiScore.ForeColor = Color.Lime;
                lb_MultiScore.Text = $"{Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultScore(Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].HighestResultToolNumber()).ToString("F2")}%";
            }
            else
            {
                ImageClear();
                lb_MultiScore.Text = "패턴 찾기 실패";
                lb_FindPatternCount.Text = $"찾은 패턴 수 : {Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultCount()}개";
                lb_MultiScore.ForeColor = Color.Red;
                Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultDisplay(ref cdyDisplay, (int)num_MultiPatternToolNumber.Value);
            }
        }

        private void btn_PatternInput_Click(object sender, EventArgs e)
        {
            Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].InputImage((CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ToolSetup();
            ChangeMultiPatternToolNumber();
        }

        private void btn_ResultShow_Click(object sender, EventArgs e)
        {
            frm_MultiPatternResult = new MultiPatternResult(this, Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].ResultCount());
            frm_MultiPatternResult.Show();
        }

        private void num_MultiPatternScore_ValueChanged(object sender, EventArgs e)
        {
            Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].Threshold((double)num_MultiPatternScore.Value / 100);
        }

        private void btn_MultiPTSearchArea_Click(object sender, EventArgs e)
        {
            ImageClear();
            Pattern_Train();
            Glob.코그넥스파일.패턴툴[Glob.CamNumber, (int)num_MultiPatternToolNumber.Value].SearchArea(ref cdyDisplay, (CogImage8Grey)cdyDisplay.Image, Glob.CamNumber, (int)num_MultiPatternToolNumber.Value);
        }

        private void cb_MultiPatternName_SelectedIndexChanged(object sender, EventArgs e)
        {
            int toolnum = cb_MultiPatternName.SelectedIndex;
            Glob.코그넥스파일.블롭툴픽스쳐번호[Glob.CamNumber, (int)num_BlobToolNum.Value] = toolnum;
            ImageClear();
            if (Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnum].Run((CogImage8Grey)cdyDisplay.Image))
            {
                int usePatternNumber = Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnum].HighestResultToolNumber();
                Fiximage = Glob.코그넥스파일.모델.Blob_FixtureImage((CogImage8Grey)cdyDisplay.Image, Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnum].ResultPoint(usePatternNumber), Glob.코그넥스파일.패턴툴[Glob.CamNumber, toolnum].ToolName(), Glob.CamNumber, toolnum, out FimageSpace, usePatternNumber);
            }
            else
            {
                //cm.info("이미지가 없습니다.");
            }
        }

        private void label55_DoubleClick(object sender, EventArgs e)
        {
            Glob.코그넥스파일.거리측정툴[Glob.CamNumber, (int)num_DimensionToolNum.Value].InputImage((int)num_DimensionToolNum.Value, (CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.거리측정툴[Glob.CamNumber, (int)num_DimensionToolNum.Value].ToolSetup((int)num_DimensionToolNum.Value);
        }

        private void num_DimensionToolNum_ValueChanged(object sender, EventArgs e)
        {
            ChangeDistanceToolNumber();
        }

        private void Num1_DimensionTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            string toolname = Num1_DimensionTool.SelectedItem.ToString();
            string[] s_toolname = toolname.Split('-');
            TempDistance_Tool1_Number[Glob.CamNumber, (int)num_DimensionToolNum.Value] = toolname;
            switch (s_toolname[0])
            {
                case "Line ":
                    Glob.코그넥스파일.라인툴사용여부[Glob.CamNumber, Convert.ToInt32(s_toolname[1])] = true;
                    break;
                case "Caliper ":
                    Glob.코그넥스파일.캘리퍼툴사용여부[Glob.CamNumber, Convert.ToInt32(s_toolname[1])] = true;
                    break;
                case "Circle ":
                    Glob.코그넥스파일.써클툴사용여부[Glob.CamNumber, Convert.ToInt32(s_toolname[1])] = true;
                    break;
            }
        }
        private void Num2_DimensionTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            string toolname = Num2_DimensionTool.SelectedItem.ToString();
            string[] s_toolname = toolname.Split('-');
            TempDistance_Tool2_Number[Glob.CamNumber, (int)num_DimensionToolNum.Value] = toolname;
            switch (s_toolname[0])
            {
                case "Line ":
                    Glob.코그넥스파일.라인툴사용여부[Glob.CamNumber, Convert.ToInt32(s_toolname[1])] = true;
                    break;
                case "Caliper ":
                    Glob.코그넥스파일.캘리퍼툴사용여부[Glob.CamNumber, Convert.ToInt32(s_toolname[1])] = true;
                    break;
                case "Circle ":
                    Glob.코그넥스파일.써클툴사용여부[Glob.CamNumber, Convert.ToInt32(s_toolname[1])] = true;
                    break;
            }
        }
        private void ChangeDistanceToolNumber()
        {
            Dataset = true;
            int Toolnumber = (int)num_DimensionToolNum.Value;
            Num1_DimensionTool.SelectedItem = TempDistance_Tool1_Number[Glob.CamNumber, Toolnumber];
            Num2_DimensionTool.SelectedItem = TempDistance_Tool2_Number[Glob.CamNumber, Toolnumber];
            tb_DemensionTool_Measure.Text = string.Empty;
            tb_DemensionTool_Pix.Text = string.Empty;
            tb_DemensionTool_Calibration.Text = Glob.코그넥스파일.보정값[Glob.CamNumber, Toolnumber].ToString("F5");
            tb_LowValue.Text = Glob.코그넥스파일.최소값[Glob.CamNumber, Toolnumber].ToString("F3");
            tb_HighValue.Text = Glob.코그넥스파일.최대값[Glob.CamNumber, Toolnumber].ToString("F3");

            cb_DimensionToolUsed.Checked = Glob.코그넥스파일.거리측정툴사용여부[Glob.CamNumber, Toolnumber];
            DistanceEnableChange(Toolnumber);
            Dataset = false;
        }
        private void DistanceEnableChange(int toolnumber)
        {
            cb_DimensionToolUsed.Text = cb_DimensionToolUsed.Checked == true ? "USE" : "UNUSED";
            cb_DimensionToolUsed.ForeColor = cb_DimensionToolUsed.Checked == true ? Color.Lime : Color.Red;
        }

        private void cb_DimensionToolUsed_CheckedChanged(object sender, EventArgs e)
        {
            if (Dataset == false)
            {
                Glob.코그넥스파일.거리측정툴사용여부[Glob.CamNumber, (int)num_DimensionToolNum.Value] = cb_DimensionToolUsed.Checked;
                DistanceEnableChange((int)num_DimensionToolNum.Value);
            }
        }

        private void Num1_DimensionTool_Area_Click(object sender, EventArgs e)
        {
            string ToolName = Num1_DimensionTool.SelectedItem.ToString();
            string[] Tool1_Name = ToolName.Split('-');

            if (Tool1_Name[0] == "Line ")
            {
                ImageClear();
                Pattern_Train();
                Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(Tool1_Name[1])].Area(ref cdyDisplay, (CogImage8Grey)cdyDisplay.Image, Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].ToolName());
            }
        }
        private void LineInspection(string ToolName)
        {
            CogGraphicCollection Collection = new CogGraphicCollection();
            string[] Tool1_Name = ToolName.Split('-');

            Pattern_Train();

            Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(Tool1_Name[1])].Run((CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(Tool1_Name[1])].ResultDisplay(cdyDisplay, Collection);
        }

        private void CaliperXY(string ToolName)
        {
            CogGraphicCollection Collection = new CogGraphicCollection();
            string[] Tool2_Name = ToolName.Split('-');

            Pattern_Train();

            Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].Run((CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].ResultDisplay(ref cdyDisplay);
        }
        private void AveragePointXY(string ToolName)
        {
            CogGraphicCollection Collection = new CogGraphicCollection();
            string[] Tool2_Name = ToolName.Split('-');

            Pattern_Train();

            Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].Run((CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].ResultDisplay(cdyDisplay, Collection);
        }

        private void btn_Tool1Inspect_Click(object sender, EventArgs e)
        {
            //CogGraphicCollection Collection = new CogGraphicCollection();
            string ToolName = Num1_DimensionTool.SelectedItem.ToString();
            string[] Tool1_Name = ToolName.Split('-');
            if (Tool1_Name[0] == "Line ")
            {
                ImageClear();
                LineInspection(ToolName);
            }
        }
        private void Num2_DimensionTool_Area_Click(object sender, EventArgs e)
        {
            string ToolName = Num2_DimensionTool.SelectedItem.ToString();
            string[] Tool2_Name = ToolName.Split('-');
            ImageClear();
            Pattern_Train();
            switch (Tool2_Name[0])
            {
                case "Line ":
                    Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].Area(ref cdyDisplay, (CogImage8Grey)cdyDisplay.Image, Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].ToolName());
                    break;
                case "Caliper ":
                    Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].Area(ref cdyDisplay, (CogImage8Grey)cdyDisplay.Image, Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].ToolName());
                    break;
                case "Circle ":
                    Glob.코그넥스파일.써클툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].Area(ref cdyDisplay, (CogImage8Grey)cdyDisplay.Image, Glob.코그넥스파일.패턴툴[Glob.CamNumber, 0].ToolName());
                    break;
            }
        }
        private void btn_Tool2Inspect_Click(object sender, EventArgs e)
        {
            CogGraphicCollection Collection = new CogGraphicCollection();
            string ToolName = Num2_DimensionTool.SelectedItem.ToString();
            string[] Tool2_Name = ToolName.Split('-');
            ImageClear();
            switch (Tool2_Name[0])
            {
                case "Line ":
                    AveragePointXY(ToolName);
                    break;
                case "Caliper ":
                    CaliperXY(ToolName);
                    break;
                case "Circle ":
                    Circle(ToolName);
                    break;
            }
        }
        private void Circle(string ToolName)
        {
            CogGraphicCollection Collection = new CogGraphicCollection();
            string[] Tool2_Name = ToolName.Split('-');

            Pattern_Train();

            Glob.코그넥스파일.써클툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].Run((CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.써클툴[Glob.CamNumber, Convert.ToInt32(Tool2_Name[1])].ResultDisplay(ref cdyDisplay);
        }
        private void btn_DimensionTool_Inspection_Click(object sender, EventArgs e)
        {
            if (cdyDisplay.Image == null)
                return;

            CogGraphicCollection Collection = new CogGraphicCollection();
            CogCreateGraphicLabelTool Label = new CogCreateGraphicLabelTool();
            double ResultValue = 0;
            ImageClear();
            Pattern_Train();
            string Tool1_Name = TempDistance_Tool1_Number[Glob.CamNumber, (int)num_DimensionToolNum.Value];
            string Tool2_Name = TempDistance_Tool2_Number[Glob.CamNumber, (int)num_DimensionToolNum.Value];
            string[] splitTool2Name = Tool2_Name.Split('-');

            LineInspection(Tool1_Name);
            switch (splitTool2Name[0])
            {
                case "Line ":
                    AveragePointXY(Tool2_Name);
                    break;
                case "Caliper ":
                    CaliperXY(Tool2_Name);
                    break;
                case "Circle ":
                    Circle(Tool2_Name);
                    break;
            }

            Glob.코그넥스파일.거리측정툴[Glob.CamNumber, (int)num_DimensionToolNum.Value].Run((int)num_DimensionToolNum.Value, (CogImage8Grey)cdyDisplay.Image);
            Glob.코그넥스파일.거리측정툴[Glob.CamNumber, (int)num_DimensionToolNum.Value].ResultDisplay((int)num_DimensionToolNum.Value, cdyDisplay, Collection);
            ResultValue = Glob.코그넥스파일.거리측정툴[Glob.CamNumber, (int)num_DimensionToolNum.Value].DistanceValue((int)num_DimensionToolNum.Value) * Glob.코그넥스파일.보정값[Glob.CamNumber, (int)num_DimensionToolNum.Value];
            tb_DemensionTool_Pix.Text = Glob.코그넥스파일.거리측정툴[Glob.CamNumber, (int)num_DimensionToolNum.Value].DistanceValue((int)num_DimensionToolNum.Value).ToString("F3");
            tb_DemensionTool_Measure.Text = ResultValue.ToString("F3");

            if (Glob.코그넥스파일.최소값[Glob.CamNumber, (int)num_DimensionToolNum.Value] <= ResultValue && Glob.코그넥스파일.최대값[Glob.CamNumber, (int)num_DimensionToolNum.Value] >= ResultValue)
            {
                Label.OutputColor = CogColorConstants.Green;
            }
            else
            {
                Label.OutputColor = CogColorConstants.Red;
            }

            Label.InputImage = cdyDisplay.Image;
            Label.InputGraphicLabel.X = 1200;
            Label.InputGraphicLabel.Y = 600;
            Label.InputGraphicLabel.Text = $"{Glob.코그넥스파일.거리측정툴[Glob.CamNumber, (int)num_DimensionToolNum.Value].ToolName((int)num_DimensionToolNum.Value)} : {tb_DemensionTool_Measure.Text}";
            Label.Run();
            cdyDisplay.StaticGraphics.Add(Label.GetOutputGraphicLabel(), "");
        }

        private void label57_DoubleClick(object sender, EventArgs e)
        {
            string toolname = Num1_DimensionTool.SelectedItem.ToString();
            string[] s_toolname = toolname.Split('-');
            switch (s_toolname[0])
            {
                case "Line ":
                    Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].InputImage((CogImage8Grey)cdyDisplay.Image);
                    Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].ToolSetup();
                    break;
                case "Caliper ":
                    Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].InputImage((CogImage8Grey)cdyDisplay.Image);
                    Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].ToolSetup();
                    break;
            }
        }

        private void label59_DoubleClick(object sender, EventArgs e)
        {
            string toolname = Num2_DimensionTool.SelectedItem.ToString();
            string[] s_toolname = toolname.Split('-');
            switch (s_toolname[0])
            {
                case "Line ":
                    Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].InputImage((CogImage8Grey)cdyDisplay.Image);
                    Glob.코그넥스파일.라인툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].ToolSetup();
                    break;
                case "Caliper ":
                    Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].InputImage((CogImage8Grey)cdyDisplay.Image);
                    Glob.코그넥스파일.캘리퍼툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].ToolSetup();
                    break;
                case "Circle ":
                    Glob.코그넥스파일.써클툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].InputImage((CogImage8Grey)cdyDisplay.Image);
                    Glob.코그넥스파일.써클툴[Glob.CamNumber, Convert.ToInt32(s_toolname[1])].ToolSetup();
                    break;
            }
        }

        private void btn_DemensionTool_GetCalValue_Click(object sender, EventArgs e)
        {
            try
            {
                Glob.코그넥스파일.보정값[Glob.CamNumber, (int)num_DimensionToolNum.Value] = Convert.ToDouble(tb_DemensionTool_Measure.Text) / Convert.ToDouble(tb_DemensionTool_Pix.Text);
                tb_DemensionTool_Calibration.Text = Glob.코그넥스파일.보정값[Glob.CamNumber, (int)num_DimensionToolNum.Value].ToString("F5");
            }
            catch
            {
                Glob.코그넥스파일.보정값[Glob.CamNumber, (int)num_DimensionToolNum.Value] = Convert.ToDouble(tb_DemensionTool_Calibration.Text);
                //tb_DemensionTool_Calibration.Text = tb_DemensionTool_Calibration.Text;
            }
        }

        private void btn_SpecApply_Click(object sender, EventArgs e)
        {
            Glob.코그넥스파일.최소값[Glob.CamNumber, (int)num_DimensionToolNum.Value] = Convert.ToDouble(tb_LowValue.Text);
            Glob.코그넥스파일.최대값[Glob.CamNumber, (int)num_DimensionToolNum.Value] = Convert.ToDouble(tb_HighValue.Text);
        }

        private void btn_ApplyMaster_Click(object sender, EventArgs e)
        {
            string fileName;
            if (cdyDisplay.Image == null)
            {
                MessageBox.Show("이미지가 없습니다. 카메라촬영을 진행해 주시거나, 이미지파일을 불러오시기 바랍니다.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var result = MessageBox.Show($"현재 이미지를 CAM{Glob.CamNumber + 1} Master 이미지로 저장 하시겠습니까?", " 이미지 저장 ",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                fileName = $"CAM{Glob.CamNumber}_Master";
                CogImageFileBMP ImageSave = new CogImageFileBMP();
                ImageSave.Open($"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\Cam{Glob.CamNumber}\\CAM{Glob.CamNumber}_Master.bmp", CogImageFileModeConstants.Write);
                ImageSave.Append(cdyDisplay.Image);
                ImageSave.Close();
                MessageBox.Show($"CAM{Glob.CamNumber + 1} Master Image 등록 완료", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btn_OpenMaster_Click(object sender, EventArgs e)
        {
            string ImageType;
            string ImageName = $"{Glob.MODELROOT}\\{Glob.RunnModel.Modelname()}\\Cam{Glob.CamNumber}\\CAM{Glob.CamNumber}_Master.bmp";
            FileInfo fileInfo = new FileInfo(ImageName);

            cdyDisplay.InteractiveGraphics.Clear();
            cdyDisplay.StaticGraphics.Clear();

            if (!fileInfo.Exists)
            {
                MessageBox.Show("마스터 이미지가 없습니다. 마스터이미지를 등록해주시기 바랍니다.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CogImageFileTool curimage = new CogImageFileTool();
            curimage.Operator.Open(ImageName, CogImageFileModeConstants.Read);
            curimage.Run();
            ImageType = curimage.OutputImage.GetType().ToString();
            if (ImageType.Contains("CogImage24PlanarColor"))
            {
                CogImageConvertTool imageconvert = new CogImageConvertTool();
                imageconvert.InputImage = curimage.OutputImage;
                imageconvert.RunParams.RunMode = CogImageConvertRunModeConstants.Plane2;
                imageconvert.Run();
                cdyDisplay.Image = (CogImage8Grey)imageconvert.OutputImage;
            }
            else
            {
                cdyDisplay.Image = (CogImage8Grey)curimage.OutputImage; //JPG 파일
            }
            //cdyDisplay.Image = (CogImage8Grey)curimage.OutputImage;
            cdyDisplay.Fit();
            GC.Collect();
        }
    }
}
