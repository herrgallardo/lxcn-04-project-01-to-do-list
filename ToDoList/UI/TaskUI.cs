using System;
using System.Collections.Generic;
using System.Linq;
using TodoListApp.Models;
using TodoListApp.Services;
using TodoListApp.Utils;

namespace TodoListApp.UI
{
    public static class TaskUI
    {
        // Helper to select a single task from a list with numbering
        // Returns the selected task or null if cancelled
        public static TodoListApp.Models.Task? SelectTask(TaskManager taskManager, string title = "Select a Task", IEnumerable<TodoListApp.Models.Task>? taskList = null)
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
        public static List<TodoListApp.Models.Task> SelectMultipleTasks(TaskManager taskManager, string title = "Select Multiple Tasks", IEnumerable<TodoListApp.Models.Task>? taskList = null)
        {
            var tasks = taskList?.ToList() ?? taskManager.GetAll().ToList();
            if (!tasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No tasks found.");
                Console.ResetColor();
                Console.ReadKey(true);
                return new List<TodoListApp.Models.Task>();
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

            if (input == "0") return new List<TodoListApp.Models.Task>();

            var selectedTasks = new List<TodoListApp.Models.Task>();
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
        public static void ListTasks(TaskManager taskManager, ref string currentSortField, ref bool sortAscending)
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
            TodoListApp.Models.TaskStatus? status = null;
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
                    status = TodoListApp.Models.TaskStatus.Pending;
                    break;
                case ConsoleKey.I:
                    status = TodoListApp.Models.TaskStatus.InProgress;
                    break;
                case ConsoleKey.C:
                    status = TodoListApp.Models.TaskStatus.Done;
                    break;
                case ConsoleKey.O:
                    status = TodoListApp.Models.TaskStatus.Pending;
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
                    ListTasks(taskManager, ref currentSortField, ref sortAscending);
                    return;
                default:
                    return;
            }

            // Recurse to show the same list with new sort field
            if (sortKey is ConsoleKey.D or ConsoleKey.P or ConsoleKey.R or ConsoleKey.S or ConsoleKey.T)
            {
                ListTasks(taskManager, ref currentSortField, ref sortAscending);
            }
        }

        public static void ViewTaskDetails(TaskManager taskManager)
        {
            // Use the selection helper to choose a task
            var task = SelectTask(taskManager, "View Task Details");
            if (task == null) return;

            UIHelper.ShowTask(task);
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
        }

        public static void AddTask(TaskManager taskManager)
        {
            UIHelper.PrintTitle("Add New Task");

            Console.Write("Title: ");
            var title = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Title cannot be empty. Task creation cancelled.");
                Console.ResetColor();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.Write("Description (optional): ");
            var description = Console.ReadLine() ?? string.Empty;

            DateTime dueDate = DateTime.Today; // Initialize with default value
            bool validDate = false;
            do
            {
                Console.Write("Due Date (today/tomorrow/next week or YYYY-MM-DD): ");
                var dateInput = Console.ReadLine() ?? string.Empty;
                var parseResult = NaturalDateParser.Parse(dateInput);

                if (parseResult.Success)
                {
                    dueDate = parseResult.Date;
                    validDate = true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {parseResult.ErrorMessage}");
                    Console.ResetColor();
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey(true);
                }
            } while (!validDate);

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

            taskManager.Add(new TodoListApp.Models.Task
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
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

        public static void EditTask(TaskManager taskManager)
        {
            // Use the selection helper to choose a task to edit
            var task = SelectTask(taskManager, "Edit Task");
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

            // No need to declare a dueDate variable here since we're modifying task.DueDate directly
            bool validDate = false;
            do
            {
                Console.Write($"New Due Date [{task.DueDate:yyyy-MM-dd}]: ");
                var dateInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(dateInput))
                {
                    // Keep the current date if input is empty
                    validDate = true;
                }
                else
                {
                    var parseResult = NaturalDateParser.Parse(dateInput);
                    if (parseResult.Success)
                    {
                        task.DueDate = parseResult.Date;
                        validDate = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {parseResult.ErrorMessage}");
                        Console.ResetColor();
                        Console.WriteLine("Press any key to try again or just press Enter to keep the current date...");
                        Console.ReadKey(true);
                    }
                }
            } while (!validDate);

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

        public static void ChangeStatus(TaskManager taskManager)
        {
            // Use the selection helper to choose a task to change status
            var task = SelectTask(taskManager, "Change Task Status");
            if (task == null) return;

            Console.WriteLine($"Current Status: {task.Status}");
            Console.WriteLine("[P] Pending | [I] In Progress | [D] Done");
            Console.Write("New Status: ");

            var key = Console.ReadKey(true).Key;
            TodoListApp.Models.TaskStatus newStatus = key switch
            {
                ConsoleKey.P => TodoListApp.Models.TaskStatus.Pending,
                ConsoleKey.I => TodoListApp.Models.TaskStatus.InProgress,
                ConsoleKey.D => TodoListApp.Models.TaskStatus.Done,
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

        public static void DeleteTask(TaskManager taskManager)
        {
            // Use the selection helper to choose a task to delete
            var task = SelectTask(taskManager, "Remove Task");
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
        public static void BulkOperations(TaskManager taskManager)
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
            var selectedTasks = SelectMultipleTasks(taskManager, operationTitle);

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
                    success = taskManager.BulkUpdateStatus(ids, TodoListApp.Models.TaskStatus.Done);
                    operationName = "marked as done";
                    break;
                case ConsoleKey.P:
                    success = taskManager.BulkUpdateStatus(ids, TodoListApp.Models.TaskStatus.InProgress);
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

        public static void UndoDelete(TaskManager taskManager)
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

        public static void ExportTasks(TaskManager taskManager)
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