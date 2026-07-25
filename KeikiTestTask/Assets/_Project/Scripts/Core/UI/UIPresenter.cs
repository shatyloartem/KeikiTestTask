using System;
using Zenject;

namespace Core.UI
{
    public abstract class UIPresenter<TView> : IInitializable, IDisposable
        where TView : UIView
    {
        private bool _isInitialized;

        protected UIPresenter(TView view)
        {
            View = view
                ? view
                : throw new ArgumentNullException(nameof(view));
        }

        protected TView View { get; }

        protected virtual bool ShowViewOnInitialize => true;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            SubscribeToEvents();
            OnInitialized();

            if (ShowViewOnInitialize)
                View.Show();
        }

        public void Dispose()
        {
            if (!_isInitialized)
                return;

            UnsubscribeFromEvents();
            OnDisposed();

            _isInitialized = false;
        }

        protected abstract void SubscribeToEvents();

        protected abstract void UnsubscribeFromEvents();

        protected virtual void OnInitialized()
        { }

        protected virtual void OnDisposed()
        { }
    }
}
