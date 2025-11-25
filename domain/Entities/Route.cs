using System;

namespace Domain.Entities
{
    public class Route
    {
        public string StartLocation { get; set; }
        public string EndLocation { get; set; }
        public TimeSpan EstimatedTime { get; set; }

        public Route(string startLocation, string endLocation, TimeSpan estimatedTime)
        {
            StartLocation = startLocation;
            EndLocation = endLocation;
            EstimatedTime = estimatedTime;
        }
    }
}