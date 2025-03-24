using System;
using System.Collections.Generic;
using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TodoApp.API.DTOs
{
    public class TodoTaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskPriority Priority { get; set; }
        public TodoTaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrenceInfoDto RecurrenceInfo { get; set; }
        public List<ReminderDto> Reminders { get; set; }
        public List<string> Tags { get; set; }
        public List<TodoNoteDto> Notes { get; set; }

        public string UserName { get; set; }
    }
}