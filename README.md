# Todo List Application

## Course Context

- **Program**: Arbetsmarknadsutbildning - IT Påbyggnad/Programmerare
- **Course**: C# .NET Fullstack System Developer
- **Individual Project**: Todo List Application

## Learning Objectives

This console application demonstrates advanced C# programming concepts:

- Object-Oriented Programming with proper architecture
- Advanced Console UI/UX design with color-coding and pagination
- JSON serialization/deserialization for state persistence
- Natural language processing for date inputs
- LINQ for complex data filtering and sorting
- State management with undo functionality
- Data exports and backup strategies

## Overview

A sophisticated console-based task management application that goes beyond basic todo functionality. The application features an intuitive color-coded interface, comprehensive task management capabilities, and advanced filtering and sorting options to help users efficiently organize their personal and professional responsibilities.

## Features

### Core Task Management

- Create, edit, view, and delete tasks with detailed properties
- Mark tasks as Pending, In Progress, or Done
- Assign priority levels (Low, Medium, High, Critical)
- Add multiple tags to tasks for flexible categorization
- Set up recurring tasks with various patterns (Daily, Weekly, Monthly, Yearly, Weekdays, Weekends)
- Track overdue tasks and tasks due soon with visual indicators

### Advanced UI Features

- Color-coded task display based on status, priority, and due dates
- Pagination for viewing large task collections
- Natural language date parsing ("today", "tomorrow", "next week", etc.)
- ASCII art welcome screen with task statistics
- Loading animations for longer operations
- Full-featured console menu system

### Data Management

- Automatic JSON serialization with backup creation
- CSV export functionality for external processing
- Intelligent handling of recurring tasks
- Bulk operations on multiple tasks
- Undo functionality for task deletions

### Organizational Features

- Filter tasks by status, due date, project, or tags
- Sort tasks by date, project, priority, status, or title
- View comprehensive statistics about your task collection
- Track tasks across multiple projects

## Technical Architecture

The application follows a structured architecture pattern:

- **Models**: Task class and supporting data types
- **Services**: TaskManager for core business logic
- **UI**: Components for user interface rendering
- **Utils**: Helper classes for common functionality

## Application Flow

The application presents a rich interface with multiple capabilities:

1. **Welcome Screen**

   - ASCII art logo for TodoList
   - Task statistics dashboard
   - Alert for overdue tasks

2. **Main Menu**

   - A: Add Task - Create new tasks with comprehensive details
   - L: List Tasks - View tasks with filtering and sorting options
   - V: View Task Details - See complete information about a selected task
   - E: Edit Task - Modify existing task properties
   - P: Change Task Status - Update task progress state
   - R: Remove Task - Delete unwanted tasks
   - B: Bulk Operations - Manage multiple tasks at once
   - S: Statistics - View task analytics and project breakdowns
   - U: Undo Delete - Restore recently deleted tasks
   - X: Export Tasks to CSV - Generate file exports
   - Q: Quit - Exit application

3. **Task Listing Options**

   - All Tasks
   - Pending Tasks
   - In Progress Tasks
   - Completed Tasks
   - Overdue Tasks
   - Tasks Due Today
   - Tasks Due This Week
   - Search Tasks
   - Filter by Project
   - Filter by Tag

4. **State Management**
   - Automatic saving after each action
   - Task state persistence between sessions
   - Intelligent handling of recurring tasks

## Advanced Features

### Natural Language Date Parsing

The application supports intuitive date input formats including:

- "today", "tomorrow", "next week"
- "in 3 days", "next month"
- Day names like "Monday" or "Fri"
- Standard date formats (YYYY-MM-DD, MM/DD/YYYY)

### Intelligent Color Coding

Tasks are displayed with color highlighting based on:

- Status (green for completed tasks)
- Priority (red for critical tasks)
- Due date (yellow for tasks due soon, red for overdue)

### Comprehensive Statistics

The dashboard provides insights including:

- Task counts by status
- Overdue and upcoming tasks
- High priority task counts
- Project-specific completion rates

## Requirements

- .NET SDK 6.0 or later
- Console-compatible terminal with color support
- Basic file system access for data persistence

## How to Run

```bash
dotnet build
dotnet run
```

## Implementation Highlights

- Robust exception handling throughout the application
- Automatic recurring task creation
- Pagination for large task collections
- Task uniqueness through GUID identification
- JSON serialization with backup strategy
- Natural language date parser with relative date support

## Educational Program Details

- **Type**: Arbetsmarknadsutbildning (Labor Market Training)
- **Focus**: IT Påbyggnad/Programmerare (IT Advanced/Programmer)
- **Course**: C# .NET Fullstack System Developer

## Potential Improvements for Future Learning

- Database integration (SQL Server or SQLite)
- Web API layer for remote access
- Email or notification integration
- Calendar application synchronization
- Multi-user support with authentication
- Mobile companion application
- Task dependency tracking
- Time estimation and tracking
- Custom project workflows
