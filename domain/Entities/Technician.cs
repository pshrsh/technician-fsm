public static class Technician
{
    public string BaseLocation { get; set; }
    public string[] Skills { get; set; }
    public string StartHours { get; set; }
    public string EndHours { get; set; }
    public task[] AssignedTasks { get; set; }
    public Technician(string baseLocation, string[] skills, string startHours, string endHours)
    {
        BaseLocation = baseLocation;
        Skills = skills;
        StartHours = startHours;
        EndHours = endHours;
        AssignedTasks = new task[] { };
    }
}