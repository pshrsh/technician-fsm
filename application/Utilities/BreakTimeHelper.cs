using System;

namespace FSM.Application.Utilities
{
    /// <summary>
    /// Helper class to handle mandatory break periods during technician shifts.
    /// Break is from 13:00 (1 PM) to 14:00 (2 PM).
    /// </summary>
    public static class BreakTimeHelper
    {
        public static readonly TimeSpan BreakStart = new TimeSpan(13, 0, 0); // 1:00 PM
        public static readonly TimeSpan BreakEnd = new TimeSpan(13, 45, 00);   // 2:00 PM
        public static readonly TimeSpan BreakDuration = TimeSpan.FromHours(1);

        /// <summary>
        /// Adjusts the current time forward if it falls within or crosses the break period.
        /// </summary>
        /// <param name="currentTime">The current time as TimeSpan (time of day)</param>
        /// <returns>Adjusted time that accounts for the break</returns>
        public static TimeSpan AdjustForBreak(TimeSpan currentTime)
        {
            // If we're currently in the break period, jump to the end of break
            if (currentTime >= BreakStart && currentTime < BreakEnd)
            {
                return BreakEnd;
            }
            return currentTime;
        }

        /// <summary>
        /// Calculates the end time after performing a task, accounting for break.
        /// If the task would span across the break period, it shifts to after break.
        /// </summary>
        /// <param name="startTime">When the task would start (TimeSpan)</param>
        /// <param name="duration">How long the task takes</param>
        /// <returns>The actual end time after accounting for break</returns>
        public static TimeSpan CalculateEndTimeWithBreak(TimeSpan startTime, TimeSpan duration)
        {
            TimeSpan endTime = startTime.Add(duration);

            // Case 1: Task starts and ends before break - no adjustment needed
            if (endTime <= BreakStart)
            {
                return endTime;
            }

            // Case 2: Task starts before break but would end during or after break
            // Push the entire task to after break
            if (startTime < BreakStart && endTime > BreakStart)
            {
                startTime = BreakEnd;
                return startTime.Add(duration);
            }

            // Case 3: Task starts during break - already handled by AdjustForBreak
            // Case 4: Task starts after break - no adjustment needed
            return endTime;
        }

        /// <summary>
        /// Adds travel/work time to current time, accounting for break period.
        /// </summary>
        /// <param name="currentTime">Current time of day</param>
        /// <param name="additionalTime">Time to add (travel or task duration)</param>
        /// <returns>New time after adding duration and accounting for break</returns>
        public static TimeSpan AddTimeWithBreak(TimeSpan currentTime, TimeSpan additionalTime)
        {
            // First, adjust current time if we're in break
            currentTime = AdjustForBreak(currentTime);

            TimeSpan projectedEnd = currentTime.Add(additionalTime);

            // If we would cross into the break period, we need to add break duration
            if (currentTime < BreakStart && projectedEnd > BreakStart)
            {
                // We cross the break, so add the break hour
                return projectedEnd.Add(BreakDuration);
            }

            return projectedEnd;
        }

        /// <summary>
        /// Calculates the effective working time between two TimeSpans, excluding break.
        /// </summary>
        /// <param name="start">Start time</param>
        /// <param name="end">End time</param>
        /// <returns>Duration excluding break time</returns>
        public static TimeSpan GetWorkingDuration(TimeSpan start, TimeSpan end)
        {
            TimeSpan totalDuration = end - start;

            // If the span includes the break period, subtract it
            if (start < BreakEnd && end > BreakStart)
            {
                return totalDuration.Subtract(BreakDuration);
            }

            return totalDuration;
        }
    }
}