using System;
using System.Globalization;


namespace TodoListApp
{
    public enum TaskStatus { Pending, inProgress, Done }

    public enum Priority { Low, Medium, High, Critical }

    public class Task
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public string Project { get; set; } = string.Empty;
        public Priority Priority { get; set; } = Priority.Medium;
    }

    public static class naturaldateParser
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
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Todo List!");
        }
    }
}