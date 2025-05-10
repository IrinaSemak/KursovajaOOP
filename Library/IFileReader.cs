using Library;
using System.Collections.Generic;

namespace Library
{
    // Интерфейс для чтения данных из файла
    public interface IFileReader
    {
        // Метод для загрузки записей из файла
        IEnumerable<WildfireRecord> LoadRecords(string filePath);
    }
}