namespace RefactoringExample
{
    /// <summary>
    /// Оркестратор импорта: читает данные, отбирает обработанные записи и передаёт их в хранилище.
    /// Содержит только порядок вызовов — вся работа делегирована зависимостям.
    /// </summary>
    public sealed class DataImporter
    {
        private readonly IDataSource dataSource;
        private readonly IRecordRepository repository;
        private readonly ProcessedRecordFilter filter;
        private readonly CsvParser parser;
        private readonly IOutput output;

        public DataImporter(IDataSource dataSource, IRecordRepository repository, ProcessedRecordFilter filter, CsvParser parser, IOutput output)
        {
            this.dataSource = dataSource;
            this.repository = repository;
            this.filter = filter;
            this.parser = parser;
            this.output = output;
        }

        public async Task ImportDataAsync()
        {
            try
            {
                IReadOnlyList<string> lines = dataSource.ReadLines();
                IReadOnlyDictionary<string, int> columns = parser.ParseHeader(lines);

                IReadOnlyList<DataRecord> processedRecords = filter.SelectProcessed(SkipHeader(lines), parser, columns);

                await repository.SaveAsync(processedRecords);
            }
            catch (Exception ex)
            {
                // ошибка импорта не должна ронять программу, но она обязана быть видна
                output.WriteLine($"Import failed: {ex.Message}");
            }
        }

        private static IEnumerable<string> SkipHeader(IReadOnlyList<string> lines)
        {
            return lines.Skip(1).Where(line => !string.IsNullOrEmpty(line));
        }
    }
}
