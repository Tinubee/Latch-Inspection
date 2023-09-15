using Cognex.VisionPro;
using Cognex.VisionPro.Dimensioning;
using Cognex.VisionPro.Display;
using KimLib;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VISION.Camera;
using VISION.Cogs;

namespace VISION.Schemas
{
    public class Inspection
    {
        private PGgloble Glob;
        private CogImage8Grey Fiximage;
        private string FimageSpace;

        public void Init()
        {
            Glob = PGgloble.getInstance;
        }

        public bool 이미지픽스쳐(CogDisplay cog, int CameraNumber)
        {
            int FixPatternNumber = 0;
            if (Glob.코그넥스파일.패턴툴[CameraNumber, FixPatternNumber].Run((CogImage8Grey)cog.Image))
            {
                int usePatternNumber = Glob.코그넥스파일.패턴툴[CameraNumber, FixPatternNumber].HighestResultToolNumber();
                Fiximage = Glob.코그넥스파일.모델.FixtureImage((CogImage8Grey)cog.Image, Glob.코그넥스파일.패턴툴[CameraNumber, FixPatternNumber].ResultPoint(usePatternNumber), Glob.코그넥스파일.패턴툴[CameraNumber, FixPatternNumber].ToolName(), CameraNumber, out FimageSpace, usePatternNumber);
                return true;
            }

            return false;
        }

        public void 패턴검사(CogDisplay cog, int 카메라번호, CogGraphicCollection Collection)
        {
            Glob.PatternResult[카메라번호] = true;
            int 패턴툴갯수 = Glob.코그넥스파일.모델.MultiPatterns().Length / PGgloble.CamCount;
            string[] temp = new string[패턴툴갯수];

            if (Glob.코그넥스파일.모델.MultiPattern_Inspection(ref cog, (CogImage8Grey)cog.Image, ref temp, 카메라번호, Collection)) //검사결과가 true 일때
            {
                for (int lop = 0; lop < 패턴툴갯수; lop++)
                {
                    if (Glob.코그넥스파일.패턴툴사용여부[카메라번호, lop] == true)
                    {
                        if (Glob.코그넥스파일.패턴툴[카메라번호, lop].Threshold() * 100 > Glob.MultiInsPat_Result[카메라번호, lop])
                        {
                            Glob.InspectResult[카메라번호] = false;
                            Glob.PatternResult[카메라번호] = false;
                        }
                    }
                }
            }
            else
            {
                Glob.InspectResult[카메라번호] = false;
                Glob.PatternResult[카메라번호] = false;
            }
        }

        public void 블롭검사(CogDisplay cog, int 카메라번호, CogGraphicCollection Collection)
        {
            Glob.BlobResult[카메라번호] = true;
            int 블롭툴갯수 = Glob.코그넥스파일.모델.Blob().Length / PGgloble.CamCount;
            string[] temp = new string[블롭툴갯수];

            //블롭툴 넘버와 패턴툴넘버 맞추는 작업.
            for (int toolnum = 0; toolnum < 블롭툴갯수; toolnum++)
            {
                if (Glob.코그넥스파일.블롭툴사용여부[카메라번호, toolnum])
                {
                    Bolb_Train(cog, 카메라번호, Glob.코그넥스파일.블롭툴픽스쳐번호[카메라번호, toolnum]);
                    Glob.코그넥스파일.블롭툴[카메라번호, toolnum].Area_Affine_Main1(ref cog, (CogImage8Grey)cog.Image, Glob.코그넥스파일.블롭툴픽스쳐번호[카메라번호, toolnum].ToString());
                }
            }
            //******************************Blob Tool Run******************************//
            if (Glob.코그넥스파일.모델.Blob_Inspection(ref cog, (CogImage8Grey)cog.Image, ref temp, 카메라번호, Collection) == false)
            {
                Glob.InspectResult[카메라번호] = false;
                Glob.BlobResult[카메라번호] = false;
            }
        }

