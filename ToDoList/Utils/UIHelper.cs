using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TodoListApp.Models;

namespace TodoListApp.Utils
{
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
        public static ConsoleColor GetTaskColor(TodoListApp.Models.Task task)
        {
            return task.Status == TodoListApp.Models.TaskStatus.Done ? ConsoleColor.Green :
                   task.IsOverdue ? ConsoleColor.Red :
                   task.IsDueSoon ? ConsoleColor.Yellow :
                   task.Priority == Priority.Critical ? ConsoleColor.DarkRed :
                   task.Priority == Priority.High ? ConsoleColor.DarkYellow :
                   task.Status == TodoListApp.Models.TaskStatus.InProgress ? ConsoleColor.Cyan :
                   ConsoleColor.White;
        }

        // Display detailed information about a single task
        public static void ShowTask(TodoListApp.Models.Task task)
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
        public static void ListTasks(IEnumerable<TodoListApp.Models.Task> tasks)
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
}