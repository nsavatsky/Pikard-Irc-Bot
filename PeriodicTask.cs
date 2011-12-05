using System;
using System.Threading;

namespace PikardIrcBot
{
    /// <summary>
    /// Base class for tasks that run periodically.
    /// </summary>
    internal abstract class PeriodicTask : IDisposable
    {
        private readonly Timer timer;
        private bool disposed;
        private TimeSpan interval;
        private bool running;

        protected PeriodicTask()
        {
            timer = new Timer((state) => this.Run());
            interval = Duration.Infinite;
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
            running = true;
        }

        public void Stop()
        {
            timer.Change(Duration.Infinite, Duration.Infinite);
            running = false;
        }

        protected abstract void Run();

        public TimeSpan Interval
        {
            get { return interval; }
            set
            {
                interval = value;
                if (running)
                {
                    this.Start();
                }
            }
        }

        public bool Running {
            get { return running; }
        }
    }
}