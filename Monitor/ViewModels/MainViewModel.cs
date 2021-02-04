using System.Collections.ObjectModel;
using Monitor.Helpers;
using System.Windows.Threading;
using System;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using System.Linq;

namespace Monitor.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        readonly IKeyboardMouseEvents _globalHook;
        readonly System.Timers.Timer _timer;
        readonly Dispatcher _dispatcher;


        public ObservableCollection<ActivityEvent> Activities { get; }

        double _totalWork;
        double _totalIdle;
        public double TotalWork { get => _totalWork; private set { _totalWork = value; OnPropertyChanged(); } }
        public double TotalIdle { get => _totalIdle; private set { _totalIdle = value; OnPropertyChanged(); } }

        ActivityEvent _currentActivity;
        DateTime _currentActivityStartedUtc;
        readonly TimeSpan _delta = TimeSpan.FromMilliseconds(100);


        public MainViewModel()
        {
            Activities = new ObservableCollection<ActivityEvent>();
            _currentActivity = new ActivityEvent(ActiveWindowHelper.GetActiveWindowTitle(), false, TimeSpan.Zero);
            _currentActivityStartedUtc = DateTime.UtcNow;

            Activities.Add(_currentActivity);

            _dispatcher = Dispatcher.CurrentDispatcher;

            _globalHook = Hook.GlobalEvents();
            _timer = new System.Timers.Timer(100);
            _timer.Elapsed += OnTimerElapsed;
        }


        public void OnLoaded()
        {
            _globalHook.MouseDownExt += GlobalHookMouseDownExt;
            _globalHook.MouseMoveExt += GlobalHookMouseMoveExt;
            _globalHook.KeyPress += GlobalHookKeyPress;

            _timer.Start();
        }

        public void OnClosed()
        {
            _globalHook.MouseDownExt -= GlobalHookMouseDownExt;
            _globalHook.MouseMoveExt -= GlobalHookMouseMoveExt;
            _globalHook.KeyPress -= GlobalHookKeyPress;
            _globalHook.Dispose();

            _timer.Stop();
            _timer.Dispose();
        }


        void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateState();
        }


        void GlobalHookMouseMoveExt(object sender, MouseEventExtArgs e)
        {
            UpdateActive();
        }

        void GlobalHookKeyPress(object sender, KeyPressEventArgs e)
        {
            UpdateActive();
        }

        void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        {
            UpdateActive();
        }


        void UpdateActive()
        {
            var nowUtc = DateTime.UtcNow;
            _currentActivity.Duration = nowUtc - _currentActivityStartedUtc;

            var window = ActiveWindowHelper.GetActiveWindowTitle();
            if (_currentActivity.Window != window || !_currentActivity.IsActive)
            {
                _currentActivityStartedUtc = nowUtc;
                _currentActivity = new ActivityEvent(window, true, TimeSpan.Zero);
                Activities.Add(_currentActivity);
            }

            Recalc();
        }

        void UpdateState()
        {
            var nowUtc = DateTime.UtcNow;
            var currentActivityEndedUtc = _currentActivityStartedUtc + _currentActivity.Duration;
            if (nowUtc - currentActivityEndedUtc > _delta)
            {
                _dispatcher.Invoke(
                    () =>
                    {
                        if (_currentActivity.IsActive)
                        {
                            var window = ActiveWindowHelper.GetActiveWindowTitle();
                            _currentActivityStartedUtc = nowUtc;
                            _currentActivity = new ActivityEvent(window, false, TimeSpan.Zero);
                            Activities.Add(_currentActivity);
                        }
                        else
                        {
                            _currentActivity.Duration = nowUtc - _currentActivityStartedUtc;
                        }

                        Recalc();
                    });
            }
        }


        void Recalc()
        {
            TotalWork = Activities.Where(x => x.IsActive).Sum(x => x.Duration.TotalSeconds);
            TotalIdle = Activities.Where(x => !x.IsActive).Sum(x => x.Duration.TotalSeconds);
        }
    }
}
