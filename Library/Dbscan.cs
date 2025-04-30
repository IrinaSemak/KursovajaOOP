using System;
using System.Collections.Generic;

namespace Library
{
    // Класс DbscanCustom выполняет кластеризацию точек (группирует их)
    public class DbscanCustom
    {
        private readonly double epsilon; // Максимальное расстояние между точками в одном кластере
        private readonly int minPoints; // Минимальное количество точек для создания кластера
        private List<WildfireRecord> records; // Список всех точек
        private int[] labels; // Метки для каждой точки (какой кластер или шум)

        // Событие, которое вызывается, когда найден новый кластер
        public event Action<List<WildfireRecord>, int> ClusterFound;

        // Свойство, чтобы получить метки точек
        public int[] Labels => labels;

        // Конструктор: задаём параметры epsilon и minPoints
        public DbscanCustom(double epsilon, int minPoints)
        {
            this.epsilon = epsilon;
            this.minPoints = minPoints;
            records = new List<WildfireRecord>();
            labels = new int[0];
        }

        // Главный метод для кластеризации
        public void Cluster(IEnumerable<WildfireRecord> inputRecords)
        {
            // Очищаем старые данные
            records.Clear();
            records.AddRange(inputRecords);

            // Если нет точек, выходим
            if (records.Count == 0)
            {
                labels = new int[0];
                return;
            }

            // Создаём массив меток для каждой точки
            labels = new int[records.Count];

            int clusterLabel = 0; // Номер текущего кластера

            // Проходим по всем точкам
            for (int i = 0; i < records.Count; i++)
            {
                // Если точка уже обработана, пропускаем её
                if (labels[i] != 0) continue;

                // Находим соседей точки
                var neighbors = GetNeighbors(i);

                // Если соседей слишком мало, помечаем точку как шум
                if (neighbors.Count < minPoints)
                {
                    labels[i] = -1; // -1 означает шум
                    continue;
                }

                // Создаём новый кластер
                clusterLabel++;
                labels[i] = clusterLabel; // Помечаем точку как часть кластера
                var cluster = new List<WildfireRecord> { records[i] }; // Начинаем кластер с этой точки

                // Расширяем кластер, добавляя соседей
                ExpandCluster(neighbors, clusterLabel, cluster);

                // Сообщаем, что найден новый кластер
                ClusterFound?.Invoke(cluster, clusterLabel);
            }
        }

        // Метод для поиска соседей точки
        private List<int> GetNeighbors(int pointIndex)
        {
            var neighbors = new List<int>();
            var point = records[pointIndex];

            // Проходим по всем точкам
            for (int i = 0; i < records.Count; i++)
            {
                // Пропускаем саму точку
                if (i == pointIndex) continue;

                // Вычисляем расстояние между точками
                double distance = CalculateDistance(
                    point.Latitude, point.Longitude,
                    records[i].Latitude, records[i].Longitude
                );

                // Если расстояние меньше epsilon, добавляем точку в соседи
                if (distance <= epsilon)
                {
                    neighbors.Add(i);
                }
            }

            return neighbors;
        }

        // Метод для вычисления расстояния между двумя точками (в километрах)
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Радиус Земли в километрах
            const double R = 6371;

            // Переводим градусы в радианы
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            // Формула гаверсинуса для вычисления расстояния
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // Метод для расширения кластера
        private void ExpandCluster(List<int> initialNeighbors, int clusterLabel, List<WildfireRecord> cluster)
        {
            // Создаём очередь для обработки соседей
            var queue = new Queue<int>(initialNeighbors);

            // Пока есть соседи для обработки
            while (queue.Count > 0)
            {
                // Берём следующую точку из очереди
                int neighborIndex = queue.Dequeue();

                // Если точка уже обработана (не шум и не новая), пропускаем
                if (labels[neighborIndex] != 0 && labels[neighborIndex] != -1) continue;

                // Помечаем точку как часть кластера
                labels[neighborIndex] = clusterLabel;
                cluster.Add(records[neighborIndex]);

                // Находим соседей этой точки
                var furtherNeighbors = GetNeighbors(neighborIndex);

                // Если у точки достаточно соседей, добавляем их в очередь
                if (furtherNeighbors.Count >= minPoints)
                {
                    foreach (var furtherNeighbor in furtherNeighbors)
                    {
                        // Добавляем только необработанные точки или шум
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