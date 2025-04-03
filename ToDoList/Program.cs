using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

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

        // Quick checks for task state - used for color coding and filtering
        public bool IsOverdue => Status != TaskStatus.Done && DueDate < DateTime.Today;
        public bool IsDueSoon => Status != TaskStatus.Done && !IsOverdue && DueDate <= DateTime.Today.AddDays(2);
    }

    public static class NaturalDateParser
    {
        // Handles various date formats like "tomorrow", "next week", "in 3 days"
        // Allows users to input dates in a natural language style
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

        // Returns the date of the next occurrence of the specified day of week
        // If today is the target day, returns next week's occurrence
        private static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
        {
            DateTime date = DateTime.Today;
            int daysToAdd = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;
            if (daysToAdd == 0) daysToAdd = 7; // If today is the target day, get next week
            return date.AddDays(daysToAdd);
        }

        // Returns the date for the next Sunday (end of week)
        private static DateTime GetEndOfWeek()
        {
            DateTime date = DateTime.Today;
            int daysToAdd = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
            return date.AddDays(daysToAdd);
        }

        // Returns the last day of the current month
        private static DateTime GetEndOfMonth()
        {
            DateTime date = DateTime.Today;
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }

        // Handles "next X" format where X is a day or time period
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

        // Handles "in X days/weeks/months/years" format
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

        // Fallback to standard date parsing with several format attempts
        // First tries exact formats, then falls back to culture-specific parsing
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
        private List<Task> _deletedTasks = new(); // For tracking all deleted tasks
        private List<List<Task>> _deletionBatches = new(); // For tracking batches of deletions
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

        // Delete a task and store it for possible undo operations
        public bool Delete(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;

            _tasks.Remove(task);
            _deletedTasks.Add(task); // Store for undo

            Save();
            return true;
        }

        // Restore the most recently deleted task
        public bool UndoDelete()
        {
            if (_deletedTasks.Count == 0) return false;

            var task = _deletedTasks[^1]; // Get the last deleted task
            _deletedTasks.RemoveAt(_deletedTasks.Count - 1);
            _tasks.Add(task);

            Save();
            return true;
        }

        // Delete multiple tasks at once and store them for undo
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

        // Update the status of multiple tasks at once
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

        // Get a distinct list of all projects for filtering
        public IEnumerable<string> GetAllProjects()
        {
            return _tasks.Select(t => t.Project)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Distinct()
                        .OrderBy(p => p);
        }

        // Get a distinct list of all tags for filtering
        public IEnumerable<string> GetAllTags()
        {
            return _tasks.SelectMany(t => t.Tags)
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .Distinct()
                        .OrderBy(t => t);
        }

        // Generate statistics about the task list for dashboard display
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

        // Save tasks to a JSON file with backup creation
        public void Save()
        {
            if (File.Exists(TasksFilePath)) File.Copy(TasksFilePath, BackupFilePath, true);
            var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(TasksFilePath, json);
        }

        // Load tasks from JSON and handle recurring tasks
        // If a recurring task is completed, create a new pending instance
        public void Load()
        {
            if (!File.Exists(TasksFilePath)) return;
            try
            {
                var json = File.ReadAllText(TasksFilePath);
                _tasks = JsonSerializer.Deserialize<List<Task>>(json) ?? new List<Task>();

                // Process recurring tasks - if a recurring task is completed,
                // create a new instance for the next occurrence and disable recurrence on completed task
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
                // If the main file is corrupted, try loading from backup
                _tasks = File.Exists(BackupFilePath)
                    ? JsonSerializer.Deserialize<List<Task>>(File.ReadAllText(BackupFilePath)) ?? new List<Task>()
                    : new List<Task>();
            }
        }

        // Calculate the next due date for recurring tasks based on recurrence pattern
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

        // Find the next weekday (Monday-Friday) from the given date
        private DateTime GetNextWeekday(DateTime date)
        {
            date = date.AddDays(1);
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }
            return date;
        }

        // Find the next weekend day (Saturday-Sunday) from the given date
        private DateTime GetNextWeekend(DateTime date)
        {
            date = date.AddDays(1);
            while (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }
            return date;
        }

        // Export the task list to a CSV file for external processing
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
        // Format and display a title section with a border
        public static void PrintTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"  {title}");
            Console.WriteLine(new string('=', 60));
            Console.ResetColor();
        }

        // Display menu options with highlighted keys
        public static void PrintMenu(Dictionary<string, string> options)
        {
            foreach (var opt in options)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"[{opt.Key}] ");
                Console.ResetColor();
                Console.WriteLine(opt.Value);
            }
        }

        // Determine the appropriate color for a task based on its status and priority
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

        // Display detailed information about a single task
        public static void ShowTask(Task task)
        {
            Console.ForegroundColor = GetTaskColor(task);
            Console.WriteLine(new string('-', 60));
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

            Console.WriteLine(new string('-', 60));
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

        // Display a paginated list of tasks with color coding
        public static void ListTasks(IEnumerable<Task> tasks)
        {
            if (!tasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No tasks found.");
                Console.ResetColor();
                return;
            }

            // Display table header
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"{"#",-5} | {"Title",-25} | {"Due Date",-10} | {"Project",-15} | {"Priority",-8} | {"Status",-10}");
            Console.WriteLine(new string('-', 85)); // Shortened line length for better readability
            Console.ResetColor();

            int counter = 0;
            int index = 1; // Use a simple index for display
            foreach (var task in tasks)
            {
                Console.ForegroundColor = GetTaskColor(task);
                Console.WriteLine($"{index,-5} | {TruncateString(task.Title, 25),-25} | {task.DueDate:yyyy-MM-dd} | {TruncateString(task.Project, 15),-15} | {task.Priority,-8} | {task.Status,-10}");
                Console.ResetColor();

                counter++;
                index++;

                // Pagination: show 20 tasks at a time
                if (counter >= 20 && tasks.Count() > 20)
                {
                    Console.WriteLine("Press any key to see more tasks or ESC to return...");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) break;
                    counter = 0;
                    Console.Clear();

                    // Re-print header
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"{"#",-5} | {"Title",-25} | {"Due Date",-10} | {"Project",-15} | {"Priority",-8} | {"Status",-10}");
                    Console.WriteLine(new string('-', 85));
                    Console.ResetColor();
                }
            }
        }

        // Display task statistics with color coding
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

        // Truncate string with ellipsis for better display in limited space
        public static string TruncateString(string str, int maxLength)
        {
            return str.Length <= maxLength ? str : str.Substring(0, maxLength - 3) + "...";
        }

        // Display an animated spinner to indicate activity
        public static void ShowLoadingAnimation(string message, int duration)
        {
            Console.Write(message);
            string[] animationFrames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            int frameDelay = 100;

            int totalFrames = duration / frameDelay;
            for (int i = 0; i < totalFrames; i++)
            {
                Console.Write(animationFrames[i % animationFrames.Length]);
                Thread.Sleep(frameDelay);
                Console.Write("\b");
            }

            Console.WriteLine("Done!");
        }
    }

    class Program
    {
        static TaskManager taskManager = new();

        // Track current sort settings for consistent UI experience
        static string currentSortField = "date";
        static bool sortAscending = true;

        static void Main(string[] args)
        {
            DisplayWelcomeScreen();

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                UIHelper.PrintTitle("Todo List Menu");

                var menuOptions = new Dictionary<string, string>
                {
                    { "A", "Add Task" },
                    { "L", "List Tasks" },
                    { "V", "View Task Details" },
                    { "E", "Edit Task" },
                    { "P", "Change Task Status" },
                    { "R", "Remove Task" },
                    { "B", "Bulk Operations" },
                    { "S", "Statistics" },
                    { "U", "Undo Delete" },
                    { "X", "Export Tasks to CSV" },
                    { "Q", "Quit" }
                };

                UIHelper.PrintMenu(menuOptions);
                Console.Write("Select an option: ");
                var key = Console.ReadKey(true).Key;
                Console.Clear();

                try
                {
                    switch (key)
                    {
                        case ConsoleKey.A: AddTask(); break;
                        case ConsoleKey.L: ListTasks(); break;
                        case ConsoleKey.V: ViewTaskDetails(); break;
                        case ConsoleKey.E: EditTask(); break;
                        case ConsoleKey.P: ChangeStatus(); break;
                        case ConsoleKey.R: DeleteTask(); break;
                        case ConsoleKey.B: BulkOperations(); break;
                        case ConsoleKey.S: ShowStatistics(); break;
                        case ConsoleKey.U: UndoDelete(); break;
                        case ConsoleKey.X: ExportTasks(); break;
                        case ConsoleKey.Q: exit = true; Console.WriteLine("Goodbye!"); break;
                        default: Console.WriteLine("Invalid option. Press any key to continue..."); Console.ReadKey(true); break;
                    }
                }
                catch (Exception ex)
                {
                    // Global exception handler to prevent crashes
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.ResetColor();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
            }
        }

        // Display welcome screen with ASCII art and task statistics
        static void DisplayWelcomeScreen()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;

            string[] logo = {
                @"  _______          _         _      _     _   ",
                @" |__   __|        | |       | |    (_)   | |  ",
                @"    | | ___   ___ | | ___   | |     _ ___| |_ ",
                @"    | |/ _ \ / _ \| |/ _ \  | |    | / __| __|",
                @"    | | (_) | (_) | | (_) | | |____| \__ \ |_ ",
                @"    |_|\___/ \___/|_|\___/  |______|_|___/\__|"
            };

            foreach (var line in logo)
            {
                Console.WriteLine(line);
            }

            Console.ResetColor();

            // Get statistics for the welcome screen
            var stats = taskManager.GetTaskStatistics();

            Console.WriteLine("\nWelcome to your enhanced Todo List!");
            Console.WriteLine($"You have {stats["Pending"]} pending tasks and {stats["Completed"]} completed tasks.");

            if (stats["Overdue"] > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"⚠️  You have {stats["Overdue"]} overdue tasks!");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
        }

        // Helper to select a single task from a list with numbering
        // Returns the selected task or null if cancelled
        static Task? SelectTask(string title = "Select a Task", IEnumerable<Task>? taskList = null)
        {
            var tasks = taskList?.ToList() ?? taskManager.GetAll().ToList();
            if (!tasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No tasks found.");
                Console.ResetColor();
                Console.ReadKey(true);
                return null;
            }

            UIHelper.PrintTitle(title);

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                Console.ForegroundColor = UIHelper.GetTaskColor(task);
                Console.WriteLine($"[{i + 1}] {task.Title} - Due: {task.DueDate:yyyy-MM-dd} ({task.Status})");
                Console.ResetColor();
            }

            Console.Write("\nEnter task number (or 0 to cancel): ");
            if (int.TryParse(Console.ReadLine(), out var selection) && selection > 0 && selection <= tasks.Count)
            {
                return tasks[selection - 1];
            }

            Console.WriteLine("Operation cancelled or invalid selection.");
            Console.ReadKey(true);
            return null;
        }

        // Helper to select multiple tasks from a list
        // Returns a list of selected tasks (empty if cancelled)
        static List<Task> SelectMultipleTasks(string title = "Select Multiple Tasks", IEnumerable<Task>? taskList = null)
        {
            var tasks = taskList?.ToList() ?? taskManager.GetAll().ToList();
            if (!tasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No tasks found.");
                Console.ResetColor();
                Console.ReadKey(true);
                return new List<Task>();
            }

            UIHelper.PrintTitle(title);

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                Console.ForegroundColor = UIHelper.GetTaskColor(task);
                Console.WriteLine($"[{i + 1}] {task.Title} - Due: {task.DueDate:yyyy-MM-dd} ({task.Status})");
                Console.ResetColor();
            }

            Console.WriteLine("\nEnter task numbers separated by commas (or 0 to cancel):");
            var input = Console.ReadLine() ?? string.Empty;

            if (input == "0") return new List<Task>();

            var selectedTasks = new List<Task>();
            var selections = input.Split(',').Select(s => s.Trim());

            foreach (var sel in selections)
            {
                if (int.TryParse(sel, out var index) && index > 0 && index <= tasks.Count)
                {
                    selectedTasks.Add(tasks[index - 1]);
                }
            }

            return selectedTasks;
        }

        // Complex task listing with filtering, sorting, and pagination
        static void ListTasks()
        {
            // Show list options
            UIHelper.PrintMenu(new Dictionary<string, string>
            {
                { "A", "All Tasks" },
                { "P", "Pending Tasks" },
                { "I", "In Progress Tasks" },
                { "C", "Completed Tasks" },
                { "O", "Overdue Tasks" },
                { "T", "Tasks Due Today" },
                { "W", "Tasks Due This Week" },
                { "S", "Search Tasks" },
                { "F", "Filter by Project" },
                { "G", "Filter by Tag" }
            });

            Console.Write("Select an option: ");
            var key = Console.ReadKey(true).Key;
            Console.WriteLine();

            // Variables for filtering
            TaskStatus? status = null;
            string? keyword = null;
            DateTime? dueAfter = null;
            DateTime? dueBefore = null;
            string? project = null;
            string? tag = null;

            // Determine filtering based on selection
            switch (key)
            {
                case ConsoleKey.A: // All tasks - no filter
                    break;
                case ConsoleKey.P:
                    status = TaskStatus.Pending;
                    break;
                case ConsoleKey.I:
                    status = TaskStatus.InProgress;
                    break;
                case ConsoleKey.C:
                    status = TaskStatus.Done;
                    break;
                case ConsoleKey.O:
                    status = TaskStatus.Pending;
                    dueBefore = DateTime.Today;
                    break;
                case ConsoleKey.T:
                    dueAfter = DateTime.Today;
                    dueBefore = DateTime.Today;
                    break;
                case ConsoleKey.W:
                    dueAfter = DateTime.Today;
                    dueBefore = DateTime.Today.AddDays(7);
                    break;
                case ConsoleKey.S:
                    Console.Write("Enter search term: ");
                    keyword = Console.ReadLine();
                    break;
                case ConsoleKey.F:
                    // Show available projects for selection
                    var projects = taskManager.GetAllProjects().ToList();
                    if (projects.Count == 0)
                    {
                        Console.WriteLine("No projects found.");
                        Console.ReadKey(true);
                        return;
                    }

                    for (int i = 0; i < projects.Count; i++)
                    {
                        Console.WriteLine($"[{i + 1}] {projects[i]}");
                    }

                    Console.Write("Enter project number (or 0 to cancel): ");
                    if (int.TryParse(Console.ReadLine(), out int projectIndex) &&
                        projectIndex > 0 && projectIndex <= projects.Count)
                    {
                        project = projects[projectIndex - 1];
                    }
                    else if (projectIndex != 0)
                    {
                        Console.WriteLine("Invalid selection.");
                        Console.ReadKey(true);
                        return;
                    }
                    else
                    {
                        return;
                    }
                    break;
                case ConsoleKey.G:
                    // Show available tags for selection
                    var tags = taskManager.GetAllTags().ToList();
                    if (tags.Count == 0)
                    {
                        Console.WriteLine("No tags found.");
                        Console.ReadKey(true);
                        return;
                    }

                    for (int i = 0; i < tags.Count; i++)
                    {
                        Console.WriteLine($"[{i + 1}] {tags[i]}");
                    }

                    Console.Write("Enter tag number (or 0 to cancel): ");
                    if (int.TryParse(Console.ReadLine(), out int tagIndex) &&
                        tagIndex > 0 && tagIndex <= tags.Count)
                    {
                        tag = tags[tagIndex - 1];
                    }
                    else if (tagIndex != 0)
                    {
                        Console.WriteLine("Invalid selection.");
                        Console.ReadKey(true);
                        return;
                    }
                    else
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }

            // Apply all filters using LINQ, handling nulls safely
            var filteredTasks = taskManager.GetAll().Where(t =>
                (!status.HasValue || t.Status == status.Value) &&
                (string.IsNullOrWhiteSpace(keyword) ||
                t.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                t.Project.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase))) &&
                (!dueBefore.HasValue || t.DueDate <= dueBefore.Value) &&
                (!dueAfter.HasValue || t.DueDate >= dueAfter.Value) &&
                (string.IsNullOrWhiteSpace(project) || t.Project.Equals(project, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(tag) || t.Tags.Any(tg => tg.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            );

            // Apply sort based on current sort field
            filteredTasks = currentSortField switch
            {
                "date" => sortAscending ? filteredTasks.OrderBy(t => t.DueDate) : filteredTasks.OrderByDescending(t => t.DueDate),
                "project" => sortAscending ? filteredTasks.OrderBy(t => t.Project) : filteredTasks.OrderByDescending(t => t.Project),
                "priority" => sortAscending ? filteredTasks.OrderBy(t => t.Priority) : filteredTasks.OrderByDescending(t => t.Priority),
                "title" => sortAscending ? filteredTasks.OrderBy(t => t.Title) : filteredTasks.OrderByDescending(t => t.Title),
                "status" => sortAscending ? filteredTasks.OrderBy(t => t.Status) : filteredTasks.OrderByDescending(t => t.Status),
                _ => filteredTasks.OrderBy(t => t.DueDate)
            };

            Console.Clear();

            // Print header with filter info
            string filterInfo = key switch
            {
                ConsoleKey.A => "All Tasks",
                ConsoleKey.P => "Pending Tasks",
                ConsoleKey.I => "In Progress Tasks",
                ConsoleKey.C => "Completed Tasks",
                ConsoleKey.O => "Overdue Tasks",
                ConsoleKey.T => "Tasks Due Today",
                ConsoleKey.W => "Tasks Due This Week",
                ConsoleKey.S => $"Search Results for '{keyword}'",
                ConsoleKey.F => $"Tasks in Project '{project}'",
                ConsoleKey.G => $"Tasks with Tag '{tag}'",
                _ => "Tasks"
            };

            UIHelper.PrintTitle(filterInfo);

            // Show sort options
            Console.WriteLine("Sort by: [D]ate | [P]roject | [R]iority | [S]tatus | [T]itle | [↑/↓] Direction");
            Console.WriteLine($"Current sort: {currentSortField} ({(sortAscending ? "ascending" : "descending")})");
            Console.WriteLine();

            UIHelper.ListTasks(filteredTasks);

            Console.WriteLine("\nSort: [D]ate | [P]roject | [R]iority | [S]tatus | [T]itle | [↑/↓] Direction | Any other key to return");
            var sortKey = Console.ReadKey(true).Key;

            // Update sort field based on key press
            switch (sortKey)
            {
                case ConsoleKey.D:
                    currentSortField = "date";
                    break;
                case ConsoleKey.P:
                    currentSortField = "project";
                    break;
                case ConsoleKey.R:
                    currentSortField = "priority";
                    break;
                case ConsoleKey.S:
                    currentSortField = "status";
                    break;
                case ConsoleKey.T:
                    currentSortField = "title";
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    sortAscending = !sortAscending;
                    // Recurse to show the same list with new sort direction
                    ListTasks();
                    return;
                default:
                    return;
            }

            // Recurse to show the same list with new sort field
            if (sortKey is ConsoleKey.D or ConsoleKey.P or ConsoleKey.R or ConsoleKey.S or ConsoleKey.T)
            {
                ListTasks();
            }
        }

        static void ViewTaskDetails()
        {
            // Use the selection helper to choose a task
            var task = SelectTask("View Task Details");
            if (task == null) return;

            UIHelper.ShowTask(task);
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
        }

        static void AddTask()
        {
            UIHelper.PrintTitle("Add New Task");

            Console.Write("Title: ");
            var title = Console.ReadLine() ?? string.Empty;

            Console.Write("Description (optional): ");
            var description = Console.ReadLine() ?? string.Empty;

            Console.Write("Due Date (today/tomorrow/next week or YYYY-MM-DD): ");
            var due = NaturalDateParser.Parse(Console.ReadLine() ?? "");

            Console.Write("Project: ");
            var project = Console.ReadLine() ?? string.Empty;

            Console.WriteLine("Priority:");
            Console.WriteLine("[1] Low | [2] Medium | [3] High | [4] Critical");
            var priority = Console.ReadKey(true).KeyChar switch
            {
                '1' => Priority.Low,
                '3' => Priority.High,
                '4' => Priority.Critical,
                _ => Priority.Medium // Default
            };
            Console.WriteLine($"Selected Priority: {priority}");

            Console.Write("Tags (comma separated, optional): ");
            var tagsInput = Console.ReadLine() ?? string.Empty;
            var tags = !string.IsNullOrWhiteSpace(tagsInput)
                ? tagsInput.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList()
                : new List<string>();

            Console.Write("Is this a recurring task? (y/n): ");
            var isRecurring = Console.ReadKey().KeyChar.ToString().ToLower() == "y";
            Console.WriteLine();

            Recurrence recurrence = Recurrence.None;
            if (isRecurring)
            {
                Console.WriteLine("Recurrence Pattern:");
                Console.WriteLine("[D] Daily | [W] Weekly | [M] Monthly | [Y] Yearly | [K] Weekdays | [E] Weekends");
                recurrence = Console.ReadKey(true).KeyChar.ToString().ToUpper() switch
                {
                    "D" => Recurrence.Daily,
                    "W" => Recurrence.Weekly,
                    "M" => Recurrence.Monthly,
                    "Y" => Recurrence.Yearly,
                    "K" => Recurrence.Weekdays,
                    "E" => Recurrence.Weekends,
                    _ => Recurrence.Daily // Default
                };
                Console.WriteLine($"Selected Recurrence: {recurrence}");
            }

            taskManager.Add(new Task
            {
                Title = title,
                Description = description,
                DueDate = due,
                Project = project,
                Priority = priority,
                Tags = tags,
                Recurrence = recurrence
            });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ Task added successfully!");
            Console.ResetColor();

            UIHelper.ShowLoadingAnimation("Saving... ", 500);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void EditTask()
        {
            // Use the selection helper to choose a task to edit
            var task = SelectTask("Edit Task");
            if (task == null) return;

            Console.WriteLine("Current task details:");
            UIHelper.ShowTask(task);
            Console.WriteLine("Leave fields blank to keep current values.");

            // Collect new values, keeping old ones if input is empty
            Console.Write($"New Title [{task.Title}]: ");
            var title = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(title)) task.Title = title;

            Console.Write($"New Description [{task.Description}]: ");
            var description = Console.ReadLine();
            if (description != null) task.Description = description;

            Console.Write($"New Due Date [{task.DueDate:yyyy-MM-dd}]: ");
            var dateInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(dateInput)) task.DueDate = NaturalDateParser.Parse(dateInput);

            Console.Write($"New Project [{task.Project}]: ");
            var project = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(project)) task.Project = project;

            Console.WriteLine($"New Priority [{task.Priority}]:");
            Console.WriteLine("[1] Low | [2] Medium | [3] High | [4] Critical | [Enter] Keep current");
            var key = Console.ReadKey(true).KeyChar;
            switch (key)
            {
                case '1': task.Priority = Priority.Low; break;
                case '2': task.Priority = Priority.Medium; break;
                case '3': task.Priority = Priority.High; break;
                case '4': task.Priority = Priority.Critical; break;
            }

            Console.Write($"New Tags [{string.Join(", ", task.Tags)}]: ");
            var tagsInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(tagsInput))
            {
                task.Tags = tagsInput.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            Console.Write($"Is this a recurring task? (y/n/[Enter] keep current [{(task.Recurrence != Recurrence.None ? "y" : "n")}]): ");
            var recurKey = Console.ReadKey();
            Console.WriteLine();

            if (recurKey.KeyChar.ToString().ToLower() == "y" ||
                (recurKey.Key == ConsoleKey.Enter && task.Recurrence != Recurrence.None))
            {
                Console.WriteLine("Recurrence Pattern:");
                Console.WriteLine("[D] Daily | [W] Weekly | [M] Monthly | [Y] Yearly | [K] Weekdays | [E] Weekends");
                task.Recurrence = Console.ReadKey(true).KeyChar.ToString().ToUpper() switch
                {
                    "D" => Recurrence.Daily,
                    "W" => Recurrence.Weekly,
                    "M" => Recurrence.Monthly,
                    "Y" => Recurrence.Yearly,
                    "K" => Recurrence.Weekdays,
                    "E" => Recurrence.Weekends,
                    _ => task.Recurrence // Keep current if invalid input
                };
                Console.WriteLine($"Selected Recurrence: {task.Recurrence}");
            }
            else if (recurKey.KeyChar.ToString().ToLower() == "n")
            {
                task.Recurrence = Recurrence.None;
            }

            taskManager.Update(task);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ Task updated successfully!");
            Console.ResetColor();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ChangeStatus()
        {
            // Use the selection helper to choose a task to change status
            var task = SelectTask("Change Task Status");
            if (task == null) return;

            Console.WriteLine($"Current Status: {task.Status}");
            Console.WriteLine("[P] Pending | [I] In Progress | [D] Done");
            Console.Write("New Status: ");

            var key = Console.ReadKey(true).Key;
            TaskStatus newStatus = key switch
            {
                ConsoleKey.P => TaskStatus.Pending,
                ConsoleKey.I => TaskStatus.InProgress,
                ConsoleKey.D => TaskStatus.Done,
                _ => task.Status
            };

            if (newStatus == task.Status)
            {
                Console.WriteLine("Status unchanged.");
                Console.ReadKey(true);
                return;
            }

            task.Status = newStatus;
            taskManager.Update(task);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ Task status updated to {newStatus}!");
            Console.ResetColor();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void DeleteTask()
        {
            // Use the selection helper to choose a task to delete
            var task = SelectTask("Remove Task");
            if (task == null) return;

            Console.WriteLine("Task to be removed:");
            UIHelper.ShowTask(task);

            Console.Write("Are you sure you want to remove this task? (y/n): ");
            if (Console.ReadKey().KeyChar.ToString().ToLower() != "y")
            {
                Console.WriteLine("\nTask removal cancelled.");
                Console.ReadKey(true);
                return;
            }

            Console.WriteLine();
            var removed = taskManager.Delete(task.Id);

            if (removed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✅ Task removed successfully!");
                Console.ResetColor();
                Console.WriteLine("You can use the 'U' option to undo this deletion if needed.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Failed to remove task.");
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        // Handle operations on multiple tasks at once (mark as done, in progress, or delete)
        static void BulkOperations()
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

            // Get operation description for task selection prompt
            string operationTitle = key switch
            {
                ConsoleKey.M => "Mark Tasks as Done",
                ConsoleKey.P => "Mark Tasks as In Progress",
                ConsoleKey.D => "Delete Tasks",
                _ => "Select Tasks"
            };

            // Use the multiple selection helper
            var selectedTasks = SelectMultipleTasks(operationTitle);

            if (selectedTasks.Count == 0)
            {
                Console.WriteLine("No tasks selected or operation cancelled.");
                Console.ReadKey(true);
                return;
            }

            // Convert tasks to IDs for the bulk operations
            var ids = selectedTasks.Select(t => t.Id).ToList();

            bool success = false;
            string operationName = "";

            switch (key)
            {
                case ConsoleKey.M:
                    success = taskManager.BulkUpdateStatus(ids, TaskStatus.Done);
                    operationName = "marked as done";
                    break;
                case ConsoleKey.P:
                    success = taskManager.BulkUpdateStatus(ids, TaskStatus.InProgress);
                    operationName = "marked as in progress";
                    break;
                case ConsoleKey.D:
                    Console.Write($"Are you sure you want to delete {ids.Count} tasks? (y/n): ");
                    if (Console.ReadKey().KeyChar.ToString().ToLower() == "y")
                    {
                        Console.WriteLine();
                        success = taskManager.BulkDelete(ids);
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

        // Display task statistics with project breakdowns
        static void ShowStatistics()
        {
            var stats = taskManager.GetTaskStatistics();
            UIHelper.DisplayStatistics(stats);

            // Project breakdown - show completion stats for each project
            Console.WriteLine("\n--- Projects Breakdown ---");
            var allProjects = taskManager.GetAllProjects().ToList();
            foreach (var project in allProjects)
            {
                var projectTasks = taskManager.GetAll().Where(t => t.Project == project).ToList();
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

        // Restore the most recently deleted task
        static void UndoDelete()
        {
            UIHelper.PrintTitle("Undo Delete");

            var success = taskManager.UndoDelete();

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Last deletion operation undone successfully!");
                Console.ResetColor();
                Console.WriteLine("All tasks from the last deletion have been restored.");
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

        // Export task data to CSV for use in other applications
        static void ExportTasks()
        {
            UIHelper.PrintTitle("Export Tasks to CSV");

            UIHelper.ShowLoadingAnimation("Exporting tasks to CSV... ", 1000);

            taskManager.ExportToCsv();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ Tasks exported successfully to 'tasks_export.csv' in the application directory!");
            Console.ResetColor();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}