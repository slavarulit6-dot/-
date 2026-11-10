using System;
using System.Collections.Generic;
using System.Threading;

namespace GoldenArmorTetris
{
    // --- КЛАСС ФИГУРЫ (Шаг 2) ---
    public class Tetromino
    {
        public int[,] Shape { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }
        public char Symbol { get; set; }

        // Матрицы всех 7 стандартных фигур
        private static readonly List<int[,]> Shapes = new List<int[,]>
        {
            new int[,] { {1,1,1,1} },           // I
            new int[,] { {1,0,0},{1,1,1} },     // J
            new int[,] { {0,0,1},{1,1,1} },     // L
            new int[,] { {1,1},{1,1} },         // O
            new int[,] { {0,1,1},{1,1,0} },     // S
            new int[,] { {0,1,0},{1,1,1} },     // T
            new int[,] { {1,1,0},{0,1,1} }      // Z
        };

        private static readonly char[] Symbols = { 'I', 'J', 'L', 'O', 'S', 'T', 'Z' };
        private Random _rand = new Random();
        private int _currentType;

        public Tetromino()
        {
            Spawn();
        }

        public void Spawn()
        {
            _currentType = _rand.Next(Shapes.Count);
            Shape = Shapes[_currentType];
            Symbol = Symbols[_currentType];
            // Появляемся по центру сверху
            X = Board.Width / 2 - Shape.GetLength(1) / 2;
            Y = 0;
        }

        // Вращение матрицы на 90 градусов по часовой стрелке
        public void Rotate()
        {
            int rows = Shape.GetLength(0);
            int cols = Shape.GetLength(1);
            int[,] newShape = new int[cols, rows];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    newShape[j, rows - 1 - i] = Shape[i, j];

            Shape = newShape;
        }
    }

    // --- КЛАСС ИГРОВОГО ПОЛЯ (Шаги 3, 4, 5, 7) ---
    public class Board
    {
        public const int Width = 10;
        public const int Height = 20;
        public char[,] Grid { get; private set; }

        public Board()
        {
            Grid = new char[Height, Width];
            Clear();
        }

        public void Clear()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Grid[y, x] = ' ';
        }

