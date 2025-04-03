using System;
using System.Collections.Generic;
using System.Linq;
using TodoListApp.Models;
using TodoListApp.Services;
using TodoListApp.Utils;

namespace TodoListApp.UI
{
    public static class DisplayHelper
    {
        // Display welcome screen with ASCII art and task statistics
        public static void DisplayWelcomeScreen(TaskManager taskManager)
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

        // Display task statistics with project breakdowns
        public static void ShowStatistics(TaskManager taskManager)
        {
            var stats = taskManager.GetTaskStatistics();
            UIHelper.DisplayStatistics(stats);

            // Project breakdown - show completion stats for each project
            Console.WriteLine("\n--- Projects Breakdown ---");
            var allProjects = taskManager.GetAllProjects().ToList();
            foreach (var project in allProjects)
            {
                var projectTasks = taskManager.GetAll().Where(t => t.Project == project).ToList();
                var completedCount = projectTasks.Count(t => t.Status == TodoListApp.Models.TaskStatus.Done);
                var pendingCount = projectTasks.Count(t => t.Status == TodoListApp.Models.TaskStatus.Pending);
                var inProgressCount = projectTasks.Count(t => t.Status == TodoListApp.Models.TaskStatus.InProgress);

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
    }
}