namespace RefactoringExample
{
    /// <summary>
    /// Имена полей CSV-файла и значения статусов, которые используются в приложении.
    /// Константы вынесены, чтобы строковые литералы не дублировались по коду.
    /// </summary>
    public static class DataFields
    {
        public const string Id = "id";
        public const string Name = "name";
        public const string Status = "status";

        public const string ProcessedStatus = "processed";
    }

    /// <summary>
    /// Одна запись импортируемых данных. Заменяет <c>Dictionary&lt;string, string&gt;</c>:
    /// контракт полей виден из сигнатуры, обращение по строковому ключу невозможно.
    /// </summary>
    public sealed class DataRecord
    {
        public DataRecord(string id, string name, string status)
        {
            Id = id;
            Name = name;
            Status = status;
        }

        public string Id { get; }
        public string Name { get; }
        public string Status { get; }

        /// <summary>
        /// Единственное место в программе, где определено, что считается обработанной записью.
        /// </summary>
        public bool IsProcessed => Status == DataFields.ProcessedStatus;
    }
}
