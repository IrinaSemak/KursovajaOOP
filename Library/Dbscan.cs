using System;
using System.Collections.Generic;

namespace Library
{
    public class DbscanCustom
    {
        private readonly double epsilon; // Радиус в км
        private readonly int minPoints;
        private readonly List<WildfireRecord> records;
        private int[] labels;

        public event Action<List<WildfireRecord>, int> ClusterFound;
        public int[] Labels => labels;

        public DbscanCustom(double epsilon, int minPoints)
        {
            this.epsilon = epsilon;
            this.minPoints = minPoints;
            records = new List<WildfireRecord>();
            labels = new int[0];
        }

        public void Cluster(IEnumerable<WildfireRecord> inputRecords)
        {
            records.Clear();
            records.AddRange(inputRecords);

            if (records.Count == 0)
            {
                labels = new int[0];
                return;
            }

            labels = new int[records.Count];
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

                ClusterFound?.Invoke(cluster, clusterLabel);
            }
        }

        private List<int> GetNeighbors(int pointIndex)
        {
            var neighbors = new List<int>();
            var point = records[pointIndex];

            for (int i = 0; i < records.Count; i++)
            {
                if (i == pointIndex)
                    continue;

                double distance = CalculateHaversineDistance(
                    point.Latitude, point.Longitude,
                    records[i].Latitude, records[i].Longitude
                );

                if (distance <= epsilon)
                    neighbors.Add(i);
            }

            return neighbors;
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Радиус Земли в км
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

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
