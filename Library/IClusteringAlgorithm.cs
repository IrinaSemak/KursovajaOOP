using Library;
using System.Collections.Generic;

namespace Library
{
    // Интерфейс для алгоритмов кластеризации
    public interface IClusteringAlgorithm
    {
        // Метод для выполнения кластеризации, возвращает словарь кластеров
        Dictionary<int, List<WildfireRecord>> Cluster(IEnumerable<WildfireRecord> records);

        // Свойство для получения меток кластеров
        int[] Labels { get; }
    }
}