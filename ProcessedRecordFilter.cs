namespace RefactoringExample
{
    /// <summary>
    /// Валидация и отбор записей: возвращает обработанные записи,
    /// а про отклонённые сообщает через <see cref="IOutput"/>.
    /// </summary>
    public sealed class ProcessedRecordFilter
    {
        private readonly IOutput output;

        public ProcessedRecordFilter(IOutput output)
        {
            this.output = output;
        }

        public IReadOnlyList<DataRecord> SelectProcessed(IEnumerable<string> dataLines, CsvParser parser, IReadOnlyDictionary<string, int> columns)
        {
            var processedRecords = new List<DataRecord>();

            foreach (string line in dataLines)
            {
                DataRecord record = parser.ParseRecord(line, columns);

                if (record.IsProcessed)
                {
                    processedRecords.Add(record);
                }
                else
                {
                    output.WriteLine($"Skipping record with status: {record.Status}");
                }
            }

            return processedRecords;
        }
    }
}
