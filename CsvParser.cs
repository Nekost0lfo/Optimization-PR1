namespace RefactoringExample
{
    /// <summary>
    /// Разбор CSV-строк в объекты <see cref="DataRecord"/>.
    /// Единственная ответственность класса — превращение текста в данные:
    /// ни ввода-вывода, ни печати, ни сохранения здесь нет.
    /// </summary>
    public sealed class CsvParser
    {
        private const char FieldSeparator = ',';

        /// <summary>
        /// Сопоставляет имена колонок их номерам.
        /// </summary>
        public IReadOnlyDictionary<string, int> ParseHeader(IReadOnlyList<string> lines)
        {
            string[] headerCells = lines[0].Split(FieldSeparator);
            var columns = new Dictionary<string, int>(headerCells.Length);

            for (int columnIndex = 0; columnIndex < headerCells.Length; columnIndex++)
            {
                columns[headerCells[columnIndex].Trim()] = columnIndex;
            }

            return columns;
        }

        /// <summary>
        /// Разбирает одну строку данных, пропуская строку заголовка.
        /// </summary>
        public DataRecord ParseRecord(string line, IReadOnlyDictionary<string, int> columns)
        {
            string[] values = line.Split(FieldSeparator);

            return new DataRecord(
                GetFieldValue(values, columns, DataFields.Id),
                GetFieldValue(values, columns, DataFields.Name),
                GetFieldValue(values, columns, DataFields.Status));
        }

        private static string GetFieldValue(string[] values, IReadOnlyDictionary<string, int> columns, string fieldName)
        {
            int columnIndex = columns[fieldName];
            return values[columnIndex].Trim();
        }
    }
}
