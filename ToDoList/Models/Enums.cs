using System;

namespace TodoListApp.Models
{
    public enum TaskStatus { Pending, InProgress, Done }
    public enum Priority { Low, Medium, High, Critical }
    public enum Recurrence { None, Daily, Weekly, Monthly, Yearly, Weekdays, Weekends }
}