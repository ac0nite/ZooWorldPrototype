using TMPro;
using UnityEngine;

namespace ZooWorld.UI
{
    public sealed class HudView : MonoBehaviour, IHudView
    {
        private const string CounterFormat = "Dead prey: {0}\nDead predators: {1}";

        [SerializeField] private TMP_Text _counterText;

        public void SetCounters(int preyDeaths, int predatorDeaths)
        {
            if (_counterText == null)
            {
                Debug.LogError("HudView has no counter text assigned.", this);
                return;
            }

            _counterText.text = string.Format(CounterFormat, preyDeaths, predatorDeaths);
        }
    }
}