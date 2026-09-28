namespace RefactoringExample
{
    /// <summary>
    /// Абстракция хранилища. Импортер не знает, куда сохраняются записи:
    /// в реальную БД, в файл или никуда.
    /// </summary>
    public interface IRecordRepository
    {
        Task SaveAsync(IReadOnlyList<DataRecord> records);
    }

    /// <summary>
    /// Имитация сохранения в базу данных (как в исходной версии).
    /// </summary>
    public sealed class SimulatedRecordRepository : IRecordRepository
    {
        private const int SaveDelayMilliseconds = 10;

        private readonly IOutput output;

        public SimulatedRecordRepository(IOutput output)
        {
            this.output = output;
        }

        public async Task SaveAsync(IReadOnlyList<DataRecord> records)
        {
            foreach (DataRecord record in records)
            {
                output.WriteLine($"Saving record: {record.Id}");
                await Task.Delay(SaveDelayMilliseconds); // имитация асинхронной операции
            }
        }
    }
}
