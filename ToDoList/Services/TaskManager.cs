using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TodoListApp.Models;

namespace TodoListApp.Services
{
    public class TaskManager
    {
        private List<TodoListApp.Models.Task> _tasks = new();
        private List<TodoListApp.Models.Task> _deletedTasks = new(); // For tracking all deleted tasks
        private List<List<TodoListApp.Models.Task>> _deletionBatches = new(); // For tracking batches of deletions
        private static readonly string TasksFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks.json");
        private static readonly string BackupFilePath = Path.Combine(AppContext.BaseDirectory, "../../../tasks_backup.json");

        public TaskManager() => Load();

        public void Add(TodoListApp.Models.Task task) { _tasks.Add(task); Save(); }
        public IEnumerable<TodoListApp.Models.Task> GetAll() => _tasks;
        public IEnumerable<TodoListApp.Models.Task> GetSortedByDate() => _tasks.OrderBy(t => t.DueDate);
        public IEnumerable<TodoListApp.Models.Task> GetSortedByProject() => _tasks.OrderBy(t => t.Project);
        public IEnumerable<TodoListApp.Models.Task> Search(string term) => _tasks.Where(t =>
            t.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.Project.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)));
        public TodoListApp.Models.Task? Find(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);
        public void Update(TodoListApp.Models.Task task) { var i = _tasks.FindIndex(t => t.Id == task.Id); if (i >= 0) { _tasks[i] = task; Save(); } }

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
        public bool BulkUpdateStatus(List<Guid> ids, TodoListApp.Models.TaskStatus status)
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
                { "Pending", _tasks.Count(t => t.Status == TodoListApp.Models.TaskStatus.Pending) },
                { "In Progress", _tasks.Count(t => t.Status == TodoListApp.Models.TaskStatus.InProgress) },
                { "Completed", _tasks.Count(t => t.Status == TodoListApp.Models.TaskStatus.Done) },
                { "Overdue", _tasks.Count(t => t.IsOverdue) },
                { "Due Today", _tasks.Count(t => t.Status != TodoListApp.Models.TaskStatus.Done && t.DueDate.Date == DateTime.Today) },
                { "Due This Week", _tasks.Count(t => t.Status != TodoListApp.Models.TaskStatus.Done &&
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
                _tasks = JsonSerializer.Deserialize<List<TodoListApp.Models.Task>>(json) ?? new List<TodoListApp.Models.Task>();

                // Process recurring tasks - if a recurring task is completed,
                // create a new instance for the next occurrence and disable recurrence on completed task
                foreach (var task in _tasks.ToList())
                {
                    if (task.Status == TodoListApp.Models.TaskStatus.Done && task.Recurrence != Recurrence.None)
                    {
                        var newTask = new TodoListApp.Models.Task
                        {
                            Title = task.Title,
                            Description = task.Description,
                            Project = task.Project,
                            Priority = task.Priority,
                            Tags = new List<string>(task.Tags),
                            Recurrence = task.Recurrence,
                            DueDate = GetNextDueDate(task.DueDate, task.Recurrence),
                            Status = TodoListApp.Models.TaskStatus.Pending
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
                    ? JsonSerializer.Deserialize<List<TodoListApp.Models.Task>>(File.ReadAllText(BackupFilePath)) ?? new List<TodoListApp.Models.Task>()
                    : new List<TodoListApp.Models.Task>();
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
}