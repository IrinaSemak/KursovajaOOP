using System;
using System.Collections.Generic;
using System.Linq;
using DbscanSharp;

namespace Library
{
    public class HdbscanCustom
    {
        private readonly double epsilon; // Радиус (epsilon) задан в километрах
        private readonly int minPoints;
        private readonly List<WildfireRecord> records;
        private int[] labels;

        public event Action<List<WildfireRecord>, int> ClusterFound;
        public int[] Labels => labels;

        public HdbscanCustom(double epsilonKm, int minPoints)
        {
            this.epsilon = epsilonKm;
            this.minPoints = minPoints;
            records = new List<WildfireRecord>();
            labels = Array.Empty<int>();
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

            // Вычисляем среднюю широту (в градусах), затем переводим в радианы
            double avgLatitude = records.Average(r => r.Latitude);
            double avgLatitudeRad = avgLatitude * Math.PI / 180.0;
            const double R = 6371; // Радиус Земли в километрах

            // Преобразуем координаты:
            // X = Latitude (в радианах) * R
            // Y = Longitude (в радианах) * R * cos(avgLatitudeRad)
            var points = records.Select(r => new DbscanSharp.Dbscan.Point(
                (float)(r.Latitude * Math.PI / 180.0 * R),
                (float)(r.Longitude * Math.PI / 180.0 * R * Math.Cos(avgLatitudeRad))
            )).ToList();

            // Создаем объект кластеризации Dbscan из библиотеки, epsilonKm уже в километрах.
            var dbscan = new Dbscan(points, (float)epsilon, minPoints);
            // Метод Fit() возвращает массив меток для каждой точки.
            labels = dbscan.Fit();

            // Переиндексация меток: шум (изначально -1) остаётся, а кластеры перенумеровываются последовательно (1,2,3…)
            Dictionary<int, int> clusterMapping = new Dictionary<int, int>();
            int nextClusterLabel = 1;
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == -1)
                    continue;
                if (!clusterMapping.ContainsKey(labels[i]))
                    clusterMapping[labels[i]] = nextClusterLabel++;
                labels[i] = clusterMapping[labels[i]];
            }

            // Группируем записи по меткам.
            var clustersDict = new Dictionary<int, List<WildfireRecord>>();
            for (int i = 0; i < labels.Length; i++)
            {
                int label = labels[i];
                if (!clustersDict.ContainsKey(label))
                    clustersDict[label] = new List<WildfireRecord>();
                clustersDict[label].Add(records[i]);
            }

            // Для каждого кластера (кроме шума) вызываем событие ClusterFound.
            foreach (var kvp in clustersDict)
            {
                if (kvp.Key == -1)
                    continue;
                ClusterFound?.Invoke(kvp.Value, kvp.Key);
            }
        }
    }
}
