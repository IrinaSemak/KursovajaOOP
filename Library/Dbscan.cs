using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace Library
{
    public class DbscanCustom
    {
        private readonly double epsilon;
        private readonly int minPoints;
        private readonly List<WildfireRecord> records;
        private int[] labels;
        private STRtree<int> spatialIndex;

        public event Action<List<WildfireRecord>, int> ClusterFound;
        public int[] Labels => labels;

        public DbscanCustom(double epsilon, int minPoints)
        {
            this.epsilon = epsilon;
            this.minPoints = minPoints;
            records = new List<WildfireRecord>();
            labels = Array.Empty<int>();
        }

        public void Cluster(IEnumerable<WildfireRecord> inputRecords)
        {
            try
            {
                records.Clear();
                records.AddRange(inputRecords);
                if (records.Count == 0) return;

                labels = new int[records.Count];
                BuildSpatialIndex();

                int clusterLabel = 0;
                for (int i = 0; i < records.Count; i++)
                {
                    if (labels[i] != 0) continue;

                    var neighbors = GetNeighbors(i);
                    if (neighbors.Count < minPoints)
                    {
                        labels[i] = -1;
                        continue;
                    }

                    clusterLabel++;
                    labels[i] = clusterLabel;
                    var cluster = new List<WildfireRecord>(neighbors.Count) { records[i] };
                    ExpandCluster(i, neighbors, clusterLabel, cluster);

                    ClusterFound?.Invoke(cluster, clusterLabel);
                }
            }
            catch (TypeInitializationException ex)
            {
                throw new Exception($"Ошибка инициализации NetTopologySuite: {ex.InnerException?.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка кластеризации: {ex.Message}", ex);
            }
        }

        private void BuildSpatialIndex()
        {
            spatialIndex = new STRtree<int>(10);
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].IsLatitudeMissing || records[i].IsLongitudeMissing ||
                    double.IsNaN(records[i].Latitude) || double.IsNaN(records[i].Longitude) ||
                    double.IsInfinity(records[i].Latitude) || double.IsInfinity(records[i].Longitude))
                {
                    Console.WriteLine($"Пропущена запись с индексом {i}: Lat={records[i].Latitude}, Lon={records[i].Longitude}");
                    continue;
                }
                var point = new Point(records[i].Longitude, records[i].Latitude);
                spatialIndex.Insert(point.EnvelopeInternal, i);
            }
            spatialIndex.Build();
        }

        private List<int> GetNeighbors(int pointIndex)
        {
            var record = records[pointIndex];
            var point = new Point(record.Longitude, record.Latitude);
            double degreeRadius = epsilon / 111.32;
            var envelope = point.EnvelopeInternal;
            envelope.ExpandBy(degreeRadius);

            var candidates = spatialIndex.Query(envelope);
            var neighbors = new List<int>(Math.Min(candidates.Count, 1000));

            foreach (var candidateIndex in candidates)
            {
                if (candidateIndex == pointIndex) continue;
                if (CalculateHaversineDistance(record, records[candidateIndex]) <= epsilon)
                {
                    neighbors.Add(candidateIndex);
                    if (neighbors.Count >= 1000) break;
                }
            }
            return neighbors;
        }

        private void ExpandCluster(int pointIndex, List<int> initialNeighbors, int clusterLabel, List<WildfireRecord> cluster)
        {
            var queue = new Queue<int>(initialNeighbors);
            while (queue.Count > 0)
            {
                int neighborIndex = queue.Dequeue();
                if (labels[neighborIndex] == -1 || labels[neighborIndex] == 0)
                {
                    labels[neighborIndex] = clusterLabel;
                    cluster.Add(records[neighborIndex]);

                    var furtherNeighbors = GetNeighbors(neighborIndex);
                    if (furtherNeighbors.Count >= minPoints)
                    {
                        foreach (var furtherNeighbor in furtherNeighbors)
                            if (labels[furtherNeighbor] == 0 || labels[furtherNeighbor] == -1)
                                queue.Enqueue(furtherNeighbor);
                    }
                }
            }
        }

        private double CalculateHaversineDistance(WildfireRecord p1, WildfireRecord p2)
        {
            const double R = 6371;
            double lat1 = ToRadians(p1.Latitude), lon1 = ToRadians(p1.Longitude);
            double lat2 = ToRadians(p2.Latitude), lon2 = ToRadians(p2.Longitude);
            double dLat = lat2 - lat1, dLon = lon2 - lon1;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}