using System;
using System.Collections.Generic;

namespace Library
{
    // Абстрактный базовый класс для алгоритмов кластеризации
    public abstract class BaseClusteringAlgorithm : IClusteringAlgorithm
    {
        // Поля для хранения данных
        protected readonly List<WildfireRecord> records;
        protected int[] labels;
        protected readonly double epsilon;
        protected readonly int minPoints;

        // Свойство для получения меток
        public int[] Labels => labels;

        // Конструктор
        protected BaseClusteringAlgorithm(double epsilon, int minPoints)
        {
            this.epsilon = epsilon;
            this.minPoints = minPoints;
            records = new List<WildfireRecord>();
            labels = Array.Empty<int>();
        }

        // Абстрактный метод кластеризации
        public abstract Dictionary<int, List<WildfireRecord>> Cluster(IEnumerable<WildfireRecord> inputRecords);

        // Метод для очистки и загрузки записей
        protected void LoadRecords(IEnumerable<WildfireRecord> inputRecords)
        {
            records.Clear();
            records.AddRange(inputRecords);
            labels = new int[records.Count];
        }
    }
}