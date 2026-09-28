namespace RefactoringExample
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var output = new ConsoleOutput();

            // создание тестового CSV файла
            string testFilePath = "test_data.csv";
            CreateTestCsvFile(testFilePath, output);

            // сборка объектов — единственное место, где известны конкретные реализации
            var importer = new DataImporter(
                dataSource: new CsvFileDataSource(testFilePath),
                repository: new SimulatedRecordRepository(output),
                filter: new ProcessedRecordFilter(output),
                parser: new CsvParser(),
                output: output);

            await importer.ImportDataAsync();

            var workflow = new RecordWorkflow(output);
            IReadOnlyList<DataRecord> sampleRecords = CreateSampleRecords();

            workflow.ProcessRecords(sampleRecords);
            workflow.ManageRecords(sampleRecords);

            // демонстрация проверки статуса
            ReportRecordStatuses(sampleRecords, output);

            output.WriteLine("Application completed. Press any key to exit.");
            Console.ReadKey();
        }

        private static void CreateTestCsvFile(string filePath, IOutput output)
        {
            string[] lines =
            {
                "id,name,status",
                "1,Product A,processed",
                "2,Product B,pending",
                "3,Product C,processed",
                "4,Product D,rejected"
            };

            File.WriteAllLines(filePath, lines);
            output.WriteLine($"Created test CSV file: {filePath}");
        }

        private static IReadOnlyList<DataRecord> CreateSampleRecords()
        {
            return new List<DataRecord>
            {
                new DataRecord("1", "Product A", DataFields.ProcessedStatus),
                new DataRecord("2", "Product B", "pending")
            };
        }

        private static void ReportRecordStatuses(IReadOnlyList<DataRecord> records, IOutput output)
        {
            foreach (DataRecord record in records)
            {
                string statusText = record.IsProcessed ? "is processed" : "is not processed";
                output.WriteLine($"Record {record.Id} {statusText}");
            }
        }
    }
}
