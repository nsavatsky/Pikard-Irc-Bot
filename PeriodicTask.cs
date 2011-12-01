using System;
using System.Threading;

namespace PikardIrcBot
{
    /// <summary>
    /// Base class for tasks that run periodically.
    /// </summary>
    internal abstract class PeriodicTask : IDisposable
    {
        private bool disposed;
        private Timer timer;
        private TimeSpan interval;

        public PeriodicTask()
        {
            timer = new Timer((state) => this.Run());
        }

        protected PeriodicTask(TimeSpan interval)
        {
            this.interval = interval;
        }

        ~PeriodicTask()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            timer.Dispose();

            disposed = true;
        }

        public void Start()
        {
            timer.Change(interval, interval);
        }

        public void Stop()
        {
            timer.Change(Duration.Infinite, Duration.Infinite);
        }

        protected abstract void Run();

        public TimeSpan Interval
        {
            get { return interval; }
            set { interval = value; }
        }
    }
}