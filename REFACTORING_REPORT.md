# Отчёт о рефакторинге (вариант 6)

Проект: `RPKApp6` (C#, .NET 8, консольное приложение).
Исходный файл: `Program.cs`, 151 строка, 3 класса. Состояние «до»: 1 файл, 0 ошибок и **1 предупреждение** компилятора (CS0168 — неиспользуемая переменная `ex`).

---

## 1. Найденные проблемы качества

| # | Проблема | Где было | Почему это проблема / на что влияет |
|---|----------|----------|-------------------------------------|
| 1 | **Дублирование доменного знания** `record["status"] == "processed"` | `ImportData` (стр. 39) и `IsProcessed` (стр. 60) | Метод `IsProcessed` создан именно для этой проверки, но `ImportData` его не вызывает. Правило «что считается обработанной записью» существует в двух местах: правка в одном месте забудет другое. |
| 2 | **Дублирование форматирования вывода** | `OrderManager.Proc`, `OrderManager.Mgr` | Два метода отличаются только глаголом, строка `$"...{data.Count} records"` скопирована. |
| 3 | **Длинный метод с 6 задачами** — `ImportData`, 44 строки | стр. 13–56 | Читает файл, парсит CSV, валидирует, фильтрует, печатает, сохраняет, глушит ошибки. Невозможно изменить или протестировать одну обязанность, не задев остальные. |
| 4 | **Нарушение SRP у `DataImporter`**: работа с ФС + «БД» + консоль | стр. 9–72 | Три внешние зависимости внутри одного класса. Ни одну из них нельзя подменить в тестах — проверять класс можно только запуском реальной программы. |
| 5 | **Пустой `catch (Exception)`, проглатывающий всё** | стр. 52–55 | Ошибка чтения файла или битый CSV исчезает бесследно: пользователь видит «Application completed». Плюс мёртвая переменная `ex` (предупреждение CS0168). |
| 6 | **Непонятные имена**: `d`, `j`, `Proc`, `Mgr` | стр. 28, 29, 76, 82 | `d` не читается; `j` — индекс без контекста; `Proc`/`Mgr` — телеграфные аббревиатуры, назначение метода видно только из тела. |
| 7 | **Магические строки** `"id"`, `"status"`, `"processed"`, `','`, `Task.Delay(10)` | по всему файлу | 11 вхождений строковых литералов в разных местах. Опечатка (`"staus"`) даёт `KeyNotFoundException` в рантайме, а не ошибку компиляции. |
| 8 | **Примитивная однозначность**: `Dictionary<string, string>` вместо типа домена | вся модель данных | Отсутствует контракт: запись — просто словарь строк. Отсутствие обязательного поля → `KeyNotFoundException`; валидации схемы нет. |
| 9 | **Скрытое изменяемое состояние** `private List<...> data` | стр. 11, 41 | Поле не `readonly` и **не очищается** между вызовами: повторный `ImportData` продублирует записи. При этом прочитать импортированные данные извне нельзя (нет геттера), поэтому `Main` вынужден строить **второй, параллельный** список вручную (стр. 103–107). |
| 10 | **Риск `IndexOutOfRangeException` в парсере** | стр. 29–31 | `values[j]` при `headers.Length` больше числа полей строки. Исключение проглатывается пустым `catch` → импорт молча обрывается, ничего не сохранив. |
| 11 | **`Main` совмещает создание данных, связывание и вывод** | стр. 91–133 | Пять разных уровней в одном методе, M = 3. |
| 12 | **Мёртвый код** — закомментированный `ExportData` с `TODO` | стр. 125–129 | Комментарии-копии кода не компилируются, поэтому не могут быть проверены сборкой, и устаревают незаметно. |
| 13 | **Класс `OrderManager` не управляет заказами** | стр. 74 | В программе нет сущности «заказ», класс только печатает. Имя вводит в заблуждение. |
| 14 | `Program` объявлен как `class`, а не `static class`, хотя содержит только статические члены | стр. 89 | Мелочь, но лишний шум в дизайне. |

---

## 2. План рефакторинга → применённые шаблоны

| Шаг | Проблема | Применённый шаблон рефакторинга | Что сделано |
|-----|----------|-------------------------------|-------------|
| 1 | п. 6, 7 | **Rename Variable**, **Rename Method**, **Introduce Constant** | `d` → `parsedRecord`, `j` → `columnIndex`, `ImportData` → `ImportDataAsync`; литералы `"id"/"status"/"processed"` → константы `DataFields` |
| 2 | п. 8 | **Replace Magic Literal**, **Extract Class** (Introduce Parameter Object) | Введён тип домена `DataRecord` (`Id`, `Name`, `Status`) вместо `Dictionary<string,string>` |
| 3 | п. 3, 10 | **Extract Method**, **Split Method** | `ImportData` → `ReadLines` / `ParseHeader` / `ParseRecord` / `GetFieldValue` / `CollectProcessedRecords` / `SaveToDatabase`; удалено поле `data` (п. 9) |
| 4 | п. 1 | **Extract Function** + переиспользование результата | Правило переехало в свойство `DataRecord.IsProcessed`; метод `DataImporter.IsProcessed` **удалён** как дублирующий |
| 5 | п. 4, 9 | **Extract Class**, **Extract Interface** (DIP), **Split Class** по файлам | `IOutput`/`ConsoleOutput`, `IDataSource`/`CsvFileDataSource`, `IRecordRepository`/`SimulatedRecordRepository`, `CsvParser`, `ProcessedRecordFilter`; `DataImporter` стал оркестратором без состояния |
| 6 | п. 5 | **Extract Method** + удаление мёртвого кода | Пустой `catch` → `output.WriteLine($"Import failed: {ex.Message}")`; предупреждение CS0168 устранено |
| 7 | п. 2, 11, 13, 14 | **Rename Method**, **Rename Class**, **Extract Method**, **Remove Dead Code** | `Proc`/`Mgr` → `ProcessRecords`/`ManageRecords` + общий `ReportRecordsAction`; из `Main` вынесены `CreateSampleRecords` и `ReportRecordStatuses`; `OrderManager` → `RecordWorkflow`; `Program` → `static class`; удалён закомментированный `ExportData` |

---

## 3. Что изменилось в коде

### Структура проекта

**Было:** 1 файл `Program.cs` (151 строка), 3 класса (`DataImporter`, `OrderManager`, `Program`).

**Стало:** 9 файлов, 12 типов, у каждого одна ответственность:

| Файл | Тип(ы) | Ответственность (SRP) |
|------|---------|----------------------|
| `DataRecord.cs` | `DataFields`, `DataRecord` | Домен: константы полей и запись. Никакого I/O. |
| `CsvParser.cs` | `CsvParser` | Разбор текста CSV → `DataRecord`. Ни I/O, ни печати, ни сохранения. |
| `ProcessedRecordFilter.cs` | `ProcessedRecordFilter` | Валидация и отбор обработанных записей + сообщения об отклонённых. |
| `IDataSource.cs` | `IDataSource`, `CsvFileDataSource` | Чтение строк из источника данных. |
| `IRecordRepository.cs` | `IRecordRepository`, `SimulatedRecordRepository` | Сохранение записей. |
| `IOutput.cs` | `IOutput`, `ConsoleOutput` | Вывод. Единственное место, где встречается `Console.WriteLine`. |
| `DataImporter.cs` | `DataImporter` | **Только порядок вызовов** (оркестратор), состояния нет. |
| `RecordWorkflow.cs` | `RecordWorkflow` | Два этапа работы с записями + общее форматирование. |
| `Program.cs` | `Program` | Composition root: сборка объектов и вызов. |

### Удалённые сущности

- `DataImporter.ImportData` → переименована в `ImportDataAsync` (больше не принимает путь — путь приходит в конструктор через `IDataSource`)
- `DataImporter.IsProcessed` — **удалён**, логика перенесена в `DataRecord.IsProcessed`
- `DataImporter.SaveToDatabase` → переехала в `SimulatedRecordRepository.SaveAsync`
- поле `DataImporter.data` — **удалено** (скрытое состояние); промежуточный результат возвращается явно
- `OrderManager` → переименован в `RecordWorkflow`; `Proc` → `ProcessRecords`, `Mgr` → `ManageRecords`
- закомментированный блок `// TODO: реализовать экспорт данных / ExportData` (мёртвый код)

### Добавленные сущности

`DataFields`, `DataRecord` (+`IsProcessed`), `IOutput`, `ConsoleOutput`, `IDataSource`, `CsvFileDataSource`, `IRecordRepository`, `SimulatedRecordRepository`, `CsvParser` (`ParseHeader`, `ParseRecord`, `GetFieldValue`), `ProcessedRecordFilter.SelectProcessed`, `RecordWorkflow.ReportRecordsAction`, `DataImporter.SkipHeader`, `Program.CreateSampleRecords`, `Program.ReportRecordStatuses`.

### Фрагменты «до / после»

**Оркестрация импорта (было 44 строки в одном методе):**

```csharp
// БЫЛО
public async Task ImportData(string filePath)
{
    try
    {
        string[] lines = File.ReadAllLines(filePath);
        string[] headers = lines[0].Split(',');
        var processedData = lines.Skip(1)
            .Where(line => !string.IsNullOrEmpty(line))
            .Select(line => { /* парсинг + цикл for */ });
        foreach (var record in processedData)
        {
            if (record["status"] == "processed") data.Add(record);
            else Console.WriteLine($"Skipping record with status: {record["status"]}");
        }
        await SaveToDatabase();
    }
    catch (Exception ex) { }
}

// СТАЛО
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
```

**Дублирование правила «processed» (было в двух местах → стало в одном):**

```csharp
// БЫЛО
if (record["status"] == "processed") { ... }   // ImportData
public bool IsProcessed(Dictionary<string,string> record)
    => record["status"] == "processed";          // IsProcessed

// СТАЛО — единственное место
public bool IsProcessed => Status == DataFields.ProcessedStatus;
```

**Дублирование форматирования в `OrderManager` (было дважды → стало один раз):**

```csharp
// БЫЛО
public void Proc(List<DataRecord> data) { output.WriteLine($"Processing {data.Count} records"); }
public void Mgr(List<DataRecord> data) { output.WriteLine($"Managing {data.Count} records"); }

// СТАЛО
public void ProcessRecords(IReadOnlyList<DataRecord> records) => ReportRecordsAction("Processing", records);
public void ManageRecords(IReadOnlyList<DataRecord> records)  => ReportRecordsAction("Managing", records);

private void ReportRecordsAction(string action, IReadOnlyList<DataRecord> records)
    => output.WriteLine($"{action} {records.Count} records");
```

**Ввод-вывод вместо `Console` в доменной логике:**

```csharp
// БЫЛО — ProcessedRecordFilter пишет в консоль напрямую
Console.WriteLine($"Skipping record with status: {record.Status}");

// СТАЛО
output.WriteLine($"Skipping record with status: {record.Status}");   // подменяется в тестах
```

**Исчезнувший `KeyNotFoundException` (словарь → тип домена):**

```csharp
// БЫЛО — падает в рантайме при опечатке в данных
Console.WriteLine($"Record {record["id"]} is processed");

// СТАЛО — контракт виден в сигнатуре
output.WriteLine($"Record {record.Id} {statusText}");
```

---

## 4. Почему код стал лучше (метрики)

| Метрика | Было | Стало |
|---------|------|-------|
| Файлов / типов | 1 / 3 | 9 / 12 |
| Строк кода (без XML-доков и пустых) | ~130 | 257 |
| Из них XML-документации | 0 | 59 |
| Вхождений магических строк-ключей `"id"/"status"/"processed"` | **11** | **4** (и все 4 — в объявлениях констант, использований 0) |
| Реализаций правила «запись обработана» | 2 | 1 |
| Копий строки форматирования `... {Count} records` | 2 | 1 |
| Прямых обращений к `Console` в логике | 6 | 1 (только в `ConsoleOutput`) |
| Пустых `catch` | 1 | 0 |
| Скрытых изменяемых полей | 1 | 0 |
| Предупреждений компилятора | 1 (CS0168) | 0 |
| Длина самого длинного метода | 44 строки (`ImportData`) | 20 строк (`SelectProcessed`) |
| Длина «метода с логикой импорта» | 44 строки, 6 задач | 4 строки вызовов |

**Про «избежание повторяющихся строк».** Абсолютный объём кода вырос (+127 строк кода), и это осознанно: 59 строк — XML-документация, 18 — конструкторы с внедрением зависимостей, остальное — сигнатуры с типами вместо `Dictionary<string,string>`. При этом *продублированная* логика сократилась:

- ручной разбор строки в словарь (7 строк `for` + `Split` + `Trim` в лямбде) → 4 строки одного метода `GetFieldValue`, вызываемого из трёх мест без копипаста;
- правило «processed» — 2 реализации → 1;
- форматирование строки в `OrderManager` — 2 реализации → 1;
- чтение файла, печать и «сохранение в БД» больше не повторяются по коду, а вынесены в отдельные классы и могут быть переиспользованы (например, второй источник данных = одна новая реализация `IDataSource`, а не копия `ImportData`).

**Про читаемость.**

1. `ImportDataAsync` читается как оглавление: «прочитали → разобрали заголовок → отобрали → сохранили». Задача «что здесь происходит» определяется по именам, а не по чтению 44 строк с вложенными лямбдами.
2. Лямбда `.Select(line => { ...for... })` (анонимный метод с собственным контекстом и изменяемым словарём) заменена именованным классом `CsvParser` — у неё появилось имя, по которому её можно найти и переиспользовать, и она отдельно тестируется без файловой системы.
3. Имена стали предметными: `d` → `parsedRecord`, `j` → `columnIndex`, `Proc` → `ProcessRecords`, `Mgr` → `ManageRecords`, `testData` → `sampleRecords`, `OrderManager` → `RecordWorkflow`.
4. Отсутствие колонок и пустые строки больше не роняют импорт молча — причина попадает в вывод.
5. Зависимости видны в сигнатуре конструктора: `new DataImporter(dataSource: …, repository: …, filter: …, parser: …, output: …)` сразу показывает, что у импортёра 5 внешних точек, которые можно подменить.
6. `Console.WriteLine` остался ровно в одном месте, поэтому «куда печатает программа» определяется одним взглядом на `ConsoleOutput`.

---

## 5. Проверка: цикломатическая сложность

Метрика: `M = 1 +` число точек принятия решения (`if`, `else if`, циклы, `case`, `catch`, `&&`, `||`, `?:`).

| Метод (назначение) | M было | M стало | Где находится теперь |
|--------------------|--------|---------|---------------------|
| **Оркестрация импорта** | **5** (`catch`, `foreach`, `if`, `for`) | **2** (`catch`) | `DataImporter.ImportDataAsync` |
| Основной сценарий программы | 3 (`foreach`, `if`/`else`) | **1** | `Program.Main` |
| Разбор строки CSV → запись | 2 (внутри лямбды `Select`: `for`) | 1 | `CsvParser.ParseRecord` |
| Разбор заголовка | 1 (внутри лямбды `Select`: `for`) | 2 | `CsvParser.ParseHeader` |
| Валидация и фильтрация | 1 (`if` внутри `ImportData`) | 3 (`foreach`, `if`) | `ProcessedRecordFilter.SelectProcessed` |
| Сохранение | 2 (`foreach`) | 2 (`foreach`) | `SimulatedRecordRepository.SaveAsync` |
| Проверка статуса записи | 1 ×2 (дубликаты) | 1 ×1 | `DataRecord.IsProcessed` |
| `OrderManager` → `RecordWorkflow` | 1 и 1 | 1 и 1 | `ProcessRecords`, `ManageRecords` |

**Ключевой результат:** M метода, который запускает импорт, упал **с 5 до 2** (−60 %). `Main` — **с 3 до 1**. Ни один метод в итоговом коде не имеет M > 3; исходный `ImportData` с M = 5 содержал четыре разных типа работы в одном методе.

Дополнительно: M нигде не был «уменьшен» переносом того же логика в другое место без декомпозиции — наоборот, каждый новый класс выполняет одну работу, поэтому его M = 1–3 и он покрывается отдельным тестом.

---

## 6. Проверка сохранности функциональности

Метод: снят baseline-вывод исходной программы, затем после **каждого** из 7 шагов выполнялись `dotnet build` и запуск с последующим побайтовым сравнением вывода.

Результат по всем шагам: **build OK, предупреждений нет, вывод полностью совпадает с baseline**. Итоговый вывод:

```
Created test CSV file: test_data.csv
Skipping record with status: pending
Skipping record with status: rejected
Saving record: 1
Saving record: 3
Processing 2 records
Managing 2 records
Record 1 is processed
Record 2 is not processed
Application completed. Press any key to exit.
```

Порядок строк сохранён, включая сообщения «Skipping…» (печатаются в момент отбора, до сохранения) — как в исходной версии.

Дополнительно проверен сценарий сбоя (временная подстановка несуществующего файла, затем откат):

```
Created test CSV file: test_data.csv
Import failed: Could not find file '...missing_file.csv'.   <-- БЫЛО: тишина, ошибка проглатывалась
Processing 2 records
Managing 2 records
Record 1 is processed
Record 2 is not processed
Application completed. Press any key to exit.
```

Программа по-прежнему не падает (поведение сохранено), но причина теперь видна.

---

## 7. Что сознательно НЕ менялось (сохранение функционала)

- `await Task.Delay(10)` в цикле сохранения оставлен последовательным: параллельный `Task.WhenAll` изменил бы порядок строк вывода. Заменять на реальную БД — отдельная задача.
- `catch (Exception)` оставлен широким (не сужен до `FileNotFoundException`/`FormatException`), чтобы сохранить исходную семантику «любая ошибка импорта не роняет программу».
- Исключения при несоответствии схемы CSV по-прежнему прерывают импорт целиком, как и раньше (теперь с сообщением), а не «пропускаются построчно».
- Избыточные `using` оставлены: при `ImplicitUsings=enable` они дублируются, но делают файлы самодостаточными при переносе в проект с `ImplicitUsings=disable`.
- `DataFields` использует регистрозависимое сравнение — как в исходном коде (осознанная строгость).

## 8. Что можно улучшить следующим шагом (вне рамок задания)

- Добавить проект с юнит-тестами: `CsvParser`, `ProcessedRecordFilter` и `DataRecord` теперь тестируются без файлов и консоли — это и было целью выделения интерфейсов.
- Заменить `SimulatedRecordRepository` на реальную реализацию `IRecordRepository` (EF Core) — импортёр менять не придётся.
- `await foreach` / `IAsyncEnumerable<DataRecord>` для поточного импорта больших файлов.
