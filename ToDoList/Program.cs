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
        public static DateTime Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return DateTime.Today;
            input = input.ToLowerInvariant().Trim();
            return input switch
            {
                "today" => DateTime.Today,
                "tomorrow" => DateTime.Today.AddDays(1),
                "next week" => DateTime.Today.AddDays(7),
                _ => DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : DateTime.Today
            };
        }
    }

    public class TaskManager
    {
        private List<Task> _tasks = new();
        private static readonly string TasksFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks.json");
        private static readonly string BackupFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks_backup.json");

        public TaskManager() => Load();

        public void Add(Task task) { _tasks.Add(task); Save(); }
        public IEnumerable<Task> GetAll() => _tasks;
        public IEnumerable<Task> GetSortedByDate() => _tasks.OrderBy(t => t.DueDate);
        public IEnumerable<Task> GetSortedByProject() => _tasks.OrderBy(t => t.Project);
        public Task? Find(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);
        public void Update(Task task) { var i = _tasks.FindIndex(t => t.Id == task.Id); if (i >= 0) { _tasks[i] = task; Save(); } }
        public void Delete(Guid id) { _tasks = _tasks.Where(t => t.Id != id).ToList(); Save(); }

        public void Save()
        {
            if (File.Exists(TasksFilePath)) File.Copy(TasksFilePath, BackupFilePath, true);
            var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(TasksFilePath, json);
        }

        public void Load()
        {
            if (!File.Exists(TasksFilePath)) return;
            try { var json = File.ReadAllText(TasksFilePath); _tasks = JsonSerializer.Deserialize<List<Task>>(json) ?? new List<Task>(); }
            catch { _tasks = File.Exists(BackupFilePath) ? JsonSerializer.Deserialize<List<Task>>(File.ReadAllText(BackupFilePath)) ?? new List<Task>() : new List<Task>(); }
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
            Console.WriteLine($"\nID: {task.Id}\nTitle: {task.Title}\nDue: {task.DueDate:yyyy-MM-dd}\nStatus: {task.Status}\nProject: {task.Project}\nPriority: {task.Priority}");
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
                    { "L", "List All Tasks" },
                    { "P", "List Tasks by Project" },
                    { "D", "List Tasks by Due Date" },
                    { "V", "View Task Details" },
                    { "E", "Edit Task" },
                    { "S", "Change Task Status" },
                    { "R", "Remove Task" },
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
                    case "V": ViewTask(taskManager); break;
                    case "E": EditTask(taskManager); break;
                    case "S": ChangeStatus(taskManager); break;
                    case "R": DeleteTask(taskManager); break;
                    case "Q": exit = true; break;
                    default: Console.WriteLine("Invalid option."); Console.ReadKey(); break;
                }
            }
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
                    UIHelper.ShowTask(task);
                else
                    Console.WriteLine("Task not found.");
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
