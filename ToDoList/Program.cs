using System;


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
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Todo List!");
        }
    }
}