using System;

namespace FSM.Domain.Enums
{
    [Flags]
    public enum SkillSet
    {
        None = 0,
        General = 1 << 0,      // 1
        Electric = 1 << 1,     // 2
        Plumbing = 1 << 2,     // 4
        Networking = 1 << 3,   // 8
        HVAC = 1 << 4,         // 16
        All = General | Electric | Plumbing | Networking | HVAC
    }
}