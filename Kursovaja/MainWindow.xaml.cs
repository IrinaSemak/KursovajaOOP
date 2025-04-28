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
using Library;

namespace Kursovaja
{
    public partial class MainWindow : Window
    {
        private readonly List<WildfireRecord> records = new List<WildfireRecord>();
        private readonly List<ClusterInfo> clusterInfos = new List<ClusterInfo>();
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
            canvasCustom.SizeChanged += CanvasCustom_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"Размеры холста при загрузке: Width={canvasCustom.ActualWidth}, Height={canvasCustom.ActualHeight}");
        }

        private void CanvasCustom_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (canvasCustom.ActualWidth > 0 && canvasCustom.ActualHeight > 0 && records.Count > 0)
            {
                Console.WriteLine($"Размеры холста изменились: Width={canvasCustom.ActualWidth}, Height={canvasCustom.ActualHeight}");
                DisplayRecords();
            }
        }

        private void DataGridResultsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Пустая реализация, если не требуется
        }

        private async void BtnLoadDataClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Выберите CSV-файл с данными"
            };

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                records.Clear();
                canvasCustom.Children.Clear();
                dataGridResults.ItemsSource = null;

                await Task.Run(() =>
                {
                    foreach (var record in CsvLoader.LoadCsvStream(openFileDialog.FileName))
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

                DisplayRecords();
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

            const double MIN_RANGE = 1.0;
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

        private void DisplayRecords(List<WildfireRecord> cluster = null, int clusterId = 0)
        {
            if (canvasCustom.ActualWidth == 0 || canvasCustom.ActualHeight == 0)
            {
                Console.WriteLine("Холст еще не готов для отображения. Размеры: " +
                                  $"Width={canvasCustom.ActualWidth}, Height={canvasCustom.ActualHeight}");
                return;
            }

            var shapes = new List<UIElement>(cluster?.Count ?? records.Count);
            Console.WriteLine($"Отображение: clusterId={clusterId}, точек={(cluster?.Count ?? records.Count)}");
            if (cluster == null)
            {
                foreach (var record in records)
                {
                    double x = ConvertXToCanvasX(double.Parse(record.X, CultureInfo.InvariantCulture));
                    double y = ConvertYToCanvasY(double.Parse(record.Y, CultureInfo.InvariantCulture));
                    Console.WriteLine($"Точка: X={record.X}, Y={record.Y}, Canvas X={x}, Canvas Y={y}");
                    var shape = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Red };
                    Canvas.SetLeft(shape, x);
                    Canvas.SetTop(shape, y);
                    shape.ToolTip = $"{record.StreetNumber} {record.StreetName} {record.StreetType}\nLat: {record.Latitude}\nLon: {record.Longitude}";
                    shapes.Add(shape);
                }
            }
            else
            {
                Brush color = clusterColors[clusterId % clusterColors.Length];
                var clusterInfo = clusterInfos.Find(ci => ci.ClusterId == clusterId) ?? new ClusterInfo
                {
                    ClusterId = clusterId,
                    PointCount = cluster.Count,
                    ClusterColor = color
                };
                if (!clusterInfos.Contains(clusterInfo)) clusterInfos.Add(clusterInfo);
                else clusterInfo.PointCount = cluster.Count;

                Console.WriteLine($"Кластер {clusterId}: {cluster.Count} точек");
                foreach (var record in cluster)
                {
                    double x = ConvertXToCanvasX(double.Parse(record.X, CultureInfo.InvariantCulture));
                    double y = ConvertYToCanvasY(double.Parse(record.Y, CultureInfo.InvariantCulture));
                    Console.WriteLine($"Кластер {clusterId}: X={record.X}, Y={record.Y}, Canvas X={x}, Canvas Y={y}");
                    var shape = new Ellipse { Width = 5, Height = 5, Fill = color };
                    Canvas.SetLeft(shape, x);
                    Canvas.SetTop(shape, y);
                    shape.ToolTip = $"{record.StreetNumber} {record.StreetName} {record.StreetType}\nLat: {record.Latitude}\nLon: {record.Longitude}";
                    shapes.Add(shape);
                }
                dataGridResults.ItemsSource = null;
                dataGridResults.ItemsSource = clusterInfos;
            }

            foreach (var shape in shapes)
            {
                Console.WriteLine($"Добавление фигуры на холст: Left={Canvas.GetLeft(shape)}, Top={Canvas.GetTop(shape)}");
                canvasCustom.Children.Add(shape);
            }
        }

        private void DisplayNoise(List<WildfireRecord> noiseRecords)
        {
            var shapes = new List<UIElement>(noiseRecords.Count);
            Console.WriteLine($"Отображение шума: {noiseRecords.Count} точек");
            foreach (var record in noiseRecords)
            {
                double x = ConvertXToCanvasX(double.Parse(record.X, CultureInfo.InvariantCulture));
                double y = ConvertYToCanvasY(double.Parse(record.Y, CultureInfo.InvariantCulture));
                var shape = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Gray };
                Canvas.SetLeft(shape, x);
                Canvas.SetTop(shape, y);
                shape.ToolTip = $"Шум\nLat: {record.Latitude}\nLon: {record.Longitude}";
                shapes.Add(shape);
            }

            foreach (var shape in shapes)
            {
                canvasCustom.Children.Add(shape);
            }
        }

        private async void BtnClusterCustomClick(object sender, RoutedEventArgs e)
        {
            if (records.Count == 0)
            {
                MessageBox.Show("Нет данных для кластеризации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtEpsilon.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double eps) || eps <= 0 ||
                !int.TryParse(txtMinPoints.Text, out int minPts) || minPts < 1)
            {
                MessageBox.Show("Некорректные параметры Epsilon или Min Points.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                canvasCustom.Children.Clear();
                clusterInfos.Clear();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var dbscan = new DbscanCustom(eps, minPts); // Удален третий аргумент
                dbscan.ClusterFound += (cluster, clusterId) => Dispatcher.Invoke(() => DisplayRecords(cluster, clusterId));
                await Task.Run(() => dbscan.Cluster(records));
                stopwatch.Stop();
                Console.WriteLine($"DBSCAN выполнено за: {stopwatch.ElapsedMilliseconds} мс");

                // Отображение шума
                var noiseRecords = records.Where((r, i) => dbscan.Labels[i] == -1).ToList();
                await Dispatcher.InvokeAsync(() => DisplayNoise(noiseRecords));

                MessageBox.Show($"Кластеризация завершена. Найдено {clusterInfos.Count} кластеров, {noiseRecords.Count} точек шума.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка кластеризации: {ex.Message}\nInner: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnClusterHDBSCANClick(object sender, RoutedEventArgs e)
        {
            if (records.Count == 0)
            {
                MessageBox.Show("Нет данных для кластеризации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtMinPoints.Text, out int minPts) || minPts < 1)
            {
                MessageBox.Show("Некорректное значение Min Points.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                canvasCustom.Children.Clear();
                clusterInfos.Clear();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var hdbscan = new HdbscanCustom(minPts, 5); // minClusterSize = 5
                hdbscan.ClusterFound += (cluster, clusterId) => Dispatcher.Invoke(() => DisplayRecords(cluster, clusterId));
                await hdbscan.Cluster(records);
                stopwatch.Stop();
                Console.WriteLine($"HDBSCAN выполнено за: {stopwatch.ElapsedMilliseconds} мс");

                // Отображение шума
                var noiseRecords = records.Where((r, i) => hdbscan.Labels[i] == 0).ToList();
                await Dispatcher.InvokeAsync(() => DisplayNoise(noiseRecords));

                MessageBox.Show($"Кластеризация завершена. Найдено {clusterInfos.Count} кластеров, {noiseRecords.Count} точек шума.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка кластеризации: {ex.Message}\nInner: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double ConvertXToCanvasX(double x)
        {
            double range = maxX - minX;
            if (range == 0) return canvasCustom.ActualWidth / 2;
            return (x - minX) / range * (canvasCustom.ActualWidth - 10) + 5;
        }

        private double ConvertYToCanvasY(double y)
        {
            double range = maxY - minY;
            if (range == 0) return canvasCustom.ActualHeight / 2;
            return (y - minY) / range * (canvasCustom.ActualHeight - 10) + 5;
        }
    }
}