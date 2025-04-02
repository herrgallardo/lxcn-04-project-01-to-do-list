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

    public class Task
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public string Project { get; set; } = string.Empty;
        public Priority Priority { get; set; } = Priority.Medium;
    }

    public static class NaturalDateParser
    {
        // Parses human-friendly date expressions
        public static DateTime Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return DateTime.Today;

            input = input.ToLowerInvariant().Trim();

            return input switch
            {
                "today" => DateTime.Today,
                "tomorrow" => DateTime.Today.AddDays(1),
                "next week" => DateTime.Today.AddDays(7),
                _ => TryParseStandardDate(input)
            };
        }

        private static DateTime TryParseStandardDate(string input)
        {
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;

            return DateTime.Today;
        }
    }

    public class TaskManager
    {
        private List<Task> _tasks = new();

        private static readonly string TasksFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks.json");
        private static readonly string BackupFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks_backup.json");

        public TaskManager() => Load();

        public void Add(Task task)
        {
            _tasks.Add(task);
            Save();
        }

        public IEnumerable<Task> GetAll() => _tasks;

        public Task? Find(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

        public void Update(Task task)
        {
            var index = _tasks.FindIndex(t => t.Id == task.Id);
            if (index >= 0)
            {
                _tasks[index] = task;
                Save();
            }
        }

        public void Delete(Guid id)
        {
            _tasks = _tasks.Where(t => t.Id != id).ToList();
            Save();
        }

        public void Save()
        {
            if (File.Exists(TasksFilePath))
            {
                try
                {
                    File.Copy(TasksFilePath, BackupFilePath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to create backup: {ex.Message}");
                }
            }

            try
            {
                var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TasksFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving tasks: {ex.Message}");
            }
        }

        public void Load()
        {
            if (!File.Exists(TasksFilePath)) return;

            try
            {
                var json = File.ReadAllText(TasksFilePath);
                _tasks = JsonSerializer.Deserialize<List<Task>>(json) ?? new List<Task>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tasks: {ex.Message}");

                if (File.Exists(BackupFilePath))
                {
                    try
                    {
                        Console.WriteLine("Attempting to recover from backup...");
                        var backupJson = File.ReadAllText(BackupFilePath);
                        _tasks = JsonSerializer.Deserialize<List<Task>>(backupJson) ?? new List<Task>();
                        Console.WriteLine("Recovered from backup.");
                    }
                    catch (Exception backupEx)
                    {
                        Console.WriteLine($"Error recovering from backup: {backupEx.Message}");
                        _tasks = new List<Task>();
                    }
                }
                else
                {
                    _tasks = new List<Task>();
                }
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

        public static void ShowTask(Task task)
        {
            Console.WriteLine($"\nID: {task.Id}");
            Console.WriteLine($"Title: {task.Title}");
            Console.WriteLine($"Due: {task.DueDate:yyyy-MM-dd}");
            Console.WriteLine($"Status: {task.Status}");
            Console.WriteLine($"Project: {task.Project}");
            Console.WriteLine($"Priority: {task.Priority}");
        }

        public static void ListTasks(IEnumerable<Task> tasks)
        {
            foreach (var task in tasks)
            {
                Console.ForegroundColor = task.Status == TaskStatus.Done ? ConsoleColor.Green : ConsoleColor.White;
                Console.WriteLine($"- {task.Title} [{task.Status}] ({task.DueDate:yyyy-MM-dd})");
                Console.ResetColor();
            }
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
                    { "L", "List Tasks" },
                    { "V", "View Task Details" },
                    { "E", "Edit Task" },
                    { "S", "Change Task Status" },
                    { "D", "Delete Task" },
                    { "Q", "Quit" }
                };

                UIHelper.PrintMenu(menuOptions);

                Console.Write("Select an option: ");
                var input = Console.ReadLine()?.Trim().ToUpper();

                switch (input)
                {
                    case "A": AddTask(taskManager); break;
                    case "L":
                        UIHelper.PrintTitle("All Tasks");
                        UIHelper.ListTasks(taskManager.GetAll());
                        Console.WriteLine("\nPress any key to return to menu...");
                        Console.ReadKey(); break;
                    case "V": ViewTask(taskManager); break;
                    case "E": EditTask(taskManager); break;
                    case "S": ChangeStatus(taskManager); break;
                    case "D": DeleteTask(taskManager); break;
                    case "Q": exit = true; break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void AddTask(TaskManager manager)
        {
            UIHelper.PrintTitle("Add New Task");

            Console.Write("Title: ");
            var title = Console.ReadLine() ?? "Untitled";

            Console.Write("Due Date (e.g. tomorrow, 2024-12-01): ");
            var dateInput = Console.ReadLine() ?? "";
            var due = NaturalDateParser.Parse(dateInput);

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

            var task = new Task
            {
                Title = title,
                DueDate = due,
                Project = project,
                Priority = priority
            };

            manager.Add(task);

            Console.WriteLine("\nTask added. Press any key to continue...");
            Console.ReadKey();
        }

        static void ViewTask(TaskManager manager)
        {
            UIHelper.PrintTitle("View Task Details");
            Console.Write("Enter task ID: ");
            var idInput = Console.ReadLine();

            if (Guid.TryParse(idInput, out var id))
            {
                var task = manager.Find(id);
                if (task != null)
                {
                    UIHelper.ShowTask(task);
                }
                else
                {
                    Console.WriteLine("Task not found.");
                }
            }
            else
            {
                Console.WriteLine("Invalid ID format.");
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void EditTask(TaskManager manager)
        {
            UIHelper.PrintTitle("Edit Task");
            Console.Write("Enter task ID: ");
            var idInput = Console.ReadLine();

            if (!Guid.TryParse(idInput, out var id))
            {
                Console.WriteLine("Invalid ID format.");
                Console.ReadKey();
                return;
            }

            var task = manager.Find(id);
            if (task == null)
            {
                Console.WriteLine("Task not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Leave fields blank to keep existing values.");
            Console.Write($"Title [{task.Title}]: ");
            var newTitle = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newTitle)) task.Title = newTitle;

            Console.Write($"Due Date [{task.DueDate:yyyy-MM-dd}]: ");
            var dueInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(dueInput)) task.DueDate = NaturalDateParser.Parse(dueInput);

            Console.Write($"Project [{task.Project}]: ");
            var newProject = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newProject)) task.Project = newProject;

            Console.WriteLine($"Priority [{task.Priority}]: [1] Low, [2] Medium, [3] High, [4] Critical");
            var priorityKey = Console.ReadKey(true).KeyChar;
            task.Priority = priorityKey switch
            {
                '1' => Priority.Low,
                '3' => Priority.High,
                '4' => Priority.Critical,
                _ => task.Priority
            };

            manager.Update(task);
            Console.WriteLine("\nTask updated. Press any key to return...");
            Console.ReadKey();
        }

        // Delete task by ID
        static void DeleteTask(TaskManager manager)
        {
            UIHelper.PrintTitle("Delete Task");
            Console.Write("Enter task ID to delete: ");
            var idInput = Console.ReadLine();

            if (Guid.TryParse(idInput, out var id))
            {
                var task = manager.Find(id);
                if (task != null)
                {
                    UIHelper.ShowTask(task);
                    Console.Write("Are you sure you want to delete this task? (y/n): ");
                    var confirm = Console.ReadLine()?.Trim().ToLower();
                    if (confirm == "y")
                    {
                        manager.Delete(id);
                        Console.WriteLine("Task deleted.");
                    }
                    else
                    {
                        Console.WriteLine("Delete cancelled.");
                    }
                }
                else Console.WriteLine("Task not found.");
            }
            else Console.WriteLine("Invalid ID format.");

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        // Change task status
        static void ChangeStatus(TaskManager manager)
        {
            UIHelper.PrintTitle("Change Task Status");
            Console.Write("Enter task ID: ");
            var idInput = Console.ReadLine();

            if (!Guid.TryParse(idInput, out var id))
            {
                Console.WriteLine("Invalid ID format.");
                Console.ReadKey();
                return;
            }

            var task = manager.Find(id);
            if (task == null)
            {
                Console.WriteLine("Task not found.");
                Console.ReadKey();
                return;
            }

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
            Console.WriteLine("\nTask status updated. Press any key to continue...");
            Console.ReadKey();
        }
    }
}
