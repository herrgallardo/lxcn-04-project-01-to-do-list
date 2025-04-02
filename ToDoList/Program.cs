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
        // Parses friendly date strings like "today", "tomorrow", etc.
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
            {
                return result;
            }

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
        // Prints a banner title
        public static void PrintTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n" + new string('=', 40));
            Console.WriteLine(title);
            Console.WriteLine(new string('=', 40));
            Console.ResetColor();
        }

        // Prints a menu with keys and descriptions
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

        // Displays task details
        public static void ShowTask(Task task)
        {
            Console.WriteLine($"\nID: {task.Id}");
            Console.WriteLine($"Title: {task.Title}");
            Console.WriteLine($"Due: {task.DueDate:yyyy-MM-dd}");
            Console.WriteLine($"Status: {task.Status}");
            Console.WriteLine($"Project: {task.Project}");
            Console.WriteLine($"Priority: {task.Priority}");
        }

        // Lists all tasks
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
            Console.WriteLine("Welcome to Todo List!");

            var taskManager = new TaskManager();

            var newTask = new Task
            {
                Title = "Sample Task",
                DueDate = NaturalDateParser.Parse("tomorrow"),
                Project = "General",
                Priority = Priority.High
            };

            taskManager.Add(newTask);

            UIHelper.PrintTitle("All Tasks");
            UIHelper.ListTasks(taskManager.GetAll());
        }
    }
}