        public void 치수검사(CogDisplay cog, int 카메라번호, CogGraphicCollection Collection)
        {

            Glob.MeasureResult[카메라번호] = true;
            int 치수검사툴갯수 = Glob.코그넥스파일.모델.Distancess().Length;
            string[] temp = new string[치수검사툴갯수];

            if (Glob.코그넥스파일.모델.Dimension_Inspection(ref cog, (CogImage8Grey)cog.Image, ref temp, 카메라번호, Collection))
            {
                CogCreateGraphicLabelTool[] Point_Label = new CogCreateGraphicLabelTool[10];
                CogCreateGraphicLabelTool[] Label = new CogCreateGraphicLabelTool[10];

                for (int lop = 1; lop < Glob.코그넥스파일.거리측정툴.Length / PGgloble.CamCount; lop++)
                {
                    if (Glob.코그넥스파일.거리측정툴사용여부[카메라번호, lop])
                    {
                        double ResultValue = 0;
                        ResultValue = Glob.코그넥스파일.거리측정툴[카메라번호, lop].DistanceValue(lop) * Glob.코그넥스파일.보정값[카메라번호, lop];

                        Point_Label[lop] = new CogCreateGraphicLabelTool();
                        Point_Label[lop].InputImage = cog.Image;
                        Point_Label[lop].InputGraphicLabel.X = Glob.코그넥스파일.거리측정툴[카메라번호, lop].GetX(lop);
                        Point_Label[lop].InputGraphicLabel.Y = Glob.코그넥스파일.거리측정툴[카메라번호, lop].GetY(lop);
                        Point_Label[lop].InputGraphicLabel.Text = ResultValue.ToString("F3");

                        Label[lop] = new CogCreateGraphicLabelTool();
                        Label[lop].InputImage = cog.Image;
                        Label[lop].InputGraphicLabel.X = 600;
                        Label[lop].InputGraphicLabel.Y = 170 + (80 * lop);
                        Label[lop].InputGraphicLabel.Text = $"{Glob.코그넥스파일.거리측정툴[카메라번호, lop].ToolName(lop)} : {ResultValue.ToString("F3")}";
                        Label[lop].Run();
                        Collection.Add(Label[lop].GetOutputGraphicLabel());

                        if (Glob.코그넥스파일.최소값[카메라번호, lop] <= ResultValue && Glob.코그넥스파일.최대값[카메라번호, lop] >= ResultValue)
                        {
                            Point_Label[lop].OutputColor = CogColorConstants.Green;
                            Collection[lop - 1].Color = CogColorConstants.Green;
                        }
                        else
                        {
                            Point_Label[lop].OutputColor = CogColorConstants.Red;
                            Collection[lop - 1].Color = CogColorConstants.Red;
                            Glob.InspectResult[카메라번호] = false;
                            Glob.MeasureResult[카메라번호] = false;
                        }

                        Point_Label[lop].Run();
                        cog.StaticGraphics.Add(Point_Label[lop].GetOutputGraphicLabel(), "");
                    }
                }
            }
            else
            {
                Glob.InspectResult[카메라번호] = false;
            }
        }

        public void 검사결과표시(CogDisplay cog, int 카메라번호, CogGraphicCollection Collection)
        {
            CogGraphicCollection Collection2 = new CogGraphicCollection(); // 패턴
            CogGraphicCollection Collection3 = new CogGraphicCollection(); // 블롭

            if (Glob.PatternResult[카메라번호]) { DisplayLabelShow(Collection2, cog, 600, 100, 0, "PATTERN OK"); }
            else { DisplayLabelShow(Collection2, cog, 600, 100, 0, "PATTERN NG"); };

            if (Glob.BlobResult[카메라번호]) { DisplayLabelShow(Collection3, cog, 600, 170, 0, "BLOB OK"); }
            else { DisplayLabelShow(Collection3, cog, 600, 170, 0, "BLOB NG"); };

            for (int i = 0; i < Collection.Count; i++)
            {
                if (Collection[i] == null)
                {
                    continue;
                }
                if (Collection[i].ToString() == "Cognex.VisionPro.CogGraphicLabel")
                    Collection[i].Color = CogColorConstants.Blue;
            }

            for (int i = 0; i < Collection2.Count; i++)
                Collection2[i].Color = Glob.PatternResult[카메라번호] == true ? CogColorConstants.Green : CogColorConstants.Red;
            for (int i = 0; i < Collection3.Count; i++)
                Collection3[i].Color = Glob.BlobResult[카메라번호] == true ? CogColorConstants.Green : CogColorConstants.Red;

            cog.StaticGraphics.AddList(Collection, "");
            cog.StaticGraphics.AddList(Collection2, "");
            cog.StaticGraphics.AddList(Collection3, "");
        }

        public bool Run(CogDisplay cog, 카메라구분 구분)
        {
            int 카메라번호 = Convert.ToInt32(구분) - 1;
            Glob.InspectResult[카메라번호] = true; //검사 결과는 초기에 무조건 true로 되어있다.
            CogGraphicCollection Collection = new CogGraphicCollection();

            if (!이미지픽스쳐(cog, 카메라번호)) return false;

            패턴검사(cog, 카메라번호, Collection);
            블롭검사(cog, 카메라번호, Collection);
            치수검사(cog, 카메라번호, Collection);

            검사결과표시(cog, 카메라번호, Collection);
          
            return Glob.InspectResult[카메라번호];
        }

        public void Bolb_Train(CogDisplay cdy, int CameraNumber, int toolnumber)
        {
            if (Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].Run((CogImage8Grey)cdy.Image) == true)
            {
                int usePatternNumber = Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].HighestResultToolNumber();
                Fiximage = Glob.코그넥스파일.모델.Blob_FixtureImage1((CogImage8Grey)cdy.Image, Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].ResultPoint(usePatternNumber), Glob.코그넥스파일.패턴툴[CameraNumber, toolnumber].ToolName(), CameraNumber, toolnumber, out FimageSpace, usePatternNumber);
            }
        }

        public void DisplayLabelShow(CogGraphicCollection Collection, CogDisplay cog, int X, int Y, double rotate, string Text)
        {
            CogCreateGraphicLabelTool Label = new CogCreateGraphicLabelTool();
            Label.InputGraphicLabel.Color = CogColorConstants.Green;
            Label.InputImage = cog.Image;
            Label.InputGraphicLabel.X = rotate == 0 ? X : Y;
            Label.InputGraphicLabel.Y = rotate == 0 ? Y : X;
            Label.InputGraphicLabel.Rotation = rotate;
            Label.InputGraphicLabel.Text = Text;
            Label.Run();
            Collection.Add(Label.GetOutputGraphicLabel());
        }
    }

}
