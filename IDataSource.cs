namespace RefactoringExample
{
    /// <summary>
    /// Абстракция источника данных. Позволяет <see cref="DataImporter"/> работать с любым
    /// источником строк, а не только с файловой системой.
    /// </summary>
    public interface IDataSource
    {
        IReadOnlyList<string> ReadLines();
    }

    /// <summary>
    /// Источник данных — CSV-файл.
    /// </summary>
    public sealed class CsvFileDataSource : IDataSource
    {
        private readonly string filePath;

        public CsvFileDataSource(string filePath)
        {
            this.filePath = filePath;
        }

        public IReadOnlyList<string> ReadLines()
        {
            return File.ReadAllLines(filePath);
        }
    }
}
