using System;
using System.Collections.Generic;
using TodoListApp.Services;
using TodoListApp.Utils;

namespace TodoListApp.UI
{
    public static class MenuHandler
    {
        public static void ShowMainMenu(TaskManager taskManager, ref string currentSortField, ref bool sortAscending)
        {
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
                        case ConsoleKey.A: TaskUI.AddTask(taskManager); break;
                        case ConsoleKey.L: TaskUI.ListTasks(taskManager, ref currentSortField, ref sortAscending); break;
                        case ConsoleKey.V: TaskUI.ViewTaskDetails(taskManager); break;
                        case ConsoleKey.E: TaskUI.EditTask(taskManager); break;
                        case ConsoleKey.P: TaskUI.ChangeStatus(taskManager); break;
                        case ConsoleKey.R: TaskUI.DeleteTask(taskManager); break;
                        case ConsoleKey.B: TaskUI.BulkOperations(taskManager); break;
                        case ConsoleKey.S: DisplayHelper.ShowStatistics(taskManager); break;
                        case ConsoleKey.U: TaskUI.UndoDelete(taskManager); break;
                        case ConsoleKey.X: TaskUI.ExportTasks(taskManager); break;
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
    }
}