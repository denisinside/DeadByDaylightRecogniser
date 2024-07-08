using DeadByDaylightRecogniser.Models;
using OpenCvSharp;
using OpenCvSharp.Text;
using Patagames.Ocr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;

namespace DeadByDaylightRecogniser
{
    internal class ResultProcessing
    {
        private Mat _result;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ResultProcessing"/> class.
        /// </summary>
        /// <param name="imagePath">The path to the image to be processed.</param>
        public ResultProcessing(string imagePath)
        {
            _result = Cv2.ImRead(imagePath, ImreadModes.Color);
            CropScreen();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultProcessing"/> class.
        /// </summary>
        /// <param name="image">The <see cref="Mat"/> object representing the image to be processed.</param>
        public ResultProcessing(Mat image)
        {
            _result = image;
            CropScreen();
        }
        #endregion

        #region Processing methods
        /// <summary>
        /// Processes the image to recognise and extract various elemetns such as prestige, score, perks, status, perks, character, item for each player.
        /// </summary>
        public void Process()
        {
            using (var t = new ResourcesTracker())
            {
                Mat grey = t.NewMat();
                Cv2.CvtColor(_result, grey, ColorConversionCodes.BGR2GRAY);
                Mat[] playerResults = new Mat[5];
                int resultHeight = (int)(grey.Height * 0.2);
                for (int i = 0; i < 5; i++)
                {
                    Rect rect = new Rect(0, i * resultHeight, grey.Width, resultHeight);
                    playerResults[i] = t.T(new Mat(_result, rect));
                    ProcessPlayerResult(playerResults[i]);
                }
                //Window.ShowImages(playerResults[0], playerResults[1], playerResults[2], playerResults[3], playerResults[4]);



                /* Mat blur = t.NewMat();
                 Cv2.GaussianBlur(grey, blur, new Size(7, 7), 3, 4);
                 Mat canny = t.NewMat();
                 Cv2.Canny(blur, canny, 15, 100);
                 Cv2.Dilate(canny, canny, t.NewMat(), null, 2);
                 //Window.ShowImages(canny);
                 Point[][] contours;
                 HierarchyIndex[] hierarchyIndexes;
                 Cv2.FindContours(canny, out contours, out hierarchyIndexes, mode: RetrievalModes.External, method: ContourApproximationModes.ApproxSimple);

                 int perk_times = 0;
                 Mat perk = t.T(new Mat(
                     "C:\\Users\\denisinside\\source\\repos\\DeadByDaylightRecogniser\\DeadByDaylightRecogniser\\DeadByDaylightRecogniser\\perk2.png",
                     ImreadModes.Color));
                 Cv2.CvtColor(perk, perk, ColorConversionCodes.BGR2GRAY);
                 Cv2.Resize(perk, perk, new Size(64, 64));
                 Mat blur1 = t.NewMat();
                 Cv2.GaussianBlur(perk, blur1, new Size(7, 7), 3, 4);
                 Mat canny1 = t.NewMat();
                 Cv2.Canny(blur1, canny1, 15, 100);
                 Cv2.Dilate(canny1, canny1, t.NewMat(), null, 3);
                 foreach (Point[] contour in contours)
                 {
                     var rect = Cv2.BoundingRect(contour);
                     Cv2.Rectangle(_result, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height), Scalar.BlueViolet);

                     Mat mat = t.T(new Mat(canny, rect));
                     Console.WriteLine($"mat: {mat.Width}x{mat.Height}, perk: {perk.Width}x{perk.Height}");
                     if (mat.Width <= perk.Width && mat.Height <= perk.Height)
                     {
                         Mat outMat = t.NewMat();
                         double res1, res2;
                         Cv2.MatchTemplate(mat, canny1, outMat, TemplateMatchModes.CCoeffNormed);
                         Cv2.MinMaxLoc(outMat, out res1, out res2);
                         Console.WriteLine("min: " + res1 + ", max: " + res2);
                         Cv2.PutText(_result, $"{Math.Round(res2, 2, MidpointRounding.AwayFromZero)}", new Point(rect.X + rect.Width / 3, rect.Y + rect.Height/2), HersheyFonts.HersheyTriplex, 0.5, Scalar.White);
                         if (res2 > 0.8)
                         {
                             perk_times++;
                             Cv2.Rectangle(_result, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height), Scalar.Red);
                         }
                         Cv2.Resize(mat, mat, new Size(64, 64));
                      //  if (Math.Round(res2, 2, MidpointRounding.AwayFromZero) == 0.58)
                     }
                     else
                     {
                         Console.WriteLine("Пропускаем область, так как она меньше шаблона");
                     }
                 }

                 //Window.ShowImages(canny1, canny);
                 Window.ShowImages(_result, canny1, canny);*/
            }
        }


        /// <summary>
        /// Process the result for a single player.
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing the image of a player result</param>
        private void ProcessPlayerResult(Mat res)
        {
            using (var t = new ResourcesTracker())
            {
                var prestige = ExtractPrestige(res, t);
                var score = ExtractScore(res, t);
                var character = ExtractCharacter(res, t);
                var perksBounds = ExtractItem(res, t);

                Console.WriteLine($"{prestige} - {character} - {score}");
            }

        }
        #endregion

        #region Extract Methods

        #region Constants
        private const double PrestigeLeftRatio = 0.02;
        private const double PrestigeTopRatio = 0.35;
        private const double PrestigeWidthRatio = 0.09;
        private const double PrestigeHeightRatio = 0.35;

        private const double ScoreLeftRatio = 0.75;
        private const double ScoreTopRatio = 0.4;
        private const double ScoreWidthRatio = 0.25;
        private const double ScoreHeightRatio = 0.5;

        private const double CharacterLeftRatio = 0.2;
        private const double CharacterTopRatio = 0.0;
        private const double CharacterWidthRatio = 0.5;
        private const double CharacterHeightRatio = 0.3;

        private const double PerksLeftRatio = 0.15;
        private const double PerksTopRatio = 0.35;
        private const double PerksWidthRatio = 0.3;
        private const double PerksHeightRatio = 0.65;

        private const double OfferingLeftRatio = 0.46;
        private const double OfferingTopRatio = 0.35;
        private const double OfferingWidthRatio = 0.075;
        private const double OfferingHeightRatio = 0.65;

        private const double ItemLeftRatio = 0.55;
        private const double ItemTopRatio = 0.35;
        private const double ItemWidthRatio = 0.2;
        private const double ItemHeightRatio = 0.65;
        #endregion

        /// <summary>
        /// Extracts the prestige number from the image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing image to be processed.</param>
        /// <param name="t">The <see cref="ResourcesTracker"/> object to dispose <see cref="Mat"/> objects in a code.</param>
        /// <returns>An integer representing the prestige number. Returns 0 if recognition fails.</returns>
        private int ExtractPrestige(Mat res, ResourcesTracker t)
        {
            var bounds = CalculateRect(res, PrestigeLeftRatio, PrestigeTopRatio, PrestigeWidthRatio, PrestigeHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            return ReadNumber(mat);
        }
        /// <summary>
        /// Extracts the score number from the image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing image to be processed.</param>
        /// <param name="t">The <see cref="ResourcesTracker"/> object to dispose <see cref="Mat"/> objects in a code.</param>
        /// <returns>An integer representing the score number. Returns 0 if recognition fails.</returns>
        private int ExtractScore(Mat res, ResourcesTracker t)
        {
            var bounds = CalculateRect(res, ScoreLeftRatio, ScoreTopRatio, ScoreWidthRatio, ScoreHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            return ReadNumber(mat);
        }
        /// <summary>
        /// Extracts the name of the player's character from the image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing image to be processed.</param>
        /// <param name="t">The <see cref="ResourcesTracker"/> object to dispose <see cref="Mat"/> objects in a code.</param>
        /// <returns>A string representing the character name. Returns null if recognition fails.</returns>
        private string ExtractCharacter(Mat res, ResourcesTracker t)
        {
            var bounds = CalculateRect(res, CharacterLeftRatio, CharacterTopRatio, CharacterWidthRatio, CharacterHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            return ReadCharacter(mat);
        }
        /// <summary>
        /// Extracts the names of the player's perks from the image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing image to be processed.</param>
        /// <param name="t">The <see cref="ResourcesTracker"/> object to dispose <see cref="Mat"/> objects in a code.</param>
        /// <returns>A string array representing names of player's perks. Returns null if recognition fails.</returns>
        private string[] ExtractPerks(Mat res, ResourcesTracker t)
        {
            var bounds = CalculateRect(res, PerksLeftRatio, PerksTopRatio, PerksWidthRatio, PerksHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            return ReadPerks(mat);
        }
        /// <summary>
        /// Extracts the name of player's offering from the image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing image to be processed.</param>
        /// <param name="t">The <see cref="ResourcesTracker"/> object to dispose <see cref="Mat"/> objects in a code.</param>
        /// <returns>A string representing the offering name. Returns null if recognition fails.</returns>
        private string ExtractOffering(Mat res, ResourcesTracker t)
        {
            var bounds = CalculateRect(res, OfferingLeftRatio, OfferingTopRatio, OfferingWidthRatio, OfferingHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            return ReadOffering(mat);
        }
        /// <summary>
        /// Extracts the name of player's item and names of its addons from the image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing image to be processed.</param>
        /// <param name="t">The <see cref="ResourcesTracker"/> object to dispose <see cref="Mat"/> objects in a code.</param>
        /// <returns>A <see cref="Item"/> object representing the player's item name and its addons names. Returns null if recognition fails.</returns>
        private Item? ExtractItem(Mat res, ResourcesTracker t)
        {
            var bounds = CalculateRect(res, ItemLeftRatio, ItemTopRatio, ItemWidthRatio, ItemHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            Window.ShowImages(mat);
            return ReadItem(mat);
        }
        /// <summary>
        /// Calculates the rect to extract the specified part of image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object that representing player's result.</param>
        /// <param name="leftRatio">Left ratio of image.</param>
        /// <param name="topRatio">Top ratio of image./param>
        /// <param name="widthRatio">Width ratio of image.</param>
        /// <param name="heightRatio">Height ratio of image.</param>
        /// <returns>Bounds of specified part of image.</returns>
        private Rect CalculateRect(Mat res, double leftRatio, double topRatio, double widthRatio, double heightRatio)
        {
            return new Rect(
                (int)(res.Width * leftRatio),
                (int)(res.Height * topRatio),
                (int)(res.Width * widthRatio),
                (int)(res.Height * heightRatio)
            );
        }
        #endregion

        #region Reading Methods
        /// <summary>
        /// Processes the image to extract and recognize the number using OCR.
        /// </summary>
        /// <param name="number">The <see cref="Mat"/> object representing the area in the image with the number.</param>
        /// <returns>An integer representing the recognized number. Returns 0 if recognition fails.</returns>
        private int ReadNumber(Mat number)
        {
            using (var t = new ResourcesTracker())
            using (var ocrInput = OcrApi.Create())
            {
                Mat gray = t.NewMat();
                Cv2.CvtColor(number, gray, ColorConversionCodes.BGR2GRAY);

                Mat binary = t.NewMat();
                Cv2.Threshold(gray, binary, 128, 256, ThresholdTypes.Binary);
                Cv2.BitwiseNot(binary, binary);

                ocrInput.Init(Patagames.Ocr.Enums.Languages.English);

                var ocrResult = ocrInput.GetTextFromImage(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary));
                string numericResult = Regex.Replace(ocrResult, @"\D", "").Trim();
                int result;
                try
                {
                    result = int.Parse(numericResult);
                }
                catch (Exception)
                {
                    result = 0;
                }
                return result;
            }
        }
        /// <summary>
        /// Processes the image to extract and recognize the name of player's character using OCR.
        /// </summary>
        /// <param name="character">The <see cref="Mat"/> object representing the area in the image with the character's name.</param>
        /// <returns>A string representing the recognized character's name. Returns null if recognition fails.</returns>
        private string ReadCharacter(Mat character)
        {
            using (var t = new ResourcesTracker())
            using (var ocrInput = OcrApi.Create())
            {
                Mat gray = t.NewMat();
                Cv2.CvtColor(character, gray, ColorConversionCodes.BGR2GRAY);

                Mat binary = t.NewMat();
                Cv2.Threshold(gray, binary, 128, 256, ThresholdTypes.Binary);
                Cv2.BitwiseNot(binary, binary);

                ocrInput.Init(Patagames.Ocr.Enums.Languages.English);
                string result;
                try
                {
                    var ocrResult = ocrInput.GetTextFromImage(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary));
                    result = Regex.Replace(ocrResult, @"[a-z\n]", "").Trim();
                }
                catch (Exception)
                {
                    result = null;
                }
                return result;
            }
        }
        /// <summary>
        /// Processes the image to extract and recognize the names of player's perks using OpenCVsharp.
        /// </summary>
        /// <param name="perks">The <see cref="Mat"/> object representing the area in the image with player's list of perks.</param>
        /// <returns>A string array with length 4 representing the recognized player's perks names. Returns null if recognition fails.</returns>
        private string[] ReadPerks(Mat perks)
        {
            return null;
        }
        /// <summary>
        /// Processes the image to extract and recognize the name of player's offering using OpemCVsharp.
        /// </summary>
        /// <param name="offering">The <see cref="Mat"/> object representing the area in the image with the player's offering.</param>
        /// <returns>A string representing the recognized offering name. Returns null if recognition fails.</returns>
        private string ReadOffering(Mat offering)
        {
            return null;
        }
        /// <summary>
        /// Processes the image to extract and recognize the name of player's item and item's addons using OpenCVsharp.
        /// </summary>
        /// <param name="item">The <see cref="Mat"/> object representing the area in the image with the player's item and its addons.</param>
        /// <returns>A <see cref="Item"/> object representing the recognized item's name and its addons. Returns null if recognition fails.</returns>
        private Item? ReadItem(Mat item)
        {
            return null;
        } 
        #endregion

        #region Additional Methods
        /// <summary>
        /// Crops the screen image to the region of interest.
        /// </summary>
        private void CropScreen()
        {
            int width = _result.Width;
            int height = _result.Height;

            int leftCut = (int)(width * .04d);   // 4% 
            int topCut = (int)(height * .24d);   // 24% 
            int rightCut = (int)(width * .57d);  // 57% 
            int bottomCut = (int)(height * .22d); // 22% 

            Rect roi = new Rect(leftCut, topCut, width - leftCut - rightCut, height - topCut - bottomCut);

            Mat croppedImage = new Mat(_result, roi);
            _result.Dispose();
            _result = croppedImage;
        } 
        #endregion
    }
}
