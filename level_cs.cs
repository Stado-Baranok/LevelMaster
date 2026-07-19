// level_cs.cs — строительный уровень с калибровкой на C# (WPF)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;
using System.Text.Json;

namespace LevelWPF
{
    public partial class MainWindow : Window
    {
        private double angle = 0.0;
        private double offset = 0.0;
        private double sensitivity = 0.5;
        private bool calibrated = false;
        private bool simulating = false;
        private bool verticalMode = false;
        private bool beepEnabled = true;
        private DispatcherTimer timer;
        private Random rand = new Random();
        private string configFile = "level_config.json";

        private Canvas levelCanvas;
        private Ellipse bubble;
        private Label infoLabel, statusLabel;
        private Slider sensSlider;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            CreateUI();
            UpdateDisplay();
        }

        private void CreateUI()
        {
            Title = "📏 LevelMaster — C#";
            Width = 600;
            Height = 600;
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Панель управления
            var ctrlPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            var readBtn = new Button { Content = "Прочитать" };
            var calibrateBtn = new Button { Content = "Калибровка" };
            var resetBtn = new Button { Content = "Сброс" };
            var simBtn = new Button { Content = "Симуляция" };
            var vertBtn = new Button { Content = "Вертикаль" };
            var beepBtn = new Button { Content = "Звук" };
            ctrlPanel.Children.Add(readBtn);
            ctrlPanel.Children.Add(calibrateBtn);
            ctrlPanel.Children.Add(resetBtn);
            ctrlPanel.Children.Add(simBtn);
            ctrlPanel.Children.Add(vertBtn);
            ctrlPanel.Children.Add(beepBtn);
            Grid.SetRow(ctrlPanel, 0);
            grid.Children.Add(ctrlPanel);

            // Canvas для уровня
            levelCanvas = new Canvas { Background = Brushes.LightGray };
            Grid.SetRow(levelCanvas, 1);
            grid.Children.Add(levelCanvas);
            levelCanvas.SizeChanged += (s, e) => DrawLevel();

            // Информация
            infoLabel = new Label { Content = "Наклон: 0.0°", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 16 };
            Grid.SetRow(infoLabel, 2);
            grid.Children.Add(infoLabel);

            statusLabel = new Label { Content = "Готов", HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetRow(statusLabel, 3);
            grid.Children.Add(statusLabel);

            // Чувствительность
            var sensPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5) };
            sensPanel.Children.Add(new Label { Content = "Чувствительность:" });
            sensSlider = new Slider { Minimum = 1, Maximum = 50, Value = (int)(sensitivity*10), Width = 150 };
            sensSlider.ValueChanged += (s, e) => { sensitivity = sensSlider.Value / 10.0; statusLabel.Content = $"Чувствительность: {sensitivity:F1}°"; };
            sensPanel.Children.Add(sensSlider);
            Grid.SetRow(sensPanel, 4);
            grid.Children.Add(sensPanel);

            Content = grid;

