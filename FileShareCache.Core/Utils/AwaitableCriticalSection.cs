using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SInnovations.Azure.FileShareCache.Utils
{
    public class AwaitableCriticalSection
    {
        private class Token : IAsyncResult
        {
            private ManualResetEvent _event = new ManualResetEvent(false);
            private bool _synchronous = false;

            public object AsyncState
            {
                get { return null; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _event; }
            }

            public bool CompletedSynchronously
            {
                get { return _synchronous; }
            }

            public bool IsCompleted
            {
                get { return _event.WaitOne(TimeSpan.Zero); }
            }

            public void Signal(bool synchronous)
            {
                _synchronous = synchronous;
                _event.Set();
            }
        }

        private class Disposable : IDisposable
        {
            private readonly AwaitableCriticalSection _owner;

            public Disposable(AwaitableCriticalSection owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                _owner.Exit();
            }
        }

        private Queue<Token> _tokens = new Queue<Token>();
        private bool _busy = false;
        private IDisposable _disposable;

        public AwaitableCriticalSection()
        {
            _disposable = new Disposable(this);
        }

        public Task<IDisposable> EnterAsync()
        {
            lock (this)
            {
                Token token = new Token();
                _tokens.Enqueue(token);
                if (!_busy)
                {
                    _busy = true;
                    _tokens.Dequeue().Signal(true);
                }
                return Task.Factory.FromAsync(token, result => _disposable);
            }
        }

        private void Exit()
        {
            lock (this)
            {
                if (_tokens.Any())
                {
                    _tokens.Dequeue().Signal(false);
                }
                else
                {
                    _busy = false;
                }
            }
        }
    }
}
