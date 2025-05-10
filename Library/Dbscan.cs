using Library;
using System;
using System.Collections.Generic;

namespace Kursovaja
{
    // Класс для реализации алгоритма DBSCAN
    public class DbscanAlgorithm : BaseClusteringAlgorithm
    {
        private readonly HaversineDistanceCalculator distanceCalculator;

        // Конструктор
        public DbscanAlgorithm(double epsilon, int minPoints, HaversineDistanceCalculator distanceCalculator)
            : base(epsilon, minPoints)
        {
            this.distanceCalculator = distanceCalculator;
        }

        // Реализация метода кластеризации
        public override Dictionary<int, List<WildfireRecord>> Cluster(IEnumerable<WildfireRecord> inputRecords)
        {
            LoadRecords(inputRecords);
            var clustersDict = new Dictionary<int, List<WildfireRecord>>();

            if (records.Count == 0)
                return clustersDict;

            int clusterLabel = 0;

            for (int i = 0; i < records.Count; i++)
            {
                if (labels[i] != 0)
                    continue;

                var neighbors = GetNeighbors(i);
                if (neighbors.Count < minPoints)
                {
                    labels[i] = -1; // Шум
                    continue;
                }

                clusterLabel++;
                labels[i] = clusterLabel;
                var cluster = new List<WildfireRecord> { records[i] };
                ExpandCluster(neighbors, clusterLabel, cluster);

                clustersDict[clusterLabel] = cluster;
            }

            // Добавляем шум
            var noise = new List<WildfireRecord>();
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == -1)
                    noise.Add(records[i]);
            }
            if (noise.Count > 0)
                clustersDict[-1] = noise;

            return clustersDict;
        }

        // Метод для поиска соседей точки
        private List<int> GetNeighbors(int pointIndex)
        {
            var neighbors = new List<int>();
            var point = records[pointIndex];

            for (int i = 0; i < records.Count; i++)
            {
                if (i == pointIndex)
                    continue;

                double distance = distanceCalculator.CalculateDistance(
                    point.Latitude, point.Longitude,
                    records[i].Latitude, records[i].Longitude
                );

                if (distance <= epsilon)
                    neighbors.Add(i);
            }

            return neighbors;
        }

        // Метод для расширения кластера
        private void ExpandCluster(List<int> initialNeighbors, int clusterLabel, List<WildfireRecord> cluster)
        {
            var queue = new Queue<int>(initialNeighbors);
            while (queue.Count > 0)
            {
                int neighborIndex = queue.Dequeue();
                if (labels[neighborIndex] != 0 && labels[neighborIndex] != -1)
                    continue;

                labels[neighborIndex] = clusterLabel;
                cluster.Add(records[neighborIndex]);

                var furtherNeighbors = GetNeighbors(neighborIndex);
                if (furtherNeighbors.Count >= minPoints)
                {
                    foreach (var furtherNeighbor in furtherNeighbors)
                    {
                        if (labels[furtherNeighbor] == 0 || labels[furtherNeighbor] == -1)
                        {
                            queue.Enqueue(furtherNeighbor);
                        }
                    }
                }
            }
        }
    }
}