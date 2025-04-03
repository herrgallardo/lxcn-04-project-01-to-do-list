using System;
using System.Globalization;
using TodoListApp.Models;

namespace TodoListApp.Utils
{
    public static class NaturalDateParser
    {
        // Maximum years into the future for task dates (adjust as needed)
        private const int MaxYearsInFuture = 10;

        // Handles various date formats like "tomorrow", "next week", "in 3 days"
        // Allows users to input dates in a natural language style
        // Returns a DateParseResult with success/error status
        public static DateParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DateParseResult.Ok(DateTime.Today);

            input = input.ToLowerInvariant().Trim();

            try
            {
                DateTime resultDate = input switch
                {
                    "today" => DateTime.Today,
                    "tomorrow" => DateTime.Today.AddDays(1),
                    "next week" => DateTime.Today.AddDays(7),
                    "yesterday" => DateTime.Today.AddDays(-1),
                    "next month" => DateTime.Today.AddMonths(1),
                    "next year" => DateTime.Today.AddYears(1),
                    "monday" or "mon" => GetNextWeekday(DayOfWeek.Monday),
                    "tuesday" or "tue" => GetNextWeekday(DayOfWeek.Tuesday),
                    "wednesday" or "wed" => GetNextWeekday(DayOfWeek.Wednesday),
                    "thursday" or "thu" => GetNextWeekday(DayOfWeek.Thursday),
                    "friday" or "fri" => GetNextWeekday(DayOfWeek.Friday),
                    "saturday" or "sat" => GetNextWeekday(DayOfWeek.Saturday),
                    "sunday" or "sun" => GetNextWeekday(DayOfWeek.Sunday),
                    "end of week" => GetEndOfWeek(),
                    "end of month" => GetEndOfMonth(),
                    "end of year" => new DateTime(DateTime.Today.Year, 12, 31),
                    _ when input.StartsWith("in ") => ParseRelativeDate(input[3..]),
                    _ when input.StartsWith("next ") => ParseNextOccurrence(input[5..]),
                    _ => TryParseStandardDate(input, out var errorMessage)
                        ? DateTime.Today : throw new FormatException(errorMessage)
                };

                // Validate the date is within reasonable range
                return ValidateDate(resultDate);
            }
            catch (FormatException ex)
            {
                return DateParseResult.Error(ex.Message);
            }
            catch (Exception)
            {
                return DateParseResult.Error("Invalid date format. Please try again.");
            }
        }

        // Validate that the date is within reasonable bounds
        private static DateParseResult ValidateDate(DateTime date)
        {
            // Check if date is too far in the future
            if (date > DateTime.Today.AddYears(MaxYearsInFuture))
            {
                return DateParseResult.Error($"Date cannot be more than {MaxYearsInFuture} years in the future.");
            }

            // If all validations pass
            return DateParseResult.Ok(date);
        }

        // Returns the date of the next occurrence of the specified day of week
        // If today is the target day, returns next week's occurrence
        private static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
        {
            DateTime date = DateTime.Today;
            int daysToAdd = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;
            if (daysToAdd == 0) daysToAdd = 7; // If today is the target day, get next week
            return date.AddDays(daysToAdd);
        }

        // Returns the date for the next Sunday (end of week)
        private static DateTime GetEndOfWeek()
        {
            DateTime date = DateTime.Today;
            int daysToAdd = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
            return date.AddDays(daysToAdd);
        }

        // Returns the last day of the current month
        private static DateTime GetEndOfMonth()
        {
            DateTime date = DateTime.Today;
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }

        // Handles "next X" format where X is a day or time period
        private static DateTime ParseNextOccurrence(string occurrence)
        {
            return occurrence switch
            {
                "monday" or "mon" => GetNextWeekday(DayOfWeek.Monday),
                "tuesday" or "tue" => GetNextWeekday(DayOfWeek.Tuesday),
                "wednesday" or "wed" => GetNextWeekday(DayOfWeek.Wednesday),
                "thursday" or "thu" => GetNextWeekday(DayOfWeek.Thursday),
                "friday" or "fri" => GetNextWeekday(DayOfWeek.Friday),
                "saturday" or "sat" => GetNextWeekday(DayOfWeek.Saturday),
                "sunday" or "sun" => GetNextWeekday(DayOfWeek.Sunday),
                "week" => DateTime.Today.AddDays(7),
                "month" => DateTime.Today.AddMonths(1),
                "year" => DateTime.Today.AddYears(1),
                _ => throw new FormatException($"Unknown occurrence '{occurrence}'")
            };
        }

        // Handles "in X days/weeks/months/years" format
        private static DateTime ParseRelativeDate(string relativeInput)
        {
            var parts = relativeInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out int amount))
                throw new FormatException("Invalid relative date format. Use 'in X days/weeks/months/years'.");

            if (amount <= 0)
                throw new FormatException("Time amount must be positive.");

            return parts[1] switch
            {
                "day" or "days" => DateTime.Today.AddDays(amount),
                "week" or "weeks" => DateTime.Today.AddDays(amount * 7),
                "month" or "months" => DateTime.Today.AddMonths(amount),
                "year" or "years" => DateTime.Today.AddYears(amount),
                _ => throw new FormatException($"Unknown time unit '{parts[1]}'")
            };
        }

        // Attempts to parse a standard date format
        // Returns true if successful, false otherwise with an error message
        private static bool TryParseStandardDate(string input, out string errorMessage)
        {
            string[] formats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "d-MMM", "d-MMM-yyyy", "d MMM", "d MMM yyyy" };
            errorMessage = string.Empty;

            // Try parsing with exact formats first
            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return true;

            // Try with culture-specific parsing as fallback
            if (DateTime.TryParse(input, out _))
                return true;

            // If parsing fails, provide a helpful error message
            errorMessage = $"Invalid date format '{input}'. Please use one of the following formats: YYYY-MM-DD, MM/DD/YYYY, DD/MM/YYYY, or enter natural language like 'tomorrow', 'next week', etc.";
            return false;
        }
    }
}