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
using DeadByDaylightRecogniser.Utils.Enums;

namespace DeadByDaylightRecogniser
{
    internal class ResultProcessing
    {

        #region Constants
        public const int ElementResizeSize = 128;

        private const double ScreenLeftRatio = 0.04;
        private const double ScreenTopRatio = 0.24;
        private const double ScreenWidthRatio = 0.391;
        private const double ScreenHeightRatio = 0.541;

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
            using var t = new ResourcesTracker();
            Mat grey = t.NewMat();
            Cv2.CvtColor(_result, grey, ColorConversionCodes.BGR2GRAY);
            Mat[] playerResults = new Mat[5];
            int resultHeight = (int)(grey.Height * 0.2);
            for (int i = 0; i < 5; i++)
            {
                var rect = new Rect(0, (int)(i * resultHeight + i * 0.0165 * resultHeight), grey.Width, (int)(resultHeight - i * 0.0165 * resultHeight));
                playerResults[i] = t.T(new Mat(_result, rect));
                Role role = i != 4 ? Role.Survivor : Role.Killer;
                ProcessPlayerResult(playerResults[i], role);
            }
        }


        /// <summary>
        /// Process the result for a single player.
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing the image of a player result</param>
        private void ProcessPlayerResult(Mat res, Role role)
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
        private string[] ExtractPerks(Mat res, ResourcesTracker t, Role role)
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
        private string ExtractOffering(Mat res, ResourcesTracker t, Role role)
        {
            var bounds = CalculateRect(res, OfferingLeftRatio, OfferingTopRatio, OfferingWidthRatio, OfferingHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            return ReadOffering(mat, "offerings.json", role);
        }
        /// <summary>
        /// Extracts the name of player's item and names of its addons from the image. 
        /// </summary>
        /// <param name="res">The <see cref="Mat"/> object representing image to be processed.</param>
        /// <param name="t">The <see cref="ResourcesTracker"/> object to dispose <see cref="Mat"/> objects in a code.</param>
        /// <returns>A <see cref="Item"/> object representing the player's item name and its addons names. Returns null if recognition fails.</returns>
        private DBDElement[] ExtractItem(Mat res, ResourcesTracker t, Role role)
        {
            var bounds = CalculateRect(res, ItemLeftRatio, ItemTopRatio, ItemWidthRatio, ItemHeightRatio);
            var mat = t.T(new Mat(res, bounds));
            Window.ShowImages(mat);
            return ReadItem(mat, "items.json","addons.json", role);
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
        /// Processes the image to extract the number.
        /// </summary>
        /// <param name="number">The <see cref="Mat"/> object representing the area in the image with the number.</param>
        /// <returns>An integer representing the recognized number. Returns 0 if recognition fails.</returns>
        private int ReadNumber(Mat number)
        {
            var ocrResult = ReadOCR(number);
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
        /// <summary>
        /// Processes the image to extract the name of player's character.
        /// </summary>
        /// <param name="character">The <see cref="Mat"/> object representing the area in the image with the character's name.</param>
        /// <returns>A string representing the recognized character's name.</returns>
        private string ReadCharacter(Mat character)
        {
            var ocrResult = ReadOCR(character);
            var result = Regex.Replace(ocrResult, @"[^A-Z\s]", "").Trim();
            return result;
        }
        /// <summary>
        /// Additional method to processes the image to extract and recognize the perk using OCR.
        /// </summary>
        /// <param name="data">The <see cref="Mat"/> object representing the area in the image with the extracting perk.</param>
        /// <returns>A string reoresenting the recognized perk.</returns>
        private string ReadOCR(Mat data)
        {
            using var t = new ResourcesTracker();
            using var ocrInput = OcrApi.Create();
            Mat gray = t.NewMat();
            Cv2.CvtColor(data, gray, ColorConversionCodes.BGR2GRAY);

            Mat binary = t.NewMat();
            Cv2.Threshold(gray, binary, 128, 256, ThresholdTypes.Binary);
            Cv2.BitwiseNot(binary, binary);

            ocrInput.Init(Patagames.Ocr.Enums.Languages.English);
            string result = ocrInput.GetTextFromImage(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binary));
            return result;
        }

        /// <summary>
        /// Processes the image to extract and recognize the names of player's perks using OpenCVsharp.
        /// </summary>
        /// <param name="perks">The <see cref="Mat"/> object representing the area in the image with player's list of perks.</param>
        /// <returns>A string array with length 4 representing the recognized player's perks names. Returns null if recognition fails.</returns>
        private string[] ReadPerks(Mat perks, string jsonPath, Role role)
        {
            string json = File.ReadAllText(jsonPath);
            List<DBDElement> perkList = JsonSerializer.Deserialize<List<DBDElement>>(json);
            if (perkList == null)
                return null;

            string[] perkNames = new string[4];

            for (int i = 1; i <= 4; i++)
            {
                Mat perk = CropPerk(perks, i);
                var bestMatch = ReadDBDElement(perk, perkList, role);
                perk.Dispose();
                string name = bestMatch == null ? "Not Detected" : bestMatch.Value.Name;
                perkNames[i - 1] = name;
            }

            return perkNames;
        }
        /// <summary>
        /// Processes the image to extract and recognize the name of player's offering using OpemCVsharp.
        /// </summary>
        /// <param name="offering">The <see cref="Mat"/> object representing the area in the image with the player's offering.</param>
        /// <returns>A string representing the recognized offering name. Returns null if recognition fails.</returns>
        private string ReadOffering(Mat offering, string jsonPath, Role role)
        {
            return null;
        }
        /// <summary>
        /// Processes the image to extract and recognize the name of player's item and item's addons using OpenCVsharp.
        /// </summary>
        /// <param name="item">The <see cref="Mat"/> object representing the area in the image with the player's item and its addons.</param>
        /// <returns>A <see cref="Item"/> object representing the recognized item's name and its addons. Returns null if recognition fails.</returns>
        private DBDElement[] ReadItem(Mat item, string jsonItemsPath, string jsonAddonsPath, Role role)
        {
            return null;
        }
        /// <summary>
        /// Processes the image to extract the best match with data from <paramref name="elementData"/>
        /// </summary>
        /// <param name="element">The <see cref="Mat"/> object representing the area in the image with the element.</param>
        /// <param name="elementData">The <see cref="List{DBDElement}"/> with data of elements for mathcing.</param>
        /// <param name="role">The player's role to narrow chance of incorrect matching.</param>
        /// <returns></returns>
        private DBDElement? ReadDBDElement(Mat element, List<DBDElement> elementData, Role role)
        {
            using var t = new ResourcesTracker();
            Cv2.Resize(element, element, new Size(ElementResizeSize, ElementResizeSize));
            double alpha = 1.5;
            double beta = 0;
            element.ConvertTo(element, -1, alpha, beta);

            Cv2.ImShow("br", element);
            Cv2.WaitKey();

            Mat grey = t.NewMat();
            Cv2.CvtColor(element, grey, ColorConversionCodes.BGR2GRAY);

            var orb = ORB.Create();
            Mat elementDescriptors = t.NewMat();
            orb.DetectAndCompute(grey, null, out KeyPoint[] keypointsPerk, elementDescriptors);


            var bfMatcher = t.T(new BFMatcher(NormTypes.Hamming, crossCheck: true));

            double maxMatchScore = 0;
            DBDElement? bestMatch = null;
            foreach (var template in elementData.FindAll(e => e.Role == role))
            {
                Mat templateDescriptors = t.T(Cv2.ImDecode(template.Descriptors, ImreadModes.Grayscale));

                var matches = bfMatcher.Match(elementDescriptors, templateDescriptors);
                double matchScore = matches.Length;

                //Console.WriteLine($"DBDElement: {template.Name} with {matchScore} matches.");
                if (matchScore > maxMatchScore && matchScore > 30)
                {
                    maxMatchScore = matchScore;
                    bestMatch = template;
                }
            }
            if(bestMatch != null)
                Console.WriteLine($"DBDElement: {bestMatch.Value.Name} with {maxMatchScore} matches.");
            return bestMatch;
        }
        #endregion

        #region Additional Methods

        /// <summary>
        /// Crops the screen image to the region of interest.
        /// </summary>
        private void CropScreen()
        {
            var roi = CalculateRect(_result, ScreenLeftRatio, ScreenTopRatio, ScreenWidthRatio, ScreenHeightRatio);

            var croppedImage = new Mat(_result, roi);
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
            using var t = new ResourcesTracker();
            Mat imageWithAlpha = Cv2.ImRead(imagePath, ImreadModes.Unchanged);

            if (imageWithAlpha.Channels() == 4)
            {
                Mat[] channels = t.T(Cv2.Split(imageWithAlpha));
                Mat bgr = t.NewMat();
                Cv2.Merge([channels[0], channels[1], channels[2]], bgr);

                Mat alpha = channels[3];
                Mat mask = t.NewMat();
                Cv2.Threshold(alpha, mask, 0, 255, ThresholdTypes.Binary);

                Mat result = new();
                bgr.CopyTo(result, mask);
                imageWithAlpha.Dispose();
                return result;
            }
            else
            {
                return imageWithAlpha;
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
            using var t = new ResourcesTracker();
            double perkWidthRatio = number == 4 ? 0.22 : 0.25;
            double perkHeightRatio = 0.95;
            double perkLeftRatio = number == 4
                ? 1 - perkWidthRatio
                : 0.009 * (number - 1) + perkWidthRatio * (number - 1);
            double perkTopRatio = 0.05;

            var bounds = CalculateRect(perks, perkLeftRatio, perkTopRatio, perkWidthRatio, perkHeightRatio);

            Mat croppedImage = t.T(new Mat(perks, bounds));

            Mat mask = t.T(Mat.Zeros(croppedImage.Size(), MatType.CV_8UC1));
            Point center = new(croppedImage.Width / 2, croppedImage.Height / 2);
            int size = Math.Min(croppedImage.Width, croppedImage.Height) / 2;
            Point[] vertices = {
                new(center.X, center.Y + 1.15 * size),
                new(center.X - 1.15 * size, center.Y),
                new(center.X, center.Y - 0.95 * size),
                new(center.X + 0.95 * size, center.Y)
            };

            Cv2.FillConvexPoly(mask, vertices, Scalar.White);

            Mat result = t.NewMat();
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
