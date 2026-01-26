    using System;
using FSM.Domain.Enums; // Required to use SkillSet

namespace FSM.Application.DTOs
{
    public class TechnicianDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string BaseAddress { get; set; }
        public double BaseLatitude { get; set; }
        public double BaseLongitude { get; set; }

        public SkillSet Skills { get; set; }

        public TimeSpan ShiftStart { get; set; }
        public TimeSpan ShiftEnd { get; set; }
        public int MaxConcurrentTasks { get; set; }
        
        public bool IsAvailable { get; set; } 
        
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        
        // representation of the technician's planned route for the day
        public string CurrentRouteSummary { get; set; }
    }
}