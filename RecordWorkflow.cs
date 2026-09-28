namespace RefactoringExample
{
    /// <summary>
    /// Демонстрация этапов работы с импортированными записями.
    /// Переименован из <c>OrderManager</c>: в программе нет сущности «заказ»,
    /// класс оперирует записями импорта.
    /// </summary>
    public class RecordWorkflow
    {
        private readonly IOutput output;

        public RecordWorkflow(IOutput output)
        {
            this.output = output;
        }

        public void ProcessRecords(IReadOnlyList<DataRecord> records)
        {
            ReportRecordsAction("Processing", records);
        }

        public void ManageRecords(IReadOnlyList<DataRecord> records)
        {
            ReportRecordsAction("Managing", records);
        }

        /// <summary>
        /// Общее форматирование для обоих этапов: раньше строка собиралась в двух методах подряд.
        /// </summary>
        private void ReportRecordsAction(string action, IReadOnlyList<DataRecord> records)
        {
            output.WriteLine($"{action} {records.Count} records");
        }
    }
}