            readBtn.Click += (s, e) => ReadAngle();
            calibrateBtn.Click += (s, e) => Calibrate();
            resetBtn.Click += (s, e) => Reset();
            simBtn.Click += (s, e) => ToggleSimulation();
            vertBtn.Click += (s, e) => ToggleVertical();
            beepBtn.Click += (s, e) => ToggleBeep();

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer.Tick += (s, e) => Simulate();
            DrawLevel();
        }

        private void DrawLevel()
        {
            if (levelCanvas == null) return;
            levelCanvas.Children.Clear();
            double w = levelCanvas.ActualWidth;
            double h = levelCanvas.ActualHeight;
            if (w < 10 || h < 10) return;
            double cx = w/2, cy = h/2;
            // Рамка
            var rect = new System.Windows.Shapes.Rectangle();
            rect.Width = w-100;
            rect.Height = h-100;
            Canvas.SetLeft(rect, 50);
            Canvas.SetTop(rect, 50);
            rect.Stroke = Brushes.Black;
            rect.StrokeThickness = 3;
            levelCanvas.Children.Add(rect);
            // Шкала
            for (int deg = -30; deg <= 30; deg += 5) {
                double x = cx + deg * 5;
                var line = new System.Windows.Shapes.Line();
                line.X1 = x; line.Y1 = 80; line.X2 = x; line.Y2 = h-80;
                line.Stroke = Brushes.Black;
                line.StrokeThickness = 1;
                levelCanvas.Children.Add(line);
                if (deg % 10 == 0) {
                    var tb = new TextBlock();
                    tb.Text = deg.ToString();
                    tb.FontSize = 10;
                    Canvas.SetLeft(tb, x-10);
                    Canvas.SetTop(tb, 70);
                    levelCanvas.Children.Add(tb);
                }
            }
            // Центр
            var centerLine = new System.Windows.Shapes.Line();
            centerLine.X1 = cx; centerLine.Y1 = 60; centerLine.X2 = cx; centerLine.Y2 = h-60;
            centerLine.Stroke = Brushes.Red;
            centerLine.StrokeThickness = 2;
            centerLine.StrokeDashArray = new DoubleCollection(new double[]{5,5});
            levelCanvas.Children.Add(centerLine);
            // Пузырёк
            bubble = new Ellipse();
            bubble.Width = 40; bubble.Height = 40;
            bubble.Fill = Brushes.Blue;
            Canvas.SetLeft(bubble, cx-20);
            Canvas.SetTop(bubble, cy-20);
            levelCanvas.Children.Add(bubble);
            UpdateBubble();
        }

        private void UpdateBubble()
        {
            if (bubble == null) return;
            double w = levelCanvas.ActualWidth;
            double h = levelCanvas.ActualHeight;
            double cx = w/2, cy = h/2;
            double displayAngle = (angle + offset) % 360;
            if (displayAngle > 180) displayAngle -= 360;
            if (verticalMode) displayAngle -= 90;
            if (displayAngle > 30) displayAngle = 30;
            if (displayAngle < -30) displayAngle = -30;
            double x = cx + displayAngle * 5;
            Canvas.SetLeft(bubble, x-20);
            Canvas.SetTop(bubble, cy-20);
            // Цвет
            Color color;
            if (Math.Abs(displayAngle) < sensitivity) color = Colors.Green;
            else if (Math.Abs(displayAngle) < sensitivity*3) color = Colors.Orange;
            else color = Colors.Red;
            bubble.Fill = new SolidColorBrush(color);
        }

        private void UpdateDisplay()
        {
            UpdateBubble();
            double displayAngle = (angle + offset) % 360;
            if (displayAngle > 180) displayAngle -= 360;
            double deviation = displayAngle;
            if (verticalMode) deviation -= 90;
            if (deviation > 180) deviation -= 360;
            if (verticalMode)
                infoLabel.Content = $"Отклонение от вертикали: {deviation:F1}°";
            else
                infoLabel.Content = $"Наклон: {deviation:F1}°";
            if (beepEnabled && Math.Abs(deviation) < sensitivity)
                System.Media.SystemSounds.Beep.Play();
        }

        private void ReadAngle()
        {
            if (!simulating)
            {
                angle += rand.NextDouble() * 2 - 1;
                if (angle > 360) angle -= 360;
                if (angle < 0) angle += 360;
            }
            UpdateDisplay();
        }

        private void Calibrate()
        {
            statusLabel.Content = "Калибровка... (имитация)";
            double sum = 0;
            for (int i = 0; i < 10; i++) sum += rand.NextDouble() * 4 - 2;
            double avg = sum / 10;
            offset = -avg;
            calibrated = true;
            statusLabel.Content = $"Калибровка завершена. Смещение: {offset:F2}°";
            SaveConfig();
            UpdateDisplay();
        }

        private void Reset()
        {
            offset = 0.0;
            calibrated = false;
            angle = 0.0;
            statusLabel.Content = "Сброшено";
            UpdateDisplay();
        }

        private void ToggleSimulation()
        {
            simulating = !simulating;
            if (simulating) { timer.Start(); statusLabel.Content = "Симуляция включена"; }
            else { timer.Stop(); statusLabel.Content = "Симуляция выключена"; }
        }

        private void Simulate()
        {
            angle = (angle + 0.2) % 360;
            UpdateDisplay();
        }

        private void ToggleVertical()
        {
            verticalMode = !verticalMode;
            statusLabel.Content = "Режим: " + (verticalMode ? "вертикаль" : "горизонталь");
            UpdateDisplay();
        }

        private void ToggleBeep()
        {
            beepEnabled = !beepEnabled;
            statusLabel.Content = "Звук " + (beepEnabled ? "включён" : "выключен");
        }

        private void LoadConfig()
        {
            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (data != null)
                {
                    offset = Convert.ToDouble(data["offset"]);
                    calibrated = Convert.ToBoolean(data["calibrated"]);
                    sensitivity = Convert.ToDouble(data["sensitivity"]);
                    verticalMode = Convert.ToBoolean(data["verticalMode"]);
                    beepEnabled = Convert.ToBoolean(data["beepEnabled"]);
                }
            }
        }

        private void SaveConfig()
        {
            var data = new { offset, calibrated, sensitivity, verticalMode, beepEnabled };
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(configFile, json);
        }

        [STAThread]
        static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }
}
