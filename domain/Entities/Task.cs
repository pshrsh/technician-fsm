using System;
using System.Text.Json.Serialization;
using FSM.Domain.Enums;

namespace FSM.Domain.Entities
{
    public class Task
    {
        public int Id { get; set; }

        public string ClientName { get; set; } = string.Empty;
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; } 

        // Use Enums
        public TaskPriority Priority { get; set; } 
        public SkillSet RequiredSkills { get; set; }

        // FIX: Explicitly tell C# to use YOUR enum, not the system one
        public FSM.Domain.Enums.TaskStatus Status { get; set; } 

        public TimeSpan Duration { get; set; } = TimeSpan.FromHours(1);
        public DateTime TimeWindowStart { get; set; }
        public DateTime TimeWindowEnd { get; set; }

        // Algorithm Fields
        public int SequenceIndex { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }

        // Technician Assignment
        public int? AssignedTechnicianId { get; set; }
        
        [JsonIgnore]
        public Technician? AssignedTechnician { get; set; }
    }
}