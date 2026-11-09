using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ZooWorld.UI
{
    public sealed class TastyLabelPool : MonoBehaviour, ITastyView
    {
        [SerializeField] private TMP_Text _labelTemplate;
        [SerializeField] private string _labelText = "Tasty!";
        [SerializeField, Min(0.01f)] private float _fadeSteepness = 2f;
        [SerializeField, Min(1)] private int _poolSize = 4;
        [SerializeField, Min(0.01f)] private float _duration = 1.5f;
        [SerializeField] private Vector3 _belowOffset = new(0f, 0f, -1f);
        [SerializeField] private Vector3 _drift = new(0f, 0f, 0.6f);

        private readonly List<Entry> _entries = new();
        private int _nextIndex;

        private void Awake()
        {
            if (_labelTemplate == null)
            {
                Debug.LogError("TastyLabelPool has no label template assigned.", this);
                enabled = false;
                return;
            }

            for (var i = 0; i < _poolSize; i++)
            {
                var label = Instantiate(_labelTemplate, transform);
                label.text = _labelText;
                label.gameObject.SetActive(false);
                var group = label.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = label.gameObject.AddComponent<CanvasGroup>();
                }

                _entries.Add(new Entry(label, group));
            }

            _labelTemplate.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            foreach (var entry in _entries)
            {
                if (entry.Routine != null)
                {
                    StopCoroutine(entry.Routine);
                    entry.Routine = null;
                }

                if (entry.Label != null)
                {
                    entry.Label.gameObject.SetActive(false);
                }
            }
        }

        public void Show(Vector3 worldPosition)
        {
            if (!isActiveAndEnabled || _entries.Count == 0)
            {
                return;
            }

            var entry = _entries[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _entries.Count;
            if (entry.Routine != null)
            {
                StopCoroutine(entry.Routine);
            }

            entry.BasePosition = worldPosition + _belowOffset;
            entry.Label.transform.position = entry.BasePosition;
            entry.Group.alpha = 1f;
            entry.Label.gameObject.SetActive(true);
            entry.Routine = StartCoroutine(Animate(entry));
        }

        private IEnumerator Animate(Entry entry)
        {
            var elapsed = 0f;
            while (elapsed < _duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _duration);
                entry.Label.transform.position = entry.BasePosition + _drift * t;
                entry.Group.alpha = Mathf.Clamp01(_fadeSteepness * (1f - t));
            }

            entry.Label.gameObject.SetActive(false);
            entry.Routine = null;
        }

        private sealed class Entry
        {
            public readonly TMP_Text Label;
            public readonly CanvasGroup Group;
            public Coroutine Routine;
            public Vector3 BasePosition;

            public Entry(TMP_Text label, CanvasGroup group)
            {
                Label = label;
                Group = group;
            }
        }
    }
}
