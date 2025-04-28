using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HdbscanSharp.Hdbscanstar;

namespace Library
{
    public class HdbscanCustom
    {
        private readonly int minPts;
        private readonly int minClusterSize;
        private int[] labels;
        private readonly Dictionary<(int, int), double> distanceCache;

        public event Action<List<WildfireRecord>, int> ClusterFound;

        public HdbscanCustom(int minPts, int minClusterSize = 5)
        {
            this.minPts = minPts;
            this.minClusterSize = minClusterSize;
            labels = Array.Empty<int>();
            distanceCache = new Dictionary<(int, int), double>();
        }

        public async Task Cluster(IEnumerable<WildfireRecord> inputRecords)
        {
            try
            {
                var records = inputRecords.ToList();
                if (records.Count == 0) return;

                labels = new int[records.Count];
                distanceCache.Clear();

                // Подготовка данных
                double[][] data = records
                    .Where(r => !r.IsLatitudeMissing && !r.IsLongitudeMissing)
                    .Select(r => new double[] { r.Longitude, r.Latitude })
                    .ToArray();

                int numPoints = data.Length;
                Console.WriteLine($"Количество точек для HDBSCAN: {numPoints}");
                Console.WriteLine($"Диапазон данных: Lat=[{data.Min(d => d[1])},{data.Max(d => d[1])}], Lon=[{data.Min(d => d[0])},{data.Max(d => d[0])}]");

                // Функция расстояния с кэшированием
                double CalculateDistance(int i, int j)
                {
                    var key = i < j ? (i, j) : (j, i);
                    if (distanceCache.TryGetValue(key, out double distance))
                        return distance;

                    distance = CalculateHaversineDistance(data[i][1], data[i][0], data[j][1], data[j][0]);
                    distanceCache[key] = distance;
                    return distance;
                }
                Func<int, int, double> distanceFunction = CalculateDistance;

                // Выполнение HDBSCAN
                int[] hdbscanLabels = await Task.Run(() =>
                {
                    double[] coreDistances = HdbscanAlgorithm.CalculateCoreDistances(distanceFunction, numPoints, minPts);
                    var mst = HdbscanAlgorithm.ConstructMst(distanceFunction, numPoints, coreDistances, false);
                    var hierarchy = new List<int[]>();
                    var pointNoiseLevels = new double[numPoints];
                    var pointLastClusters = new int[numPoints];
                    var hdbscanClusters = HdbscanAlgorithm.ComputeHierarchyAndClusterTree(
                        mst, minPts, null, hierarchy, pointNoiseLevels, pointLastClusters);
                    HdbscanAlgorithm.PropagateTree(hdbscanClusters);
                    return HdbscanAlgorithm.FindProminentClusters(hdbscanClusters, hierarchy, numPoints);
                });

                // Присваиваем метки
                Array.Copy(hdbscanLabels, labels, numPoints);

                // Формирование кластеров
                var clusters = labels.Select((label, idx) => new { Label = label, Record = records[idx] })
                                    .GroupBy(x => x.Label)
                                    .Where(g => g.Key != 0); // Исключаем шум (метка 0)

                foreach (var cluster in clusters)
                {
                    int clusterId = cluster.Key; // Метки начинаются с 1
                    var clusterRecords = cluster.Select(x => x.Record).ToList();
                    if (clusterRecords.Count >= minClusterSize)
                    {
                        ClusterFound?.Invoke(clusterRecords, clusterId);
                    }
                    else
                    {
                        // Помечаем точки как шум, если кластер слишком мал
                        foreach (var item in cluster)
                        {
                            labels[records.IndexOf(item.Record)] = 0;
                        }
                    }
                }

                Console.WriteLine($"HDBSCAN: {clusters.Count(g => g.Select(x => x.Record).Count() >= minClusterSize)} кластеров, {labels.Count(l => l == 0)} шума");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка кластеризации HDBSCAN: {ex.Message}", ex);
            }
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Радиус Земли в км
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        public int[] Labels => labels;
    }
}