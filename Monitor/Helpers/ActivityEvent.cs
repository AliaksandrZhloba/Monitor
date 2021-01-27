using Monitor.ViewModels;
using System;

namespace Monitor.Helpers
{
    public class ActivityEvent : BaseViewModel
    {
        public string Window { get; }
        public bool IsActive { get; }

        TimeSpan _duration;
        public TimeSpan Duration { get => _duration; set { _duration = value; OnPropertyChanged(); } }


        public ActivityEvent(string window, bool isActive, TimeSpan duration)
        {
            Window = window;
            IsActive = isActive;
            Duration = duration;
        }

        public override string ToString()
        {
            var state = IsActive ? "work" : "idle";
            return $"{Window}: {state} {Duration:c}";
        }
    }
}
