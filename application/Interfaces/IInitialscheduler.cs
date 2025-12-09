using System.Collections.Generic;
using FSM.Domain.Entities;
using Task = FSM.Domain.Entities.Task; 

namespace FSM.Application.Interfaces
{
    public interface IInitialScheduler
    {
        List<TechnicianSchedule> GenerateInitialSchedule(List<Technician> technicians, List<Task> tasks);
    }
}