using System.Timers;
using System.Collections.ObjectModel;
using Monitor.Helpers;
using System.Windows.Threading;
using System;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;

namespace Monitor.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        readonly IKeyboardMouseEvents _globalHook;
        readonly DispatcherTimer _timer;


        public ObservableCollection<ActivityEvent> Activities { get; }
        ActivityEvent _currentActivity;
        DateTime _currentActivityStartedUtc;


        public MainViewModel()
        {
            Activities = new ObservableCollection<ActivityEvent>();
            _currentActivity = new ActivityEvent(ActiveWindowHelper.GetActiveWindowTitle(), false, TimeSpan.Zero);
            _currentActivityStartedUtc = DateTime.UtcNow;

            _globalHook = Hook.GlobalEvents();
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, OnTimerAlpsed, Dispatcher.CurrentDispatcher);
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
        }


        void OnTimerAlpsed(object sender, EventArgs e)
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
        }

        void UpdateState()
        {

        }
    }
}
