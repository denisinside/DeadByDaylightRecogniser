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
using static System.Drawing.Bitmap;
using System.Text.RegularExpressions;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Collections;
using System.Net.Http;

namespace DeadByDaylightRecogniser
{
    internal class ResultProcessing
    {

        #region Constants
        public const int ElementResizeSize = 128;

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

        private const double PerksLeftRatio = 0.155;
        private const double PerksTopRatio = 0.43;
        private const double PerksWidthRatio = 0.287;
        private const double PerksHeightRatio = 0.48;

        private const double OfferingLeftRatio = 0.46;
        private const double OfferingTopRatio = 0.35;
        private const double OfferingWidthRatio = 0.075;
        private const double OfferingHeightRatio = 0.65;

        private const double ItemLeftRatio = 0.55;
        private const double ItemTopRatio = 0.35;
        private const double ItemWidthRatio = 0.2;
        private const double ItemHeightRatio = 0.65;
        #endregion

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
                    Rect rect = new Rect(0,(int)(i * resultHeight + i * 0.0165 * resultHeight), grey.Width, (int)(resultHeight - i * 0.0165 * resultHeight));
                    playerResults[i] = t.T(new Mat(_result, rect));
                    string role = i != 4 ? "survivor" : "killer";
                    ProcessPlayerResult(playerResults[i], role);
                }
            }
        }


        /// <summary>
        /// Process the result for a single player.
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing the image of a player result</param>
        private void ProcessPlayerResult(Mat res, string role)
        {
            using (var t = new ResourcesTracker())
            {
                var prestige = ExtractPrestige(res, t);
                var score = ExtractScore(res, t);
                var character = ExtractCharacter(res, t);
                var perksBounds = ExtractPerks(res, t, role);

                //Cv2.ImShow("r", res);
                //Cv2.WaitKey();
                Console.WriteLine($"{prestige} - {character} - {score}");
            }

        }
        #endregion

        #region Extract Methods

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
        private string[] ExtractPerks(Mat res, ResourcesTracker t, string role)
        {
            var bounds = CalculateRect(res, PerksLeftRatio, PerksTopRatio, PerksWidthRatio, PerksHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            return ReadPerks(mat, "perks.json", role);
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
                    result = Regex.Replace(ocrResult, @"[^A-Z]", "").Trim();
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
        private string[] ReadPerks(Mat perks, string jsonPath, string role)
        {
            using (var t = new ResourcesTracker())
            {
                string json = File.ReadAllText(jsonPath);
                List<Perk> perkList = JsonSerializer.Deserialize<List<Perk>>(json);

                string[] perkNames = new string[4];

                for (int i = 1; i <= 4; i++)
                {
                    Mat perk = t.T(CropPerk(perks, i));
                    Cv2.Resize(perk, perk, new Size(ElementResizeSize, ElementResizeSize));
                    double alpha = 1.5;
                    double beta = 0;
                    perk.ConvertTo(perk, -1, alpha, beta);
                    Cv2.ImShow("br", perk);
                    Cv2.WaitKey();

                    Mat greyPerk = new Mat();
                    Cv2.CvtColor(perk, greyPerk, ColorConversionCodes.BGR2GRAY);

                    var orb = ORB.Create();
                    KeyPoint[] keypointsPerk;
                    Mat descriptorsPerk = new Mat();
                    orb.DetectAndCompute(greyPerk, null, out keypointsPerk, descriptorsPerk);


                    var bfMatcher = new BFMatcher(NormTypes.Hamming, crossCheck: true);

                    double maxMatchScore = 0;
                    string bestMatchPerkName = "Not Detected";

                    foreach (var perkTemplate in perkList.FindAll(e => e.Role.Equals(role.ToLower())))
                    {
                            Mat templateDescriptors = Cv2.ImDecode(perkTemplate.Descriptors, ImreadModes.Grayscale);

                            var matches = bfMatcher.Match(descriptorsPerk, templateDescriptors);
                            double matchScore = matches.Length;

                        //Console.WriteLine($"Perk {i}: {perkTemplate.Name} with {matchScore} matches.");
                        if (matchScore > maxMatchScore && matchScore > 30)
                            {
                                maxMatchScore = matchScore;
                                bestMatchPerkName = perkTemplate.Name;
                            }
                    }

                    perkNames[i - 1] = bestMatchPerkName;
                    Console.WriteLine($"Perk {i}: {bestMatchPerkName} with {maxMatchScore} matches.");
                }

                return perkNames;
            }
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
        /// <summary>
        /// Reads .png image with correct alpha channel.
        /// </summary>
        /// <param name="imagePath">Path to the .png image.</param>
        /// <returns>The <see cref="Mat"/> object, that will contain the result of reading the image.</returns>
        public static Mat ReadPNG(string imagePath)
        {
            using(var t = new ResourcesTracker())
            {
                Mat imageWithAlpha = Cv2.ImRead(imagePath, ImreadModes.Unchanged);

                if (imageWithAlpha.Channels() == 4)
                {
                    Mat[] channels = t.T(Cv2.Split(imageWithAlpha));
                    Mat bgr = t.NewMat();
                    Cv2.Merge(new Mat[] { channels[0], channels[1], channels[2] }, bgr);

                    Mat alpha = channels[3];
                    Mat mask = t.NewMat();
                    Cv2.Threshold(alpha, mask, 0, 255, ThresholdTypes.Binary);

                    Mat result = new Mat();
                    bgr.CopyTo(result, mask);
                    imageWithAlpha.Dispose();
                    return result;
                }
                else
                {
                    return imageWithAlpha;
                }
            }
        }
        /// <summary>
        /// Crops the screen image to specified perk.
        /// </summary>
        /// <param name="perks">The <see cref="Mat"/> object representing the image with the list of perks.</param>
        /// <param name="number">The number of the perk in the list.</param>
        /// <returns>The <see cref="Mat"/> object representing the image with the specified perk.</returns>
        private Mat CropPerk(Mat perks, int number)
        {
            double perkWidthRatio = number == 4 ? 0.22 : 0.25;
            double perkHeightRatio = 0.95;
            double perkLeftRatio = number == 4
                ? 1 - perkWidthRatio 
                : 0.009 * (number - 1) + perkWidthRatio * (number - 1); 
            double perkTopRatio = 0.05;

            var bounds = CalculateRect(perks, perkLeftRatio, perkTopRatio, perkWidthRatio, perkHeightRatio);

            Mat croppedImage = new Mat(perks, bounds);

            Mat mask = Mat.Zeros(croppedImage.Size(), MatType.CV_8UC1);
            Point center = new Point(croppedImage.Width / 2, croppedImage.Height / 2);
            int size = Math.Min(croppedImage.Width, croppedImage.Height) / 2;
            Point[] vertices = {
                new Point(center.X, center.Y + 1.15 * size),
                new Point(center.X - 1.15 * size, center.Y),
                new Point(center.X, center.Y - 0.95 * size),
                new Point(center.X + 0.95 * size, center.Y)
            };

            Cv2.FillConvexPoly(mask, vertices, Scalar.White);

            Mat result = new Mat();
            croppedImage.CopyTo(result, mask);
            Mat resultWithAlpha = new Mat(croppedImage.Size(), MatType.CV_8UC4);

            Cv2.CvtColor(result, resultWithAlpha, ColorConversionCodes.BGR2BGRA);
            for (int y = 0; y < resultWithAlpha.Rows; y++)
            {
                for (int x = 0; x < resultWithAlpha.Cols; x++)
                {
                    if (mask.At<byte>(y, x) == 0)
                    {
                        resultWithAlpha.At<Vec4b>(y, x)[3] = 0;  
                    }
                    else
                    {
                        resultWithAlpha.At<Vec4b>(y, x)[3] = 255;  
                    }
                }
            }
            return resultWithAlpha;
        }
        #endregion
    }
}
