using System;
using VContainer.Unity;
using ZooWorld.Interaction;
using ZooWorld.Systems;

namespace ZooWorld.UI
{
    public sealed class TastyPresenter : IStartable, IDisposable
    {
        private readonly AnimalEventBus _events;
        private readonly ITastyView _view;
        private bool _subscribed;

        public TastyPresenter(AnimalEventBus events, ITastyView view)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            _events.Consumed += OnConsumed;
        }

        public void Dispose()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            _events.Consumed -= OnConsumed;
        }

        private void OnConsumed(AnimalConsumed payload)
        {
            _view.Show(payload.Position);
        }
    }
}
