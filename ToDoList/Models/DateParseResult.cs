using System;

namespace TodoListApp.Models
{
    public class DateParseResult
    {
        public DateTime Date { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static DateParseResult Ok(DateTime date) => new DateParseResult { Date = date, Success = true };
        public static DateParseResult Error(string message) => new DateParseResult { Date = DateTime.Today, Success = false, ErrorMessage = message };
    }
}