        // Проверка: можно ли поставить фигуру в текущие координаты
        public bool IsValid(Tetromino t)
        {
            for (int y = 0; y < t.Shape.GetLength(0); y++)
            {
                for (int x = 0; x < t.Shape.GetLength(1); x++)
                {
                    if (t.Shape[y, x] == 1)
                    {
                        int boardX = t.X + x;
                        int boardY = t.Y + y;

                        // Выход за границы или столкновение с занятой клеткой
                        if (boardX < 0 || boardX >= Width ||
                            boardY >= Height ||
                            (boardY >= 0 && Grid[boardY, boardX] != ' '))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        // Фиксация фигуры на поле
        public void Lock(Tetromino t)
        {
            for (int y = 0; y < t.Shape.GetLength(0); y++)
                for (int x = 0; x < t.Shape.GetLength(1); x++)
                    if (t.Shape[y, x] == 1 && t.Y + y >= 0)
                        Grid[t.Y + y, t.X + x] = t.Symbol;
        }

        // Удаление заполненных линий
        public int ClearLines()
        {
            int linesCleared = 0;
            for (int y = Height - 1; y >= 0; y--)
            {
                bool isFull = true;
                for (int x = 0; x < Width; x++)
                    if (Grid[y, x] == ' ') { isFull = false; break; }

                if (isFull)
                {
                    // Сдвигаем все линии выше вниз
                    for (int ky = y; ky > 0; ky--)
                        for (int kx = 0; kx < Width; kx++)
                            Grid[ky, kx] = Grid[ky - 1, kx];

                    // Очищаем самую верхнюю линию
                    for (int kx = 0; kx < Width; kx++) Grid[0, kx] = ' ';

                    linesCleared++;
                    y++; // Проверяем эту же строку снова, т.к. туда сдвинулась новая
                }
            }
            return linesCleared;
        }

        // Отрисовка поля (Шаг 5)
        public void Draw(Tetromino currentPiece)
        {
            Console.Clear();

            // Рисуем рамку и поле
            for (int y = 0; y < Height; y++)
            {
                Console.Write("|");
                for (int x = 0; x < Width; x++)
                {
                    bool isPiece = false;
                    // Накладываем текущую падающую фигуру поверх сетки
                    if (currentPiece != null)
                    {
                        int py = y - currentPiece.Y;
                        int px = x - currentPiece.X;
                        if (py >= 0 && py < currentPiece.Shape.GetLength(0) &&
                            px >= 0 && px < currentPiece.Shape.GetLength(1) &&
                            currentPiece.Shape[py, px] == 1)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(currentPiece.Symbol);
                            Console.ResetColor();
                            isPiece = true;
                        }
                    }

                    if (!isPiece)
                    {
                        char cell = Grid[y, x];
                        if (cell != ' ') Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(cell);
                        Console.ResetColor();
                    }
                }
                Console.WriteLine("|");
            }

            // Нижняя граница
            Console.WriteLine("+" + new string('-', Width * 2) + "+");
            Console.WriteLine("Управление: ← → двигать, ↑ вращать, ↓ ускорить, Q выход");
        }
    }

    // --- ОСНОВНОЙ ЦИКЛ ИГРЫ (Шаги 6, 7) ---
    class Program
    {
        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            bool playAgain = true;

            while (playAgain)
            {
                var board = new Board();
                var piece = new Tetromino();
                int score = 0;
                int dropCounter = 0;
                int dropInterval = 20; // Скорость падения (меньше = быстрее)
                bool gameOver = false;

                Console.Title = "Тетрис - Золотая Кираса";

                while (!gameOver)
                {
                    board.Draw(piece);

                    // Обработка ввода без блокировки потока
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        Tetromino temp = new Tetromino(); // Временная копия для проверки

                        switch (key)
                        {
                            case ConsoleKey.LeftArrow:
                                piece.X--;
                                if (!board.IsValid(piece)) piece.X++;
                                break;
                            case ConsoleKey.RightArrow:
                                piece.X++;
                                if (!board.IsValid(piece)) piece.X--;
                                break;
                            case ConsoleKey.DownArrow:
                                piece.Y++;
                                if (!board.IsValid(piece)) piece.Y--;
                                break;
                            case ConsoleKey.UpArrow:
                                piece.Rotate();
                                if (!board.IsValid(piece))
                                {
                                    // Пробуем откатить вращение (wall kick упрощенный)
                                    piece.Rotate(); piece.Rotate(); piece.Rotate();
                                }
                                break;
                            case ConsoleKey.Q:
                                gameOver = true;
                                break;
                        }
                    }

                    // Гравитация
                    dropCounter++;
                    if (dropCounter >= dropInterval)
                    {
                        piece.Y++;
                        if (!board.IsValid(piece))
                        {
                            piece.Y--; // Возвращаем назад
                            board.Lock(piece);
                            int cleared = board.ClearLines();
                            score += cleared * 100;

                            // Проверка конца игры (Шаг 7)
                            if (piece.Y <= 0)
                            {
                                gameOver = true;
                                Console.Clear();
                                Console.WriteLine($"ИГРА ОКОНЧЕНА! Счет: {score}");
                                Console.WriteLine("Нажмите R для рестарта или любую другую клавишу для выхода...");
                                var restartKey = Console.ReadKey(true).Key;
                                playAgain = (restartKey == ConsoleKey.R);
                                break;
                            }

                            piece.Spawn();
                            // Если новая фигура сразу невалидна - тоже конец игры
                            if (!board.IsValid(piece)) gameOver = true;
                        }
                        dropCounter = 0;
                    }

                    Thread.Sleep(30); // Небольшая задержка для стабильности FPS
                }
            }
        }
    }
}