using System;
using System.Collections.Generic;
using System.Linq;
using DbscanSharp;
using Library;

namespace Library
{
    // Класс для реализации алгоритма HDBSCAN (упрощённая версия как DBSCAN)
    public class HdbscanAlgorithm : BaseClusteringAlgorithm
    {
        // Конструктор
        public HdbscanAlgorithm(double epsilon, int minPoints)
            : base(epsilon, minPoints)
        {
        }

        // Реализация метода кластеризации
        public override Dictionary<int, List<WildfireRecord>> Cluster(IEnumerable<WildfireRecord> inputRecords)
        {
            LoadRecords(inputRecords);
            var clustersDict = new Dictionary<int, List<WildfireRecord>>();

            if (records.Count == 0)
                return clustersDict;

            // Вычисляем среднюю широту и переводим в радианы
            double avgLatitude = records.Average(r => r.Latitude);
            double avgLatitudeRad = avgLatitude * Math.PI / 180.0;
            const double R = 6371; // Радиус Земли в километрах

            // Преобразуем координаты в двумерные точки
            var points = records.Select(r => new DbscanSharp.Dbscan.Point(
                (float)(r.Latitude * Math.PI / 180.0 * R),
                (float)(r.Longitude * Math.PI / 180.0 * R * Math.Cos(avgLatitudeRad))
            )).ToList();

            // Выполняем кластеризацию с помощью библиотеки DbscanSharp
            var dbscan = new Dbscan(points, (float)epsilon, minPoints);
            labels = dbscan.Fit();

            // Переиндексация меток
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

            // Группируем записи по меткам
            for (int i = 0; i < labels.Length; i++)
            {
                int label = labels[i];
                if (!clustersDict.ContainsKey(label))
                    clustersDict[label] = new List<WildfireRecord>();
                clustersDict[label].Add(records[i]);
            }

            return clustersDict;
        }
    }
}