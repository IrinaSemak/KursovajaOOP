using System;
using System.Collections.Generic;
using System.Linq;
using HdbscanSharp.Distance;

namespace Library
{
    public class HaversineDistance : IDistanceCalculator<double[]>
    {
        public double ComputeDistance(int indexOne, int indexTwo, double[] attributesOne, double[] attributesTwo)
        {
            const double R = 6371;
            double lat1 = ToRadians(attributesOne[0]);
            double lon1 = ToRadians(attributesOne[1]);
            double lat2 = ToRadians(attributesTwo[0]);
            double lon2 = ToRadians(attributesTwo[1]);

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }

    public class HdbscanCustom
    {
        private readonly double epsilon;
        private readonly int minPoints;
        private readonly int minClusterSize;
        private readonly List<WildfireRecord> records;
        private int[] labels;
        private readonly IDistanceCalculator<double[]> distanceCalculator;

        public event Action<List<WildfireRecord>, int> ClusterFound;
        public int[] Labels => labels;

        public HdbscanCustom(double epsilon, int minPoints, int minClusterSize)
        {
            this.epsilon = epsilon;
            this.minPoints = minPoints;
            this.minClusterSize = minClusterSize;
            records = new List<WildfireRecord>();
            labels = Array.Empty<int>();
            distanceCalculator = new HaversineDistance();
        }

        public void Cluster(IEnumerable<WildfireRecord> inputRecords)
        {
            try
            {
                records.Clear();
                records.AddRange(inputRecords);
                if (records.Count == 0)
                {
                    labels = new int[0];
                    Console.WriteLine("HDBSCAN: Нет валидных записей после фильтрации");
                    return;
                }

                Console.WriteLine($"HDBSCAN: После фильтрации: {records.Count} записей");

                labels = new int[records.Count];

                int clusterLabel = 0;
                for (int i = 0; i < records.Count; i++)
                {
                    if (labels[i] != 0) continue;

                    var neighbors = GetNeighbors(i);
                    if (neighbors.Count < minPoints)
                    {
                        labels[i] = -1;
                        Console.WriteLine($"HDBSCAN Шум: Lat={records[i].Latitude}, Lon={records[i].Longitude}, Соседей={neighbors.Count}");
                        continue;
                    }

                    clusterLabel++;
                    labels[i] = clusterLabel;
                    var cluster = new List<WildfireRecord>(neighbors.Count) { records[i] };
                    ExpandCluster(i, neighbors, clusterLabel, cluster);

                    if (cluster.Count >= minClusterSize)
                    {
                        ClusterFound?.Invoke(cluster, clusterLabel);
                        Console.WriteLine($"HDBSCAN Кластер {clusterLabel}: {cluster.Count} точек");
                    }
                    else
                    {
                        foreach (var point in cluster)
                        {
                            int idx = records.IndexOf(point);
                            labels[idx] = -1;
                        }
                        clusterLabel--;
                    }
                }

                // Проверка, все ли точки получили метку
                int unassignedPoints = labels.Count(l => l == 0);
                if (unassignedPoints > 0)
                {
                    Console.WriteLine($"Ошибка: {unassignedPoints} точек не получили метку (кластер или шум)");
                }

                Console.WriteLine($"HDBSCAN: Найдено {clusterLabel} кластеров, {labels.Count(l => l == -1)} точек шума");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка кластеризации: {ex.Message}", ex);
            }
        }

        private List<int> GetNeighbors(int pointIndex)
        {
            var record = records[pointIndex];
            var neighbors = new List<int>(records.Count);

            for (int candidateIndex = 0; candidateIndex < records.Count; candidateIndex++)
            {
                if (candidateIndex == pointIndex) continue;
                double distance = distanceCalculator.ComputeDistance(
                    pointIndex,
                    candidateIndex,
                    new[] { record.Latitude, record.Longitude },
                    new[] { records[candidateIndex].Latitude, records[candidateIndex].Longitude });
                if (distance <= epsilon)
                {
                    neighbors.Add(candidateIndex);
                    if (neighbors.Count >= 1000) break;
                }
            }
            return neighbors;
        }

        private void ExpandCluster(int _pointIndex, List<int> initialNeighbors, int clusterLabel, List<WildfireRecord> cluster)
        {
            var queue = new Queue<int>(initialNeighbors);
            while (queue.Count > 0)
            {
                int neighborIndex = queue.Dequeue();
                if (labels[neighborIndex] != 0 && labels[neighborIndex] != -1) continue;

                labels[neighborIndex] = clusterLabel;
                cluster.Add(records[neighborIndex]);

                var furtherNeighbors = GetNeighbors(neighborIndex);
                if (furtherNeighbors.Count >= minPoints)
                {
                    foreach (var furtherNeighbor in furtherNeighbors)
                    {
                        if (labels[furtherNeighbor] == 0 || labels[furtherNeighbor] == -1)
                            queue.Enqueue(furtherNeighbor);
                    }
                }
            }
        }
    }
}