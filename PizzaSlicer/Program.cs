﻿// MIT License
// Google Hash Code 2018 - Training Round
// Team: root.cake
// Happy Coding <3

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PizzaSlicer
{
    public class Program
    {
        public const char Tomato = 'T';
        public const char Mushroom = 'M';
        public const char Empty = '0';

        private static string _directoryForOutput;
        private static string _inputFileName;

        private static SlicingAttempts _theUltimateSlice;

        private static void Main()
        {
            var defaultDir = @"D:\output\";
            Console.Write($"Enter directory to store outputs ({defaultDir}):\t");
            var directoryForOutput = Console.ReadLine();
            _directoryForOutput = string.IsNullOrEmpty(directoryForOutput)
                ? defaultDir
                : directoryForOutput;
            if (!Directory.Exists(_directoryForOutput))
            {
                try
                {
                    Directory.CreateDirectory(_directoryForOutput);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not create/use specified output-directory:");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Exiting...");

                    Console.ReadLine();
                    return;
                }
            }

            Console.WriteLine();
            Console.WriteLine("<-----~-----+- STARTED -+-----~----->");
            Console.WriteLine();

            var threadStart = new ThreadStart(SelectPizzaToSlice);
            var thread = new Thread(threadStart, 102400000);
            thread.Start();
        }

        private static Pizza _pizza;

        private static string _inputPath;

        private static string PathToSave(string operationName, string extension)
        {
            var combinedFileName = _inputFileName + "__" + operationName + extension;
            return Path.Combine(_directoryForOutput, combinedFileName);
        }

        public static void SelectPizzaToSlice()
        {
            var pizzas = GetAvailablePizzas(Path.Combine(Environment.CurrentDirectory, "input"));
            if (pizzas == null)
            {
                Console.WriteLine("No Pizza to slice.. There is no dinner!");
                return;
            }

            Console.WriteLine("Found following pizza at ./input directory:");
            for (var i = 0; i < pizzas.Count; i++)
            {
                var pizza = pizzas.ElementAt(i).Key;

                Console.WriteLine($"[{i + 1}] {pizza}");
            }

            var validInt = false;
            var index = 0;
            while (!validInt)
            {
                Console.Write("Select which pizza to slice: ");
                var indexStr = Console.ReadLine();

                validInt =
                    int.TryParse(indexStr, out index) &&
                    (index >= 1 && index <= pizzas.Count);
            }

            index--; // to set into zero-started indexing

            SliceThisPizza(pizzas.ElementAt(index).Value);
        }

        public static Dictionary<string, string> GetAvailablePizzas(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return null;

            var inputFiles = Directory.GetFiles(folderPath);
            if (inputFiles.Length == 0) return null;

            var dic = new Dictionary<string, string>(inputFiles.Length);
            foreach (var inputFile in inputFiles)
            {
                var fileName = Path.GetFileName(inputFile);
                dic.Add(fileName, inputFile);
            }

            return dic;
        }

        private static void SliceThisPizza(string pizzaPath)
        {
            _inputPath = pizzaPath;
            _pizza = new Pizza(_inputPath);
            _inputFileName = Path.GetFileName(_inputPath);

            SlicingUltimate(_pizza.Clone());
        }

        private static SizePossibleSlice GetPossibleSliceDimensions(int minCells, int maxCells)
        {
            Console.WriteLine("Getting possible dimensions of the slices..");
            var attemptsPossible = new SizePossibleSlice();

            //finding all possible slices dimensions
            for (var cellsInSlice = minCells; cellsInSlice <= maxCells; cellsInSlice++)
            {
                for (var rowsInSlice = 1; rowsInSlice <= cellsInSlice; rowsInSlice++)
                {
                    //checking for valid rowsCount x columnsCount combination
                    if ((cellsInSlice % rowsInSlice) != 0) continue;
                    //columnsCount count
                    var colsInSlice = cellsInSlice / rowsInSlice;
                    //reaching this far means the combination is acceptable
                    attemptsPossible.Push(rowsInSlice, colsInSlice);
                }
            }

            Console.WriteLine($"Possible dimensions of slices had been calculated: {attemptsPossible.Count}");
            return attemptsPossible;
        }

        private static string GetSliceOutputLine(SlicePizza slice)
        {
            return slice.StartPoint.X + " " +
                   slice.StartPoint.Y + " " +
                   slice.EndPoint.X + " " +
                   slice.EndPoint.Y;
        }

        public static event PropertyChangedEventHandler UltimateSliceChangedEvent;

        protected static void OnUltimateSliceChanged([CallerMemberName] string propertyName = null)
        {
            UltimateSliceChangedEvent?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }


        private static void UltimateSliceChanged_EventHandler(object sender, PropertyChangedEventArgs e)
        {
            var operationName = "ultimate-slicing-" + _theUltimateSlice.TotalSlicedCells;

            var slicePizzaContent = _theUltimateSlice.SlicePizzaContent;
            var pizzaRowsPizzaCount = _pizza.RowsPizzaCount;
            var pizzaColumnsPizzaCount = _pizza.ColumnsPizzaCount;

            GenerateOutputFile(_theUltimateSlice.SuccessfulSlices, operationName);
            DrawPizza(slicePizzaContent, pizzaRowsPizzaCount, pizzaColumnsPizzaCount, operationName);
            WritePizzaContent(slicePizzaContent, pizzaRowsPizzaCount, pizzaColumnsPizzaCount, operationName);
        }

        private static void SlicingUltimate(Pizza pizza)
        {
            RedWriteLine("Starting the ULTIMATE pizza slicing.. Should retrieve all possible slicing layout(s)!");
            var possibleSlices = new List<PossibleSlices>();
            //_allProbableSlicingAttemptsCount = 1;

            var sizesPossibleSlice = GetPossibleSliceDimensions(pizza.CellsMinInSlice, pizza.CellsMaxInSlice);

            //looking into all valid slices from every single cell in the pizza
            Console.WriteLine("Getting all valid slices from every single cell in the pizza..");
            for (var row = 0; row < pizza.RowsPizzaCount; row++)
            {
                for (var column = 0; column < pizza.ColumnsPizzaCount; column++)
                {
                    var startPointSlice = new Point(row, column);
                    var slicesValid = new List<SlicePizza>();
                    foreach (var sizeSlice in sizesPossibleSlice)
                    {
                        var quadDirectionsEndPoints =
                            GetPossibleQuadDirectionalEndPoints(
                                row, column,
                                pizza.RowsPizzaCount, pizza.ColumnsPizzaCount,
                                sizeSlice);


                        slicesValid.AddRange(GetAllPossibleSliceForAnOrigin(pizza, quadDirectionsEndPoints,
                            startPointSlice));
                    }

                    if (slicesValid.Count > 0)
                    {
                        possibleSlices.Add(new PossibleSlices(startPointSlice, slicesValid));
                    }
                }
            }

            Console.WriteLine(
                $"All cells with valid slices originated at them: {possibleSlices.Count}");

            RedWriteLine("Starting generating all possible slicing layouts..");

            _theUltimateSlice = new SlicingAttempts(pizza);
            UltimateSliceChangedEvent += UltimateSliceChanged_EventHandler;

            GenerateAllPossibleSlicingLayouts(
                pizza.Clone(),
                possibleSlices,
                new List<SlicePizza>(),
                0,
                0);

            Console.WriteLine();
            Console.WriteLine("<-----~-----+- FINISHED -+-----~----->");
            Console.ReadLine();
        }

        private static void GenerateAllPossibleSlicingLayouts(
            Pizza pizza,
            List<PossibleSlices> possibleSlices,
            List<SlicePizza> successfulSlices,
            int totalSlicedCells,
            int currentPossibilityIndex)
        {
            if (currentPossibilityIndex < possibleSlices.Count)
                Parallel.For(currentPossibilityIndex, possibleSlices.Count, i =>
                {
                    var possibility = possibleSlices[i];
                    foreach (var slice in possibility.Slices)
                    {
                        var realSlice = new SlicePizza(pizza, possibility.OriginPoint, slice.EndPoint);
                        if (!realSlice.IsValidSlice) continue;

                        var pizzaCloned = pizza.Clone();

                        var nextPossibilityIndex = i + 1;

                        var successfulSlicesCloned = successfulSlices.GetRange(0, successfulSlices.Count);

                        pizzaCloned.CutSlice(realSlice);
                        successfulSlicesCloned.Add(realSlice);

                        var totalSlicedCellsCloned = totalSlicedCells + realSlice.Size;

                        //task = Task.Factory.StartNew(() =>
                        GenerateAllPossibleSlicingLayouts(
                            pizzaCloned,
                            possibleSlices,
                            successfulSlicesCloned,
                            totalSlicedCellsCloned,
                            nextPossibilityIndex);
                        //);
                    }
                });

            if (totalSlicedCells > _theUltimateSlice.TotalSlicedCells)
            {
                _theUltimateSlice =
                    new SlicingAttempts(
                        (char[,]) pizza.Content.Clone(),
                        successfulSlices,
                        totalSlicedCells);

                var sliceSummary =
                    $"Retrieved slicing layout, Successful Slices: {successfulSlices.Count}, Total Sliced Cells: {totalSlicedCells}";
                Console.WriteLine(sliceSummary);

                OnUltimateSliceChanged();

                File.AppendAllText(Path.Combine(_directoryForOutput, "out.txt"), sliceSummary + Environment.NewLine);
            }
        }

        private static List<SlicePizza> GetAllPossibleSliceForAnOrigin(
            Pizza pizza,
            List<Point> quadDirectionsEndPoints,
            Point startPointSlice)
        {
            var slicesValid = new List<SlicePizza>();
            foreach (var endPoint in quadDirectionsEndPoints)
            {
                var slice = new SlicePizza(pizza, startPointSlice, endPoint);
                if (slice.IsValidSlice)
                {
                    slicesValid.Add(slice);
                }
            }

            return slicesValid;
        }

        private static List<Point> GetPossibleQuadDirectionalEndPoints(
            int row,
            int column,
            int rowsCount,
            int colsCount,
            SizePossibleSlice possibleSliceSize)
        {
            //only negative coordinates are rejected
            var quadDirectionsEndPoints = new List<Point>();

            var maxRow = rowsCount - 1;
            var maxCol = colsCount - 1;

            var forwardSliceX = row + possibleSliceSize.RowsCount - 1;
            var forwardSliceY = column + possibleSliceSize.ColumnsCount - 1;

            var backwrdSliceX = row + possibleSliceSize.RowsCount - 1;
            var backwrdSliceY = column - possibleSliceSize.ColumnsCount + 1;

            var upMrrorSliceX = row - possibleSliceSize.RowsCount + 1;
            var upMrrorSliceY = column + possibleSliceSize.ColumnsCount - 1;

            var diagnalSliceX = row - possibleSliceSize.RowsCount + 1;
            var diagnalSliceY = column - possibleSliceSize.ColumnsCount + 1;


            if (forwardSliceX >= 0 && forwardSliceY >= 0)
                if (forwardSliceX <= maxRow && forwardSliceY <= maxCol)
                    quadDirectionsEndPoints.Add(new Point(forwardSliceX, forwardSliceY));

            if (backwrdSliceX >= 0 && backwrdSliceY >= 0)
                if (backwrdSliceX <= maxRow && backwrdSliceY <= maxCol)
                    quadDirectionsEndPoints.Add(new Point(backwrdSliceX, backwrdSliceY));

            if (upMrrorSliceX >= 0 && upMrrorSliceY >= 0)
                if (upMrrorSliceX <= maxRow && upMrrorSliceY <= maxCol)
                    quadDirectionsEndPoints.Add(new Point(upMrrorSliceX, upMrrorSliceY));

            if (diagnalSliceX >= 0 && diagnalSliceY >= 0)
                if (diagnalSliceX <= maxRow && diagnalSliceY <= maxCol)
                    quadDirectionsEndPoints.Add(new Point(diagnalSliceX, diagnalSliceY));

            return quadDirectionsEndPoints;
        }

        private static void GenerateOutputFile(List<SlicePizza> successfulSlices, string operationName)
        {
            Console.WriteLine($"Generating output file of operation: {operationName}");
            var outputFileName = PathToSave(operationName, ".out");

            var linedSlices = successfulSlices.Select(x => GetSliceOutputLine(x)).ToList();

            File.WriteAllText(outputFileName, successfulSlices.Count + Environment.NewLine);
            File.AppendAllLines(outputFileName, linedSlices);

            Console.WriteLine($"Saving Output file of operation: {operationName}, to {outputFileName}");
        }

        private static void WritePizzaContent(char[,] pizzaContent, int rows, int columns, string operationName)
        {
            var pathToSave = PathToSave("sliced-pizzaCell-" + operationName, ".out");

            File.WriteAllText(pathToSave, string.Empty);

            for (int i = 0; i < rows; i++)
            {
                var lineContent = string.Empty;
                for (int j = 0; j < columns; j++)
                {
                    lineContent += pizzaContent[i, j];
                }

                File.AppendAllText(pathToSave, lineContent + Environment.NewLine);
            }
        }

        private static void DrawPizza(
            char[,] pizzaContent,
            int rowsCount,
            int columnsCount,
            string operationName)
        {
            Console.WriteLine($"Painting operation: {operationName}");
            using (var image = new Image<Rgba32>(rowsCount, columnsCount))
            {
                for (var row = 0; row < rowsCount; row++)
                {
                    for (var column = 0; column < columnsCount; column++)
                    {
                        var ingredientColorRgba = IngredientColorRgba(pizzaContent[row, column]);
                        image[row, column] = ingredientColorRgba;
                    }
                }

                try
                {
                    var outputFileName = PathToSave(operationName, ".bmp");
                    Console.WriteLine($"Saving Bitmap of operation: {operationName}, to {outputFileName}");
                    image.Save(outputFileName);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private static Rgba32 IngredientColorRgba(char pizzaCell)
        {
            Rgba32 ingredientColor;
            switch (pizzaCell)
            {
                case Tomato:
                    ingredientColor = Rgba32.Maroon;
                    break;
                case Mushroom:
                    ingredientColor = Rgba32.DarkSeaGreen;
                    break;
                default:
                    ingredientColor = Rgba32.AntiqueWhite;
                    break;
            }

            return ingredientColor;
        }

        private static void RedWriteLine(string str)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ForegroundColor = defaultColor;
        }

        private class PossibleSlices
        {
            public Point OriginPoint { get; }
            public List<SlicePizza> Slices { get; }

            public PossibleSlices(Point originPoint, List<SlicePizza> slices)
            {
                OriginPoint = originPoint;
                Slices = slices;
            }
        }

        private class SlicingAttempts
        {
            public char[,] SlicePizzaContent { get; }
            public List<SlicePizza> SuccessfulSlices { get; }
            public int TotalSlicedCells { get; }

            public SlicingAttempts(Pizza pizza)
            {
                SlicePizzaContent = new char[pizza.RowsPizzaCount, pizza.ColumnsPizzaCount];
                SuccessfulSlices = new List<SlicePizza>();
                TotalSlicedCells = 0;
            }

            public SlicingAttempts(
                char[,] slicePizzaContent,
                List<SlicePizza> successfulSlices,
                int totalSlicedCells)
            {
                SlicePizzaContent = slicePizzaContent;
                SuccessfulSlices = successfulSlices;
                TotalSlicedCells = totalSlicedCells;
            }
        }

        private class SizePossibleSlice : List<SizePossibleSlice>
        {
            public int RowsCount { private set; get; }
            public int ColumnsCount { private set; get; }

            public void Push(int rows, int columns)
            {
                Add(new SizePossibleSlice {RowsCount = rows, ColumnsCount = columns});
            }
        }

        public class SlicePizza
        {
            public int Size { get; }
            public Point StartPoint { get; }
            public Point EndPoint { get; }
            private int TomatoCount { get; }
            private int MushroomCount { get; }
            public bool IsValidSlice { get; }
            private char[,] Content { get; }

            public SlicePizza()
            {
            }

            public SlicePizza(Pizza pizza, Point startPoint, Point endPoint)
            {
                StartPoint = startPoint;
                EndPoint = endPoint;

                var rowsCount = Math.Abs(endPoint.X - startPoint.X) + 1;
                var colCount = Math.Abs(endPoint.Y - startPoint.Y) + 1;

                Content = new char[rowsCount, colCount];
                Size = rowsCount * colCount;

                var unSliceableArea = false;
                var rowsCounter = 0;

                var strtngX = Math.Min(startPoint.X, endPoint.X);
                var endingX = Math.Max(startPoint.X, endPoint.X);
                for (var row = strtngX; row <= endingX; row++)
                {
                    if (unSliceableArea) break;
                    var columnsCounter = 0;

                    var strtngY = Math.Min(startPoint.Y, endPoint.Y);
                    var endingY = Math.Max(startPoint.Y, endPoint.Y);
                    for (var column = strtngY; column <= endingY; column++)
                    {
                        var ingredient = pizza.Content[row, column];
                        Content[rowsCounter, columnsCounter] = ingredient;

                        if (ingredient == Tomato)
                            TomatoCount++;
                        else if (ingredient == Mushroom)
                            MushroomCount++;
                        else
                        {
                            unSliceableArea = true;
                            break;
                        }

                        columnsCounter++;
                    }

                    rowsCounter++;
                }


                if (unSliceableArea)
                    IsValidSlice = false;
                else
                {
                    var pizzaIngredintsMinInSlice = pizza.IngredientsMinInSlice;
                    IsValidSlice =
                        (TomatoCount >= pizzaIngredintsMinInSlice) &&
                        (MushroomCount >= pizzaIngredintsMinInSlice) &&
                        (Size >= pizza.CellsMinInSlice) &&
                        (Size <= pizza.CellsMaxInSlice);
                }
            }
        }

        public class Pizza
        {
            public int RowsPizzaCount { private set; get; }
            public int ColumnsPizzaCount { private set; get; }
            public int IngredientsMinInSlice { private set; get; }
            public int CellsMaxInSlice { private set; get; }
            public int CellsMinInSlice { private set; get; }
            public int Size { private set; get; }

            public char[,] Content { private set; get; }

            private Pizza()
            {
            }

            public Pizza(string pizzaPath)
            {
                var pizzaInput = File.ReadAllText(pizzaPath, Encoding.UTF8);

                var tokensPizza = pizzaInput.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

                var headerTokens = tokensPizza[0].Split(new[] {" "}, StringSplitOptions.None);
                RowsPizzaCount = int.Parse(headerTokens[0]);
                ColumnsPizzaCount = int.Parse(headerTokens[1]);
                IngredientsMinInSlice = int.Parse(headerTokens[2]);
                CellsMaxInSlice = int.Parse(headerTokens[3]);
                CellsMinInSlice = IngredientsMinInSlice * 2;
                Size = ColumnsPizzaCount * RowsPizzaCount;

                Content = new char[RowsPizzaCount, ColumnsPizzaCount];

                for (var counter = 1; counter < tokensPizza.Length; counter++)
                {
                    var rowsIndex = counter - 1;
                    for (var j = 0; j < tokensPizza[counter].Length; j++)
                    {
                        Content[rowsIndex, j] = tokensPizza[counter][j];
                    }
                }

                WritePizzaInfoToConsole(pizzaPath);
            }

            private void WritePizzaInfoToConsole(
                string pizzaPath)
            {
                Console.WriteLine($"Pizza \"{pizzaPath}\" has been loaded successfully...");
                Console.WriteLine($"Pizza is {RowsPizzaCount} x {ColumnsPizzaCount}");
                Console.WriteLine($"Min. # of cells in each slice = {CellsMinInSlice}");
                Console.WriteLine($"Max. # of cells in each slice = {CellsMaxInSlice}");
            }

            public void CutSlice(SlicePizza slice)
            {
                var tomatoCountSlice = 0;
                var mshromCountSlice = 0;

                var strtngX = Math.Min(slice.StartPoint.X, slice.EndPoint.X);
                var endingX = Math.Max(slice.StartPoint.X, slice.EndPoint.X);
                for (var row = strtngX; row <= endingX; row++)
                {
                    var strtngY = Math.Min(slice.StartPoint.Y, slice.EndPoint.Y);
                    var endingY = Math.Max(slice.StartPoint.Y, slice.EndPoint.Y);
                    for (var column = strtngY; column <= endingY; column++)
                    {
                        var cellContent = Content[row, column];
                        if (cellContent == Tomato) tomatoCountSlice++;
                        else if (cellContent == Mushroom) mshromCountSlice++;
                        else
                        {
                            RedWriteLine("You want me to cut an empty cell! SHAME ON YOU!");
                            Console.ReadLine();
                        }

                        Content[row, column] = Empty;
                    }
                }

                if (tomatoCountSlice < IngredientsMinInSlice ||
                    mshromCountSlice < IngredientsMinInSlice)
                {
                    RedWriteLine("This slice does not satisfy the rules!");
                    Console.ReadLine();
                }
            }

            public Pizza Clone()
            {
                return new Pizza
                {
                    RowsPizzaCount = RowsPizzaCount,
                    ColumnsPizzaCount = ColumnsPizzaCount,
                    IngredientsMinInSlice = IngredientsMinInSlice,
                    CellsMaxInSlice = CellsMaxInSlice,
                    CellsMinInSlice = CellsMinInSlice,
                    Content = (char[,]) Content.Clone(),
                    Size = Size
                };
            }
        }
    }
}