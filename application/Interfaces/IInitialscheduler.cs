using System.Collections.Generic;
using FSM.Domain.Entities;
using FSM.Application.DTOs; // Add this namespace
using TaskEntity = FSM.Domain.Entities.Task;

namespace FSM.Application.Interfaces
{
    public interface IInitialScheduler
    {
        SchedulerResult GenerateInitialSchedule(List<Technician> technicians, List<TaskEntity> tasks);
    }
}