using System.Collections.ObjectModel;
using Monitor.Helpers;
using System.Windows.Threading;
using System;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using System.Linq;
using System.Windows.Input;
using System.IO;

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


        TimeSpan _idleTimer;
        public TimeSpan IdleTimer { get => _idleTimer; private set { _idleTimer = value; OnPropertyChanged(); } }


        ActivityEvent _currentActivity;
        DateTime _currentActivityStartedUtc;
        readonly TimeSpan _delta = Properties.Settings.Default.IdleThreshold;


        public ICommand SaveCommand { get; }


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

            SaveCommand = new RelayCommand(SaveExecute);
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
            _currentActivity.Duration += nowUtc - _currentActivityStartedUtc;
            _currentActivityStartedUtc = nowUtc;

            var window = ActiveWindowHelper.GetActiveWindowTitle();
            if (_currentActivity.Window != window || !_currentActivity.IsActive)
            {
                var currentWindowActivity = Activities.FirstOrDefault(x => x.Window == window && x.IsActive);
                if (currentWindowActivity != null)
                {
                    _currentActivity = currentWindowActivity;
                }
                else
                {
                    _currentActivityStartedUtc = nowUtc;
                    _currentActivity = new ActivityEvent(window, true, TimeSpan.Zero);
                    Activities.Add(_currentActivity);
                }
            }

            Recalc();
        }

        void UpdateState()
        {
            var nowUtc = DateTime.UtcNow;
            if(_currentActivity.IsActive)
            {
                if(nowUtc - _currentActivityStartedUtc > _delta)
                {
                    var window = ActiveWindowHelper.GetActiveWindowTitle();
                    var currentWindowIdleActivity = Activities.FirstOrDefault(x => x.Window == window && !x.IsActive);
                    if (currentWindowIdleActivity != null)
                    {
                        _currentActivity = currentWindowIdleActivity;
                        _dispatcher.Invoke(
                            () =>
                            {
                                _currentActivity.Duration += nowUtc - _currentActivityStartedUtc;
                                _currentActivityStartedUtc = nowUtc;
                                Recalc();

                                IdleTimer = TimeSpan.Zero;
                            });
                    }
                    else
                    {
                        _currentActivity = new ActivityEvent(window, false, TimeSpan.Zero);
                        _currentActivity.Duration += nowUtc - _currentActivityStartedUtc;
                        _dispatcher.Invoke(
                            () =>
                            {
                                Activities.Add(_currentActivity);
                                Recalc();

                                IdleTimer = TimeSpan.Zero;
                            });
                    }

                    _currentActivityStartedUtc = nowUtc;
                }
                else
                {
                    _dispatcher.Invoke(() => IdleTimer = nowUtc - _currentActivityStartedUtc);
                }
            }
            else
            {
                _dispatcher.Invoke(
                    () =>
                    {
                        _currentActivity.Duration += nowUtc - _currentActivityStartedUtc;
                        Recalc();
                    });

                _currentActivityStartedUtc = nowUtc;
            }
        }


        void Recalc()
        {
            TotalWork = Activities.Where(x => x.IsActive).Sum(x => x.Duration.TotalSeconds);
            TotalIdle = Activities.Where(x => !x.IsActive).Sum(x => x.Duration.TotalSeconds);
        }


        void SaveExecute(object prm)
        {
            var saveFileDialog = new SaveFileDialog() { Filter = "csv files|*.csv" };
            if(saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var activities = Activities.ToList();
                using(var writer = File.CreateText(saveFileDialog.FileName))
                {
                    var headers = string.Join(",", new[] { "Window", "Is Active", "Duration" } );
                    writer.WriteLine(headers);

                    foreach(var activity in activities)
                    {
                        var line = string.Join(",", new[] { $"\"{activity.Window}\"", activity.IsActive.ToString(), activity.Duration.ToString() });
                        writer.WriteLine(line);
                    }
                }
            }
        }
    }
}
