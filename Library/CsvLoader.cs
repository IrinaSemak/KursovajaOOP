using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Library
{
    public static class CsvLoader
    {
        public static IEnumerable<WildfireRecord> LoadCsvStream(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Файл не найден: " + filePath);
                yield break;
            }

            StreamReader reader = new StreamReader(filePath);
            try
            {
                string header = reader.ReadLine();
                if (string.IsNullOrEmpty(header))
                {
                    Console.WriteLine("CSV-файл пуст.");
                    yield break;
                }

                var headers = ParseCsvLine(header);
                const int EXPECTED_COLUMNS = 47;
                if (headers.Length != EXPECTED_COLUMNS)
                {
                    Console.WriteLine($"Ошибка: ожидаемое количество столбцов {EXPECTED_COLUMNS}, найдено {headers.Length}.");
                    yield break;
                }

                int latIndex = Array.FindIndex(headers, h => h.Trim().ToLower() == "latitude");
                int lonIndex = Array.FindIndex(headers, h => h.Trim().ToLower() == "longitude");
                int xIndex = Array.FindIndex(headers, h => h.Trim().ToLower() == "x");
                int yIndex = Array.FindIndex(headers, h => h.Trim().ToLower() == "y");
                if (latIndex == -1 || lonIndex == -1 || xIndex == -1 || yIndex == -1)
                {
                    Console.WriteLine("Ошибка: отсутствуют столбцы 'latitude', 'longitude', 'x' или 'y'.");
                    yield break;
                }

                string line;
                int lineNumber = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    var parts = ParseCsvLine(line);
                    if (parts.Length != EXPECTED_COLUMNS)
                    {
                        Console.WriteLine($"Строка {lineNumber}: найдено {parts.Length} столбцов вместо {EXPECTED_COLUMNS}. Пропуск.");
                        continue;
                    }

                    var record = new WildfireRecord
                    {
                        StreetNumber = parts[3],
                        StreetName = parts[4],
                        StreetType = parts[5],
                        X = parts[45], 
                        Y = parts[46]
                    };

                    if (double.TryParse(parts[latIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                        record.Latitude = lat;
                    else
                        record.IsLatitudeMissing = true;

                    if (double.TryParse(parts[lonIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                        record.Longitude = lon;
                    else
                        record.IsLongitudeMissing = true;

                    // Проверяем корректность X и Y
                    if (!double.TryParse(record.X, NumberStyles.Any, CultureInfo.InvariantCulture, out double xValue))
                    {
                        Console.WriteLine($"Строка {lineNumber}: некорректное значение X = {record.X}. Пропуск.");
                        continue;
                    }

                    if (!double.TryParse(record.Y, NumberStyles.Any, CultureInfo.InvariantCulture, out double yValue))
                    {
                        Console.WriteLine($"Строка {lineNumber}: некорректное значение Y = {record.Y}. Пропуск.");
                        continue;
                    }

                    // Логируем координаты для отладки
                    if (!record.IsLatitudeMissing && !record.IsLongitudeMissing)
                        Console.WriteLine($"Запись {lineNumber}: Latitude = {record.Latitude}, Longitude = {record.Longitude}, X = {record.X}, Y = {record.Y}");

                    yield return record;
                }
            }
            finally
            {
                reader.Dispose();
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>(47);
            bool inQuotes = false;
            string field = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(field);
                    field = "";
                }
                else
                    field += c;
            }
            result.Add(field);
            return result.ToArray();
        }
    }
}