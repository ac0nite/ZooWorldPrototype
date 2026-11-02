#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZooWorld.UI;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class UiViewTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            var cameraGo = Track(new GameObject("UI camera"));
            _camera = cameraGo.AddComponent<Camera>();
            _camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            _camera.orthographic = true;
            _camera.orthographicSize = 5;
            _camera.aspect = 1;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test, Category("AC07")]
        public void HudViewWritesBothCountersInTheTaskFormat()
        {
            Text text = CreateText(null, "Counters");
            HudView view = CreateHudView(text);
            view.SetCounters(2, 3);
            Assert.That(text.text, Is.EqualTo("Dead prey: 2\nDead predators: 3"));
        }

        [Test, Category("AC07")]
        public void HudViewStartsFromZeroedPreviewText()
        {
            Text text = CreateText(null, "Counters");
            text.text = "Dead prey: 9\nDead predators: 9";
            HudView view = CreateHudView(text);
            view.SetCounters(0, 0);
            Assert.That(text.text, Is.EqualTo("Dead prey: 0\nDead predators: 0"));
        }

        [UnityTest, Category("AC07")]
        public IEnumerator TastyLabelAppearsBelowTheEaterInTopDownViewThenHides()
        {
            TastyLabelPool pool = CreatePool(out Text _);
            var captured = new Vector3(2f, 0.5f, 3f);
            pool.Show(captured);
            Text label = SingleActiveLabel(pool);
            Assert.That(label.text, Is.EqualTo("Tasty!"));
            var group = label.GetComponent<CanvasGroup>();
            Assert.That(group, Is.Not.Null);
            float labelScreenY = _camera.WorldToScreenPoint(label.transform.position).y;
            float eaterScreenY = _camera.WorldToScreenPoint(captured).y;
            Assert.That(labelScreenY, Is.LessThan(eaterScreenY),
                "The popup must appear below the eater on screen, not above it.");
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(group.alpha, Is.EqualTo(1f),
                "The label must stay fully opaque during the first half of its duration.");
            float earlyScreenY = _camera.WorldToScreenPoint(label.transform.position).y;
            while (elapsed < 1.2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(group.alpha, Is.LessThan(1f),
                "The label must fade during the second half of its duration.");
            Assert.That(group.alpha, Is.GreaterThan(0f));
            float lateScreenY = _camera.WorldToScreenPoint(label.transform.position).y;
            Assert.That(lateScreenY, Is.GreaterThan(earlyScreenY),
                "The label must drift upward on screen while fading.");
            while (elapsed < 1.3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(label.gameObject.activeSelf, Is.True,
                "The label must still be visible at 1.3 s of its 1.5 s duration.");
            while (elapsed < 1.8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(ActiveLabels(pool), Is.Empty, "The label must hide after the 1.5 s popup duration.");
        }

        [UnityTest, Category("AC03"), Category("AC07")]
        public IEnumerator TastyLabelPoolStealsTheRoundRobinSlotWhenAllLabelsAreBusy()
        {
            TastyLabelPool pool = CreatePool(out Text _, poolSize: 2);
            pool.Show(new Vector3(0f, 0.5f, 0f));
            pool.Show(new Vector3(1f, 0.5f, 0f));
            pool.Show(new Vector3(9f, 0.5f, 9f));
            List<Text> active = ActiveLabels(pool);
            Assert.That(active.Count, Is.EqualTo(2));
            var positions = new List<Vector3>();
            foreach (Text label in active)
            {
                positions.Add(label.transform.position);
            }

            Assert.That(positions, Does.Contain(new Vector3(1f, 0.5f, -1f)));
            Assert.That(positions, Does.Contain(new Vector3(9f, 0.5f, 8f)));
            yield return null;
        }

        [UnityTest, Category("AC07"), Category("AC09")]
        public IEnumerator TastyLabelPoolReusesALabelAfterItHides()
        {
            TastyLabelPool pool = CreatePool(out Text _);
            pool.Show(new Vector3(0f, 0.5f, 0f));
            float elapsed = 0f;
            while (elapsed < 1.8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(ActiveLabels(pool), Is.Empty);
            pool.Show(new Vector3(5f, 0.5f, 5f));
            Text label = SingleActiveLabel(pool);
            Assert.That(label.transform.position, Is.EqualTo(new Vector3(5f, 0.5f, 4f)));
        }

        private HudView CreateHudView(Text counterText)
        {
            var go = Track(new GameObject("HUD"));
            go.SetActive(false);
            var view = go.AddComponent<HudView>();
            SetReference(view, "_counterText", counterText);
            go.SetActive(true);
            return view;
        }

        private TastyLabelPool CreatePool(out Text template, int poolSize = 4)
        {
            var canvasGo = Track(new GameObject("Tasty canvas"));
            canvasGo.SetActive(false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            template = CreateText(canvasGo.transform, "Tasty template");
            var pool = canvasGo.AddComponent<TastyLabelPool>();
            SetReference(pool, "_labelTemplate", template);
            var serialized = new SerializedObject(pool);
            serialized.FindProperty("_poolSize").intValue = poolSize;
            serialized.FindProperty("_belowOffset").vector3Value = new Vector3(0f, 0f, -1f);
            serialized.FindProperty("_drift").vector3Value = new Vector3(0f, 0f, 0.6f);
            serialized.FindProperty("_duration").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            canvasGo.SetActive(true);
            return pool;
        }

        private Text CreateText(Transform parent, string name)
        {
            var go = Track(new GameObject(name));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return go.AddComponent<Text>();
        }

        private static Text SingleActiveLabel(TastyLabelPool pool)
        {
            List<Text> active = ActiveLabels(pool);
            Assert.That(active.Count, Is.EqualTo(1), "Exactly one pooled label must be active.");
            return active[0];
        }

        private static List<Text> ActiveLabels(TastyLabelPool pool)
        {
            var active = new List<Text>();
            foreach (Text label in pool.GetComponentsInChildren<Text>())
            {
                if (label.gameObject.activeSelf)
                {
                    active.Add(label);
                }
            }

            return active;
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
