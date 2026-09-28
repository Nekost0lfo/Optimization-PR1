namespace RefactoringExample
{
    /// <summary>
    /// Абстракция вывода. Введена, чтобы доменная логика импорта не зависела от <see cref="System.Console"/>
    /// напрямую: вывод можно подменить в тестах.
    /// </summary>
    public interface IOutput
    {
        void WriteLine(string message);
    }

    /// <summary>
    /// Реализация вывода через консоль.
    /// </summary>
    public sealed class ConsoleOutput : IOutput
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
