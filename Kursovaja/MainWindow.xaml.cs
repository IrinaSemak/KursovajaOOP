using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Globalization;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.IO;
using Library;

namespace Kursovaja
{
    public partial class MainWindow : Window
    {
        private readonly List<WildfireRecord> records = new List<WildfireRecord>();
        private readonly List<ClusterInfo> clusterInfosDBSCAN = new List<ClusterInfo>();
        private readonly List<ClusterInfo> clusterInfosHDBSCAN = new List<ClusterInfo>();
        private double minLat, maxLat, minLon, maxLon;
        private double minX, maxX, minY, maxY;
        private readonly Brush[] clusterColors = {
            Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange, Brushes.Purple,
            Brushes.Cyan, Brushes.Magenta, Brushes.Yellow, Brushes.Brown, Brushes.Pink
        };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            canvasCustomDBSCAN.SizeChanged += CanvasCustomDBSCAN_SizeChanged;
            canvasCustomHDBSCAN.SizeChanged += CanvasCustomHDBSCAN_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"DBSCAN: Размеры холста при загрузке: Width={canvasCustomDBSCAN.ActualWidth}, Height={canvasCustomDBSCAN.ActualHeight}");
            Console.WriteLine($"HDBSCAN: Размеры холста при загрузке: Width={canvasCustomHDBSCAN.ActualWidth}, Height={canvasCustomHDBSCAN.ActualHeight}");
        }

        private void CanvasCustomDBSCAN_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (canvasCustomDBSCAN.ActualWidth > 0 && canvasCustomDBSCAN.ActualHeight > 0 && records.Count > 0)
            {
                Console.WriteLine($"DBSCAN: Размеры холста изменились: Width={canvasCustomDBSCAN.ActualWidth}, Height={canvasCustomDBSCAN.ActualHeight}");
                DisplayRecordsDBSCAN();
            }
        }

        private void CanvasCustomHDBSCAN_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (canvasCustomHDBSCAN.ActualWidth > 0 && canvasCustomHDBSCAN.ActualHeight > 0 && records.Count > 0)
            {
                Console.WriteLine($"HDBSCAN: Размеры холста изменились: Width={canvasCustomHDBSCAN.ActualWidth}, Height={canvasCustomHDBSCAN.ActualHeight}");
                DisplayRecordsHDBSCAN();
            }
        }

        private void DataGridResultsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Пустая реализация
        }

        private async void BtnLoadDataClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите CSV-файл с данными"
            };

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                records.Clear();
                canvasCustomDBSCAN.Children.Clear();
                canvasCustomHDBSCAN.Children.Clear();
                dataGridResultsDBSCAN.ItemsSource = null;
                dataGridResultsHDBSCAN.ItemsSource = null;

                // Используем интерфейс для чтения файла
                IFileReader fileReader = new CsvFileReader();
                await Task.Run(() =>
                {
                    foreach (var record in fileReader.LoadRecords(openFileDialog.FileName))
                    {
                        if (!record.IsLatitudeMissing && !record.IsLongitudeMissing &&
                            !string.IsNullOrEmpty(record.X) && !string.IsNullOrEmpty(record.Y) &&
                            double.TryParse(record.X, NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                            double.TryParse(record.Y, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        {
                            records.Add(record);
                        }
                        else
                        {
                            Console.WriteLine($"Пропущена запись: Lat={record.Latitude}, Lon={record.Longitude}, X={record.X}, Y={record.Y}");
                        }
                    }
                });

                if (records.Count == 0)
                {
                    MessageBox.Show("Нет записей с корректными координатами.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CalculateCoordinateRange();
                Console.WriteLine($"Диапазон координат: minX={minX}, maxX={maxX}, minY={minY}, maxY={maxY}");
                Console.WriteLine($"Прочитано {records.Count} точек");

                DisplayRecordsDBSCAN();
                DisplayRecordsHDBSCAN();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\nInner: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateCoordinateRange()
        {
            if (records.Count == 0)
            {
                minLat = maxLat = minLon = maxLon = 0;
                minX = maxX = minY = maxY = 0;
                return;
            }

            minLat = maxLat = records[0].Latitude;
            minLon = maxLon = records[0].Longitude;
            minX = maxX = double.Parse(records[0].X, CultureInfo.InvariantCulture);
            minY = maxY = double.Parse(records[0].Y, CultureInfo.InvariantCulture);

            foreach (var record in records)
            {
                minLat = Math.Min(minLat, record.Latitude);
                maxLat = Math.Max(maxLat, record.Latitude);
                minLon = Math.Min(minLon, record.Longitude);
                maxLon = Math.Max(maxLon, record.Longitude);

                double x = double.Parse(record.X, CultureInfo.InvariantCulture);
                double y = double.Parse(record.Y, CultureInfo.InvariantCulture);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            const double MIN_RANGE = 0.0001;
            if (maxLat - minLat < MIN_RANGE)
            {
                double midLat = (minLat + maxLat) / 2;
                minLat = midLat - MIN_RANGE / 2;
                maxLat = midLat + MIN_RANGE / 2;
                Console.WriteLine($"Широта расширена искусственно: minLat={minLat}, maxLat={maxLat}");
            }
            if (maxLon - minLon < MIN_RANGE)
            {
                double midLon = (minLon + maxLon) / 2;
                minLon = midLon - MIN_RANGE / 2;
                maxLon = midLon + MIN_RANGE / 2;
                Console.WriteLine($"Долгота расширена искусственно: minLon={minLon}, maxLon={maxLon}");
            }
            if (maxX - minX < MIN_RANGE)
            {
                double midX = (minX + maxX) / 2;
                minX = midX - MIN_RANGE / 2;
                maxX = midX + MIN_RANGE / 2;
                Console.WriteLine($"X расширен искусственно: minX={minX}, maxX={maxX}");
            }
            if (maxY - minY < MIN_RANGE)
            {
                double midY = (minY + maxY) / 2;
                minY = midY - MIN_RANGE / 2;
                maxY = midY + MIN_RANGE / 2;
                Console.WriteLine($"Y расширен искусственно: minY={minY}, maxY={maxY}");
            }
        }

        private void DisplayRecordsDBSCAN(Dictionary<int, List<WildfireRecord>> clusters = null)
        {
            if (canvasCustomDBSCAN.ActualWidth == 0 || canvasCustomDBSCAN.ActualHeight == 0)
            {
                Console.WriteLine("DBSCAN: Холст еще не готов для отображения. Размеры: " +
                    $"Width={canvasCustomDBSCAN.ActualWidth}, Height={canvasCustomDBSCAN.ActualHeight}");
                return;
            }

            canvasCustomDBSCAN.Children.Clear();
            clusterInfosDBSCAN.Clear();

            if (clusters == null)
            {
                var shapes = new List<UIElement>(records.Count);
                Console.WriteLine($"DBSCAN: Отображение всех точек: {records.Count}");
                foreach (var record in records)
                {
                    double x = ConvertXToCanvasX(double.Parse(record.X, CultureInfo.InvariantCulture));
                    double y = ConvertYToCanvasY(double.Parse(record.Y, CultureInfo.InvariantCulture));
                    if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                    {
                        Console.WriteLine($"DBSCAN: Пропущена точка (некорректные координаты): X={record.X}, Y={record.Y}, Canvas X={x}, Canvas Y={y}");
                        continue;
                    }
                    var shape = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Red };
                    Canvas.SetLeft(shape, x);
                    Canvas.SetTop(shape, y);
                    shape.ToolTip = $"{record.StreetNumber} {record.StreetName} {record.StreetType}\nLat: {record.Latitude}\nLon: {record.Longitude}";
                    shapes.Add(shape);
                }

                foreach (var shape in shapes)
                {
                    double left = Canvas.GetLeft(shape);
                    double top = Canvas.GetTop(shape);
                    if (left < 0 || left > canvasCustomDBSCAN.ActualWidth || top < 0 || top > canvasCustomDBSCAN.ActualHeight)
                        continue;
                    canvasCustomDBSCAN.Children.Add(shape);
                }
            }
            else
            {
                foreach (var kvp in clusters)
                {
                    int clusterId = kvp.Key;
                    var cluster = kvp.Value;
                    Brush color = clusterId == -1 ? Brushes.Gray : clusterColors[clusterId % clusterColors.Length];
                    var shapes = new List<UIElement>(cluster.Count);
                    Console.WriteLine($"DBSCAN: Кластер {clusterId}: {cluster.Count} точек");

                    foreach (var record in cluster)
                    {
                        double x = ConvertXToCanvasX(double.Parse(record.X, CultureInfo.InvariantCulture));
                        double y = ConvertYToCanvasY(double.Parse(record.Y, CultureInfo.InvariantCulture));
                        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                        {
                            Console.WriteLine($"DBSCAN: Пропущена точка в кластере (некорректные координаты): X={record.X}, Y={record.Y}, Canvas X={x}, Canvas Y={y}");
                            continue;
                        }
                        var shape = new Ellipse { Width = 5, Height = 5, Fill = color };
                        Canvas.SetLeft(shape, x);
                        Canvas.SetTop(shape, y);
                        shape.ToolTip = $"{record.StreetNumber} {record.StreetName} {record.StreetType}\nLat: {record.Latitude}\nLon: {record.Longitude}";
                        shapes.Add(shape);
                    }

                    foreach (var shape in shapes)
                    {
                        double left = Canvas.GetLeft(shape);
                        double top = Canvas.GetTop(shape);
                        if (left < 0 || left > canvasCustomDBSCAN.ActualWidth || top < 0 || top > canvasCustomDBSCAN.ActualHeight)
                            continue;
                        canvasCustomDBSCAN.Children.Add(shape);
                    }

                    var clusterInfo = new ClusterInfo
                    {
                        ClusterId = clusterId,
                        PointCount = cluster.Count,
                        ClusterColor = color
                    };
                    clusterInfosDBSCAN.Add(clusterInfo);
                }

                dataGridResultsDBSCAN.ItemsSource = null;
                dataGridResultsDBSCAN.ItemsSource = clusterInfosDBSCAN.OrderBy(ci => ci.ClusterId == -1 ? int.MaxValue : ci.ClusterId).ToList();
            }
        }

        private void DisplayRecordsHDBSCAN(Dictionary<int, List<WildfireRecord>> clusters = null)
        {
            if (canvasCustomHDBSCAN.ActualWidth == 0 || canvasCustomHDBSCAN.ActualHeight == 0)
            {
                Console.WriteLine("HDBSCAN: Холст еще не готов для отображения. Размеры: " +
                    $"Width={canvasCustomHDBSCAN.ActualWidth}, Height={canvasCustomHDBSCAN.ActualHeight}");
                return;
            }

            canvasCustomHDBSCAN.Children.Clear();
            clusterInfosHDBSCAN.Clear();

            if (clusters == null)
            {
                var shapes = new List<UIElement>(records.Count);
                Console.WriteLine($"HDBSCAN: Отображение всех точек: {records.Count}");
                foreach (var record in records)
                {
                    double x = ConvertXToCanvasX(double.Parse(record.X, CultureInfo.InvariantCulture));
                    double y = ConvertYToCanvasY(double.Parse(record.Y, CultureInfo.InvariantCulture));
                    if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                    {
                        Console.WriteLine($"HDBSCAN: Пропущена точка (некорректные координаты): X={record.X}, Y={record.Y}, Canvas X={x}, Canvas Y={y}");
                        continue;
                    }
                    var shape = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Red };
                    Canvas.SetLeft(shape, x);
                    Canvas.SetTop(shape, y);
                    shape.ToolTip = $"{record.StreetNumber} {record.StreetName} {record.StreetType}\nLat: {record.Latitude}\nLon: {record.Longitude}";
                    shapes.Add(shape);
                }

                foreach (var shape in shapes)
                {
                    double left = Canvas.GetLeft(shape);
                    double top = Canvas.GetTop(shape);
                    if (left < 0 || left > canvasCustomHDBSCAN.ActualWidth || top < 0 || top > canvasCustomHDBSCAN.ActualHeight)
                        continue;
                    canvasCustomHDBSCAN.Children.Add(shape);
                }
            }
            else
            {
                foreach (var kvp in clusters)
                {
                    int clusterId = kvp.Key;
                    var cluster = kvp.Value;
                    Brush color = clusterId == -1 ? Brushes.Gray : clusterColors[clusterId % clusterColors.Length];
                    var shapes = new List<UIElement>(cluster.Count);
                    Console.WriteLine($"HDBSCAN: Кластер {clusterId}: {cluster.Count} точек");

                    foreach (var record in cluster)
                    {
                        double x = ConvertXToCanvasX(double.Parse(record.X, CultureInfo.InvariantCulture));
                        double y = ConvertYToCanvasY(double.Parse(record.Y, CultureInfo.InvariantCulture));
                        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                        {
                            Console.WriteLine($"HDBSCAN: Пропущена точка в кластере (некорректные координаты): X={record.X}, Y={record.Y}, Canvas X={x}, Canvas Y={y}");
                            continue;
                        }
                        var shape = new Ellipse { Width = 5, Height = 5, Fill = color };
                        Canvas.SetLeft(shape, x);
                        Canvas.SetTop(shape, y);
                        shape.ToolTip = $"{record.StreetNumber} {record.StreetName} {record.StreetType}\nLat: {record.Latitude}\nLon: {record.Longitude}";
                        shapes.Add(shape);
                    }

                    foreach (var shape in shapes)
                    {
                        double left = Canvas.GetLeft(shape);
                        double top = Canvas.GetTop(shape);
                        if (left < 0 || left > canvasCustomHDBSCAN.ActualWidth || top < 0 || top > canvasCustomHDBSCAN.ActualHeight)
                            continue;
                        canvasCustomHDBSCAN.Children.Add(shape);
                    }

                    var clusterInfo = new ClusterInfo
                    {
                        ClusterId = clusterId,
                        PointCount = cluster.Count,
                        ClusterColor = color
                    };
                    clusterInfosHDBSCAN.Add(clusterInfo);
                }

                dataGridResultsHDBSCAN.ItemsSource = null;
                dataGridResultsHDBSCAN.ItemsSource = clusterInfosHDBSCAN.OrderBy(ci => ci.ClusterId == -1 ? int.MaxValue : ci.ClusterId).ToList();
            }
        }

        private async void BtnClusterCustomClick(object sender, RoutedEventArgs e)
        {
            if (records.Count == 0)
            {
                MessageBox.Show("Нет данных для кластеризации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtEpsilonDBSCAN.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double eps) || eps <= 0 ||
                !int.TryParse(txtMinPointsDBSCAN.Text, out int minPts) || minPts < 1)
            {
                MessageBox.Show("Некорректные параметры Epsilon или Min Points для DBSCAN.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var validRecords = records.ToList();
                Console.WriteLine($"DBSCAN: Всего записей: {records.Count}, после фильтрации: {validRecords.Count}");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                // Используем интерфейс для кластеризации
                IClusteringAlgorithm dbscan = new DbscanAlgorithm(eps, minPts, new HaversineDistanceCalculator());
                Dictionary<int, List<WildfireRecord>> clusters = await Task.Run(() => dbscan.Cluster(validRecords));
                stopwatch.Stop();
                Console.WriteLine($"DBSCAN выполнено за: {stopwatch.ElapsedMilliseconds} мс");

                int clusterCount = clusters.Count(kvp => kvp.Key != -1);
                int noiseCount = clusters.ContainsKey(-1) ? clusters[-1].Count : 0;
                Console.WriteLine($"DBSCAN: {clusterCount} кластеров, {noiseCount} точек шума");

                await Dispatcher.InvokeAsync(() => DisplayRecordsDBSCAN(clusters));
                File.WriteAllLines("dbscan_labels.txt", dbscan.Labels.Select((l, i) => $"{i}: {l}"));

                MessageBox.Show($"DBSCAN завершено. Найдено {clusterCount} кластеров, {noiseCount} точек шума.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка кластеризации DBSCAN: {ex.Message}\nInner: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnClusterHDBSCANClick(object sender, RoutedEventArgs e)
        {
            if (records.Count == 0)
            {
                MessageBox.Show("Нет данных для кластеризации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtEpsilonHDBSCAN.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double eps) || eps <= 0 ||
                !int.TryParse(txtMinPointsHDBSCAN.Text, out int minPts) || minPts < 1)
            {
                MessageBox.Show("Некорректные параметры Epsilon или Min Points для HDBSCAN.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var validRecords = records.ToList();
                Console.WriteLine($"HDBSCAN: Всего записей: {records.Count}, после фильтрации: {validRecords.Count}");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                // Используем интерфейс для кластеризации
                IClusteringAlgorithm hdbscan = new HdbscanAlgorithm(eps, minPts);
                Dictionary<int, List<WildfireRecord>> clusters = await Task.Run(() => hdbscan.Cluster(validRecords));
                stopwatch.Stop();
                Console.WriteLine($"HDBSCAN выполнено за: {stopwatch.ElapsedMilliseconds} мс");

                int clusterCount = clusters.Count(kvp => kvp.Key != -1);
                int noiseCount = clusters.ContainsKey(-1) ? clusters[-1].Count : 0;
                Console.WriteLine($"HDBSCAN: {clusterCount} кластеров, {noiseCount} точек шума");

                await Dispatcher.InvokeAsync(() => DisplayRecordsHDBSCAN(clusters));
                File.WriteAllLines("hdbscan_labels.txt", hdbscan.Labels.Select((l, i) => $"{i}: {l}"));

                MessageBox.Show($"HDBSCAN завершено. Найдено {clusterCount} кластеров, {noiseCount} точек шума.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка кластеризации HDBSCAN: {ex.Message}\nInner: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double ConvertXToCanvasX(double x)
        {
            double range = maxX - minX;
            if (range == 0) return canvasCustomDBSCAN.ActualWidth / 2;
            double canvasX = (x - minX) / range * (canvasCustomDBSCAN.ActualWidth - 10) + 5;
            return Clamp(canvasX, 0, canvasCustomDBSCAN.ActualWidth);
        }

        private double ConvertYToCanvasY(double y)
        {
            double range = maxY - minY;
            if (range == 0) return canvasCustomDBSCAN.ActualHeight / 2;
            double canvasY = (y - minY) / range * (canvasCustomDBSCAN.ActualHeight - 10) + 5;
            return Clamp(canvasY, 0, canvasCustomDBSCAN.ActualHeight);
        }

        private double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}