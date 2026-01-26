using System.Collections.Generic;
using FSM.Domain.Entities;
// Alias to avoid ambiguity
using TaskEntity = FSM.Domain.Entities.Task; 

namespace FSM.Application.DTOs
{
    public class SchedulerResult
    {
        public List<TechnicianSchedule> Schedules { get; set; } = new();
        public List<TaskEntity> UnscheduledTasks { get; set; } = new();
    }
}