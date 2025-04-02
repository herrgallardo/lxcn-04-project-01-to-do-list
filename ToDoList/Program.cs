using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TodoListApp
{
    public enum TaskStatus { Pending, InProgress, Done }
    public enum Priority { Low, Medium, High, Critical }
    public enum Recurrence { None, Daily, Weekly, Monthly, Yearly, Weekdays, Weekends }

    public class Task
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public string Project { get; set; } = string.Empty;
        public Priority Priority { get; set; } = Priority.Medium;
        public List<string> Tags { get; set; } = new List<string>();
        public Recurrence Recurrence { get; set; } = Recurrence.None;

        // Computed properties
        public bool IsOverdue => Status != TaskStatus.Done && DueDate < DateTime.Today;
        public bool IsDueSoon => Status != TaskStatus.Done && !IsOverdue && DueDate <= DateTime.Today.AddDays(2);
    }

    public static class NaturalDateParser
    {
        public static DateTime Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return DateTime.Today;

            input = input.ToLowerInvariant().Trim();

            return input switch
            {
                "today" => DateTime.Today,
                "tomorrow" => DateTime.Today.AddDays(1),
                "next week" => DateTime.Today.AddDays(7),
                "yesterday" => DateTime.Today.AddDays(-1),
                "next month" => DateTime.Today.AddMonths(1),
                "next year" => DateTime.Today.AddYears(1),
                "monday" or "mon" => GetNextWeekday(DayOfWeek.Monday),
                "tuesday" or "tue" => GetNextWeekday(DayOfWeek.Tuesday),
                "wednesday" or "wed" => GetNextWeekday(DayOfWeek.Wednesday),
                "thursday" or "thu" => GetNextWeekday(DayOfWeek.Thursday),
                "friday" or "fri" => GetNextWeekday(DayOfWeek.Friday),
                "saturday" or "sat" => GetNextWeekday(DayOfWeek.Saturday),
                "sunday" or "sun" => GetNextWeekday(DayOfWeek.Sunday),
                "end of week" => GetEndOfWeek(),
                "end of month" => GetEndOfMonth(),
                "end of year" => new DateTime(DateTime.Today.Year, 12, 31),
                _ when input.StartsWith("in ") => ParseRelativeDate(input[3..]),
                _ when input.StartsWith("next ") => ParseNextOccurrence(input[5..]),
                _ => TryParseStandardDate(input)
            };
        }

        private static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
        {
            DateTime date = DateTime.Today;
            int daysToAdd = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;
            if (daysToAdd == 0) daysToAdd = 7; // If today is the target day, get next week
            return date.AddDays(daysToAdd);
        }

        private static DateTime GetEndOfWeek()
        {
            DateTime date = DateTime.Today;
            int daysToAdd = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
            return date.AddDays(daysToAdd);
        }

        private static DateTime GetEndOfMonth()
        {
            DateTime date = DateTime.Today;
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }

        private static DateTime ParseNextOccurrence(string occurrence)
        {
            return occurrence switch
            {
                "monday" or "mon" => GetNextWeekday(DayOfWeek.Monday),
                "tuesday" or "tue" => GetNextWeekday(DayOfWeek.Tuesday),
                "wednesday" or "wed" => GetNextWeekday(DayOfWeek.Wednesday),
                "thursday" or "thu" => GetNextWeekday(DayOfWeek.Thursday),
                "friday" or "fri" => GetNextWeekday(DayOfWeek.Friday),
                "saturday" or "sat" => GetNextWeekday(DayOfWeek.Saturday),
                "sunday" or "sun" => GetNextWeekday(DayOfWeek.Sunday),
                "week" => DateTime.Today.AddDays(7),
                "month" => DateTime.Today.AddMonths(1),
                "year" => DateTime.Today.AddYears(1),
                _ => DateTime.Today
            };
        }

        private static DateTime ParseRelativeDate(string relativeInput)
        {
            var parts = relativeInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out int amount)) return DateTime.Today;

            return parts[1] switch
            {
                "day" or "days" => DateTime.Today.AddDays(amount),
                "week" or "weeks" => DateTime.Today.AddDays(amount * 7),
                "month" or "months" => DateTime.Today.AddMonths(amount),
                "year" or "years" => DateTime.Today.AddYears(amount),
                _ => DateTime.Today
            };
        }

        private static DateTime TryParseStandardDate(string input)
        {
            string[] formats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "d-MMM", "d-MMM-yyyy", "d MMM", "d MMM yyyy" };

            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactResult))
                return exactResult;

            if (DateTime.TryParse(input, out var result))
                return result;

            return DateTime.Today;
        }
    }

    public class TaskManager
    {
        private List<Task> _tasks = new();
        private List<Task> _deletedTasks = new(); // For undo functionality
        private static readonly string TasksFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks.json");
        private static readonly string BackupFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks_backup.json");

        public TaskManager() => Load();

        public void Add(Task task) { _tasks.Add(task); Save(); }
        public IEnumerable<Task> GetAll() => _tasks;
        public IEnumerable<Task> GetSortedByDate() => _tasks.OrderBy(t => t.DueDate);
        public IEnumerable<Task> GetSortedByProject() => _tasks.OrderBy(t => t.Project);
        public IEnumerable<Task> Search(string term) => _tasks.Where(t =>
            t.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.Project.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)));
        public Task? Find(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);
        public void Update(Task task) { var i = _tasks.FindIndex(t => t.Id == task.Id); if (i >= 0) { _tasks[i] = task; Save(); } }

        // Enhanced delete with undo functionality
        public bool Delete(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;

            _tasks.Remove(task);
            _deletedTasks.Add(task); // Store for undo

            Save();
            return true;
        }

        // Restores the most recently deleted task
        public bool UndoDelete()
        {
            if (_deletedTasks.Count == 0) return false;

            var task = _deletedTasks[^1]; // Get the last deleted task
            _deletedTasks.RemoveAt(_deletedTasks.Count - 1);
            _tasks.Add(task);

            Save();
            return true;
        }

        // Bulk operations
        public bool BulkDelete(List<Guid> ids)
        {
            bool anyDeleted = false;
            foreach (var id in ids)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    _tasks.Remove(task);
                    _deletedTasks.Add(task);
                    anyDeleted = true;
                }
            }

            if (anyDeleted) Save();
            return anyDeleted;
        }

        public bool BulkUpdateStatus(List<Guid> ids, TaskStatus status)
        {
            bool anyUpdated = false;
            foreach (var id in ids)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    task.Status = status;
                    anyUpdated = true;
                }
            }

            if (anyUpdated) Save();
            return anyUpdated;
        }

        public IEnumerable<string> GetAllProjects()
        {
            return _tasks.Select(t => t.Project)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Distinct()
                        .OrderBy(p => p);
        }

        public IEnumerable<string> GetAllTags()
        {
            return _tasks.SelectMany(t => t.Tags)
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .Distinct()
                        .OrderBy(t => t);
        }

        public Dictionary<string, int> GetTaskStatistics()
        {
            return new Dictionary<string, int>
            {
                { "Total", _tasks.Count },
                { "Pending", _tasks.Count(t => t.Status == TaskStatus.Pending) },
                { "In Progress", _tasks.Count(t => t.Status == TaskStatus.InProgress) },
                { "Completed", _tasks.Count(t => t.Status == TaskStatus.Done) },
                { "Overdue", _tasks.Count(t => t.IsOverdue) },
                { "Due Today", _tasks.Count(t => t.Status != TaskStatus.Done && t.DueDate.Date == DateTime.Today) },
                { "Due This Week", _tasks.Count(t => t.Status != TaskStatus.Done &&
                                                    t.DueDate.Date >= DateTime.Today &&
                                                    t.DueDate.Date <= DateTime.Today.AddDays(7)) },
                { "High Priority", _tasks.Count(t => t.Priority == Priority.High || t.Priority == Priority.Critical) }
            };
        }

        public void Save()
        {
            if (File.Exists(TasksFilePath)) File.Copy(TasksFilePath, BackupFilePath, true);
            var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(TasksFilePath, json);
        }

        public void Load()
        {
            if (!File.Exists(TasksFilePath)) return;
            try
            {
                var json = File.ReadAllText(TasksFilePath);
                _tasks = JsonSerializer.Deserialize<List<Task>>(json) ?? new List<Task>();

                foreach (var task in _tasks.ToList())
                {
                    if (task.Status == TaskStatus.Done && task.Recurrence != Recurrence.None)
                    {
                        var newTask = new Task
                        {
                            Title = task.Title,
                            Description = task.Description,
                            Project = task.Project,
                            Priority = task.Priority,
                            Tags = new List<string>(task.Tags),
                            Recurrence = task.Recurrence,
                            DueDate = GetNextDueDate(task.DueDate, task.Recurrence),
                            Status = TaskStatus.Pending
                        };
                        _tasks.Add(newTask);
                        task.Recurrence = Recurrence.None;
                    }
                }

                Save();
            }
            catch
            {
                _tasks = File.Exists(BackupFilePath)
                    ? JsonSerializer.Deserialize<List<Task>>(File.ReadAllText(BackupFilePath)) ?? new List<Task>()
                    : new List<Task>();
            }
        }

        private DateTime GetNextDueDate(DateTime current, Recurrence recurrence) => recurrence switch
        {
            Recurrence.Daily => current.AddDays(1),
            Recurrence.Weekly => current.AddDays(7),
            Recurrence.Monthly => current.AddMonths(1),
            Recurrence.Yearly => current.AddYears(1),
            Recurrence.Weekdays => GetNextWeekday(current),
            Recurrence.Weekends => GetNextWeekend(current),
            _ => current
        };

        private DateTime GetNextWeekday(DateTime date)
        {
            date = date.AddDays(1);
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }
            return date;
        }

        private DateTime GetNextWeekend(DateTime date)
        {
            date = date.AddDays(1);
            while (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }
            return date;
        }

        public void ExportToCsv()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "../../../tasks_export.csv");
            using var writer = new StreamWriter(path);
            writer.WriteLine("Id,Title,Description,DueDate,Status,Project,Priority,Tags,Recurrence");
            foreach (var t in _tasks)
            {
                var line = string.Join(",",
                    t.Id,
                    $"\"{t.Title.Replace("\"", "\"\"")}\"",
                    $"\"{t.Description.Replace("\"", "\"\"")}\"",
                    t.DueDate.ToString("yyyy-MM-dd"),
                    t.Status,
                    t.Project,
                    t.Priority,
                    $"\"{string.Join(";", t.Tags).Replace("\"", "\"\"")}\"",
                    t.Recurrence
                );
                writer.WriteLine(line);
            }
        }
    }

    public static class UIHelper
    {
        public static void PrintTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n" + new string('=', 40));
            Console.WriteLine(title);
            Console.WriteLine(new string('=', 40));
            Console.ResetColor();
        }

        public static void PrintMenu(Dictionary<string, string> options)
        {
            foreach (var opt in options)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"[{opt.Key}] ");
                Console.ResetColor();
                Console.WriteLine(opt.Value);
            }
        }

        public static ConsoleColor GetTaskColor(Task task)
        {
            return task.Status == TaskStatus.Done ? ConsoleColor.Green :
                   task.IsOverdue ? ConsoleColor.Red :
                   task.IsDueSoon ? ConsoleColor.Yellow :
                   task.Priority == Priority.Critical ? ConsoleColor.DarkRed :
                   task.Priority == Priority.High ? ConsoleColor.DarkYellow :
                   task.Status == TaskStatus.InProgress ? ConsoleColor.Cyan :
                   ConsoleColor.White;
        }

        public static void ShowTask(Task task)
        {
            Console.ForegroundColor = GetTaskColor(task);
            Console.WriteLine(new string('-', 40));
            Console.WriteLine($"ID: {task.Id}");
            Console.WriteLine($"Title: {task.Title}");

            if (!string.IsNullOrWhiteSpace(task.Description))
                Console.WriteLine($"Description: {task.Description}");

            Console.WriteLine($"Due Date: {task.DueDate:yyyy-MM-dd}");
            Console.WriteLine($"Status: {task.Status}");
            Console.WriteLine($"Project: {task.Project}");
            Console.WriteLine($"Priority: {task.Priority}");

            if (task.Tags.Count > 0)
                Console.WriteLine($"Tags: {string.Join(", ", task.Tags)}");

            Console.WriteLine($"Recurs: {task.Recurrence}");

            Console.WriteLine(new string('-', 40));
            Console.ResetColor();

            if (task.IsOverdue)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("OVERDUE!");
                Console.ResetColor();
            }
            else if (task.IsDueSoon)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Due soon!");
                Console.ResetColor();
            }
        }

        public static void ListTasks(IEnumerable<Task> tasks)
        {
            if (!tasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No tasks found.");
                Console.ResetColor();
                return;
            }

            foreach (var task in tasks)
            {
                Console.ForegroundColor = GetTaskColor(task);
                Console.WriteLine($"- {task.Title} [{task.Status}] ({task.DueDate:yyyy-MM-dd})");

                if (!string.IsNullOrWhiteSpace(task.Project))
                    Console.WriteLine($"  Project: {task.Project}");

                if (task.Tags.Count > 0)
                    Console.WriteLine($"  Tags: {string.Join(", ", task.Tags)}");

                if (task.Priority == Priority.High || task.Priority == Priority.Critical)
                    Console.WriteLine($"  Priority: {task.Priority}");

                if (task.Recurrence != Recurrence.None)
                    Console.WriteLine($"  Recurrence: {task.Recurrence}");

                Console.ResetColor();
            }
        }

        public static void DisplayStatistics(Dictionary<string, int> stats)
        {
            PrintTitle("Task Statistics");

            foreach (var stat in stats)
            {
                Console.Write($"{stat.Key}: ");
                Console.ForegroundColor = stat.Key.Contains("Overdue") ? ConsoleColor.Red :
                                         stat.Key.Contains("Completed") ? ConsoleColor.Green :
                                         stat.Key.Contains("High Priority") ? ConsoleColor.Yellow :
                                         ConsoleColor.White;
                Console.WriteLine(stat.Value);
                Console.ResetColor();
            }
        }

        public static string TruncateString(string str, int maxLength)
        {
            return str.Length <= maxLength ? str : str.Substring(0, maxLength - 3) + "...";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var taskManager = new TaskManager();
            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                UIHelper.PrintTitle("Todo List Menu");

                var menuOptions = new Dictionary<string, string>
                {
                    { "A", "Add Task" },
                    { "L", "List All Tasks" },
                    { "P", "List Tasks by Project" },
                    { "D", "List Tasks by Due Date" },
                    { "F", "Find Task (Search)" },
                    { "V", "View Task Details" },
                    { "E", "Edit Task" },
                    { "S", "Change Task Status" },
                    { "R", "Remove Task" },
                    { "B", "Bulk Operations" }, // New option
                    { "U", "Undo Delete" }, // New option
                    { "T", "Statistics" },
                    { "X", "Export Tasks to CSV" },
                    { "Q", "Quit" }
                };

                UIHelper.PrintMenu(menuOptions);
                Console.Write("Select an option: ");
                var input = Console.ReadLine()?.Trim().ToUpper();

                switch (input)
                {
                    case "A": AddTask(taskManager); break;
                    case "L": Show(taskManager.GetAll(), "All Tasks"); break;
                    case "P": Show(taskManager.GetSortedByProject(), "Tasks by Project"); break;
                    case "D": Show(taskManager.GetSortedByDate(), "Tasks by Due Date"); break;
                    case "F": Search(taskManager); break;
                    case "V": ViewTask(taskManager); break;
                    case "E": EditTask(taskManager); break;
                    case "S": ChangeStatus(taskManager); break;
                    case "R": DeleteTask(taskManager); break;
                    case "B": BulkOperations(taskManager); break; // New case
                    case "U": UndoDelete(taskManager); break; // New case
                    case "T": ShowStatistics(taskManager); break;
                    case "X": taskManager.ExportToCsv(); Console.WriteLine("\nExported. Press any key..."); Console.ReadKey(); break;
                    case "Q": exit = true; break;
                    default: Console.WriteLine("Invalid option."); Console.ReadKey(); break;
                }
            }
        }

        // New method for bulk operations
        static void BulkOperations(TaskManager manager)
        {
            UIHelper.PrintMenu(new Dictionary<string, string>
            {
                { "M", "Mark Multiple Tasks as Done" },
                { "P", "Mark Multiple Tasks as In Progress" },
                { "D", "Delete Multiple Tasks" }
            });

            Console.Write("Select an operation: ");
            var key = Console.ReadKey(true).Key;
            Console.WriteLine();

            if (key != ConsoleKey.M && key != ConsoleKey.P && key != ConsoleKey.D)
            {
                return;
            }

            Console.WriteLine("Enter task IDs separated by commas:");
            var idInput = Console.ReadLine() ?? string.Empty;
            var idStrings = idInput.Split(',').Select(s => s.Trim());

            var ids = new List<Guid>();
            foreach (var idStr in idStrings)
            {
                if (Guid.TryParse(idStr, out var id))
                {
                    ids.Add(id);
                }
                else
                {
                    Console.WriteLine($"Invalid ID format: {idStr}");
                }
            }

            if (ids.Count == 0)
            {
                Console.WriteLine("No valid IDs provided.");
                Console.ReadKey(true);
                return;
            }

            bool success = false;
            string operationName = "";

            switch (key)
            {
                case ConsoleKey.M:
                    success = manager.BulkUpdateStatus(ids, TaskStatus.Done);
                    operationName = "marked as done";
                    break;
                case ConsoleKey.P:
                    success = manager.BulkUpdateStatus(ids, TaskStatus.InProgress);
                    operationName = "marked as in progress";
                    break;
                case ConsoleKey.D:
                    Console.Write($"Are you sure you want to delete {ids.Count} tasks? (y/n): ");
                    if (Console.ReadKey().KeyChar.ToString().ToLower() == "y")
                    {
                        Console.WriteLine();
                        success = manager.BulkDelete(ids);
                        operationName = "deleted";
                    }
                    else
                    {
                        Console.WriteLine("\nOperation cancelled.");
                        Console.ReadKey(true);
                        return;
                    }
                    break;
            }

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✅ {ids.Count} tasks {operationName} successfully!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Failed to perform operation.");
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        // New method for undo delete
        static void UndoDelete(TaskManager manager)
        {
            var success = manager.UndoDelete();

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Last deleted task restored successfully!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No recently deleted tasks to restore.");
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ShowStatistics(TaskManager manager)
        {
            var stats = manager.GetTaskStatistics();
            UIHelper.DisplayStatistics(stats);

            // Also show projects breakdown
            Console.WriteLine("\n--- Projects Breakdown ---");
            var allProjects = manager.GetAllProjects().ToList();
            foreach (var project in allProjects)
            {
                var projectTasks = manager.GetAll().Where(t => t.Project == project).ToList();
                var completedCount = projectTasks.Count(t => t.Status == TaskStatus.Done);
                var pendingCount = projectTasks.Count(t => t.Status == TaskStatus.Pending);
                var inProgressCount = projectTasks.Count(t => t.Status == TaskStatus.InProgress);

                Console.WriteLine($"{project} ({projectTasks.Count} tasks):");
                Console.Write($"  Completed: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{completedCount}");
                Console.ResetColor();
                Console.Write($" | In Progress: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{inProgressCount}");
                Console.ResetColor();
                Console.Write($" | Pending: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{pendingCount}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void Show(IEnumerable<Task> tasks, string title)
        {
            UIHelper.PrintTitle(title);
            UIHelper.ListTasks(tasks);
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void AddTask(TaskManager manager)
        {
            UIHelper.PrintTitle("Add New Task");
            Console.Write("Title: ");
            var title = Console.ReadLine() ?? "Untitled";

            Console.Write("Description (optional): ");
            var description = Console.ReadLine() ?? "";

            Console.Write("Due Date (e.g. tomorrow, next friday, in 3 days): ");
            var due = NaturalDateParser.Parse(Console.ReadLine() ?? "");

            Console.Write("Project: ");
            var project = Console.ReadLine() ?? "General";

            Console.WriteLine("Priority: [1] Low, [2] Medium, [3] High, [4] Critical");
            var key = Console.ReadKey(true).KeyChar;
            var priority = key switch
            {
                '1' => Priority.Low,
                '3' => Priority.High,
                '4' => Priority.Critical,
                _ => Priority.Medium
            };

            Console.Write("Tags (comma separated, optional): ");
            var tagsInput = Console.ReadLine() ?? "";
            var tags = !string.IsNullOrWhiteSpace(tagsInput)
                ? tagsInput.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList()
                : new List<string>();

            Console.WriteLine("Recurrence: [0] None, [1] Daily, [2] Weekly, [3] Monthly, [4] Yearly, [5] Weekdays, [6] Weekends");
            var recurKey = Console.ReadKey(true).KeyChar;
            var recurrence = recurKey switch
            {
                '1' => Recurrence.Daily,
                '2' => Recurrence.Weekly,
                '3' => Recurrence.Monthly,
                '4' => Recurrence.Yearly,
                '5' => Recurrence.Weekdays,
                '6' => Recurrence.Weekends,
                _ => Recurrence.None
            };

            manager.Add(new Task
            {
                Title = title,
                Description = description,
                DueDate = due,
                Project = project,
                Priority = priority,
                Tags = tags,
                Recurrence = recurrence
            });

            Console.WriteLine("\nTask added. Press any key to continue...");
            Console.ReadKey();
        }

        static void Search(TaskManager manager)
        {
            UIHelper.PrintTitle("Search Tasks");
            Console.Write("Enter search term: ");
            var term = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(term))
                Show(manager.Search(term), $"Search Results for '{term}'");
            else
            {
                Console.WriteLine("Invalid input.");
                Console.ReadKey();
            }
        }

        static void ViewTask(TaskManager manager)
        {
            UIHelper.PrintTitle("View Task Details");
            Console.Write("Enter task ID: ");
            if (Guid.TryParse(Console.ReadLine(), out var id))
            {
                var task = manager.Find(id);
                if (task != null) UIHelper.ShowTask(task);
                else Console.WriteLine("Task not found.");
            }
            else Console.WriteLine("Invalid ID format.");
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void EditTask(TaskManager manager)
        {
            UIHelper.PrintTitle("Edit Task");
            Console.Write("Enter task ID: ");
            if (!Guid.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); Console.ReadKey(); return; }
            var task = manager.Find(id);
            if (task == null) { Console.WriteLine("Not found."); Console.ReadKey(); return; }
            Console.WriteLine("Leave fields blank to keep existing.");

            Console.Write($"Title [{task.Title}]: ");
            var t = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(t)) task.Title = t;

            Console.Write($"Description [{task.Description}]: ");
            var desc = Console.ReadLine();
            if (desc != null) task.Description = desc;

            Console.Write($"Due Date [{task.DueDate:yyyy-MM-dd}]: ");
            var d = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(d)) task.DueDate = NaturalDateParser.Parse(d);

            Console.Write($"Project [{task.Project}]: ");
            var p = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(p)) task.Project = p;

            Console.WriteLine($"Priority [{task.Priority}]: [1] Low, [2] Medium, [3] High, [4] Critical");
            var prioKey = Console.ReadKey(true).KeyChar;
            task.Priority = prioKey switch
            {
                '1' => Priority.Low,
                '3' => Priority.High,
                '4' => Priority.Critical,
                _ => task.Priority
            };

            Console.Write($"Tags [{string.Join(", ", task.Tags)}]: ");
            var tagsInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(tagsInput))
            {
                task.Tags = tagsInput.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            Console.WriteLine($"Recurrence [{task.Recurrence}]: [0] None, [1] Daily, [2] Weekly, [3] Monthly, [4] Yearly, [5] Weekdays, [6] Weekends");
            var recurKey = Console.ReadKey(true).KeyChar;
            task.Recurrence = recurKey switch
            {
                '1' => Recurrence.Daily,
                '2' => Recurrence.Weekly,
                '3' => Recurrence.Monthly,
                '4' => Recurrence.Yearly,
                '5' => Recurrence.Weekdays,
                '6' => Recurrence.Weekends,
                '0' => Recurrence.None,
                _ => task.Recurrence
            };

            manager.Update(task);
            Console.WriteLine("\nTask updated. Press any key to return...");
            Console.ReadKey();
        }

        static void ChangeStatus(TaskManager manager)
        {
            UIHelper.PrintTitle("Change Task Status");
            Console.Write("Enter task ID: ");
            if (!Guid.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); Console.ReadKey(); return; }
            var task = manager.Find(id);
            if (task == null) { Console.WriteLine("Not found."); Console.ReadKey(); return; }
            Console.WriteLine($"Current Status: {task.Status}");
            Console.WriteLine("[1] Pending, [2] In Progress, [3] Done");
            var statusKey = Console.ReadKey(true).KeyChar;
            task.Status = statusKey switch
            {
                '1' => TaskStatus.Pending,
                '2' => TaskStatus.InProgress,
                '3' => TaskStatus.Done,
                _ => task.Status
            };
            manager.Update(task);
            Console.WriteLine("\nStatus updated. Press any key to continue...");
            Console.ReadKey();
        }

        static void DeleteTask(TaskManager manager)
        {
            UIHelper.PrintTitle("Delete Task");
            Console.Write("Enter task ID: ");
            if (!Guid.TryParse(Console.ReadLine(), out var id)) { Console.WriteLine("Invalid ID."); Console.ReadKey(); return; }
            var task = manager.Find(id);
            if (task == null) { Console.WriteLine("Not found."); Console.ReadKey(); return; }
            UIHelper.ShowTask(task);
            Console.Write("Confirm delete? (y/n): ");
            if ((Console.ReadLine() ?? "n").Trim().ToLower() == "y")
            {
                var deleted = manager.Delete(id);
                if (deleted)
                    Console.WriteLine("Deleted. You can use 'U' option to undo if needed.");
                else
                    Console.WriteLine("Failed to delete task.");
            }
            else Console.WriteLine("Cancelled.");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}