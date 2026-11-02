using System;
using VContainer.Unity;
using ZooWorld.Systems;

namespace ZooWorld.UI
{
    public sealed class HudPresenter : IStartable, IDisposable
    {
        private readonly GameStats _stats;
        private readonly IHudView _view;
        private bool _subscribed;

        public HudPresenter(GameStats stats, IHudView view)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            _stats.Changed += OnStatsChanged;
            PushCounters();
        }

        public void Dispose()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            _stats.Changed -= OnStatsChanged;
        }

        private void OnStatsChanged()
        {
            PushCounters();
        }

        private void PushCounters()
        {
            _view.SetCounters(_stats.PreyDeaths, _stats.PredatorDeaths);
        }
    }
}
