using System;
using TodoListApp.Services;
using TodoListApp.UI;

namespace TodoListApp
{
    class Program
    {
        static TaskManager taskManager = new();

        // Track current sort settings for consistent UI experience
        static string currentSortField = "date";
        static bool sortAscending = true;

        static void Main(string[] args)
        {
            DisplayHelper.DisplayWelcomeScreen(taskManager);
            MenuHandler.ShowMainMenu(taskManager, ref currentSortField, ref sortAscending);
        }
    }
}