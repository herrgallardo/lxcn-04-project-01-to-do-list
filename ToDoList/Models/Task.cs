using System;
using System.Collections.Generic;

namespace TodoListApp.Models
{
    public class Task
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public string Project { get; set; } = string.Empty;
        public Priority Priority { get; set; } = Priority.Medium;
        public List<string> Tags { get; set; } = new List<string>();
        public Recurrence Recurrence { get; set; } = Recurrence.None;

        // Quick checks for task state - used for color coding and filtering
        public bool IsOverdue => Status != TaskStatus.Done && DueDate < DateTime.Today;
        public bool IsDueSoon => Status != TaskStatus.Done && !IsOverdue && DueDate <= DateTime.Today.AddDays(2);
    }
}