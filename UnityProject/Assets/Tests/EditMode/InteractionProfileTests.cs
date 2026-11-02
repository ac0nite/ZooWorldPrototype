using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZooWorld.Definitions;
using ZooWorld.Interaction;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class InteractionProfileTests
    {
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test, Description("Начальные профили дают разрешения задания")]
        public void InitialProfilesProduceTaskPermissions()
        {
            InteractionCategory preyCategory = Category("prey");
            InteractionCategory predatorCategory = Category("predator");
            InteractionData prey = Profile(preyCategory).CreateSnapshot();
            InteractionData predator = Profile(predatorCategory, preyCategory, predatorCategory).CreateSnapshot();
            Assert.That(prey.CanConsume(prey), Is.False);
            Assert.That(prey.CanConsume(predator), Is.False);
            Assert.That(predator.CanConsume(prey), Is.True);
            Assert.That(predator.CanConsume(predator), Is.True);
        }

        [Test, Description("Сохранённые ассеты воспроизводят разрешения задания")]
        public void SavedInitialAssetsReproduceTaskPermissions()
        {
            var frog = AssetDatabase.LoadAssetAtPath<InteractionProfile>(
                "Assets/Settings/Interactions/FrogInteraction.asset");
            var snake = AssetDatabase.LoadAssetAtPath<InteractionProfile>(
                "Assets/Settings/Interactions/SnakeInteraction.asset");
            Assert.That(frog, Is.Not.Null);
            Assert.That(snake, Is.Not.Null);
            InteractionData prey = frog.CreateSnapshot();
            InteractionData predator = snake.CreateSnapshot();
            Assert.That(prey.CategoryId, Is.EqualTo("prey"));
            Assert.That(predator.CategoryId, Is.EqualTo("predator"));
            Assert.That(prey.CanConsume(prey), Is.False);
            Assert.That(prey.CanConsume(predator), Is.False);
            Assert.That(predator.CanConsume(prey), Is.True);
            Assert.That(predator.CanConsume(predator), Is.True);
        }

        [Test, Description("Снимок переживает поздние изменения исходных ассетов")]
        public void SnapshotSurvivesLaterChangesToSourceAssets()
        {
            InteractionCategory preyCategory = Category("prey");
            InteractionProfile profile = Profile(Category("predator"), preyCategory);
            InteractionData snapshot = profile.CreateSnapshot();
            SetId(preyCategory, "changed");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("_category").objectReferenceValue = preyCategory;
            serialized.FindProperty("_edibleCategories").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(snapshot.CategoryId, Is.EqualTo("predator"));
            Assert.That(snapshot.CanConsume(new InteractionData("prey", Array.Empty<string>())), Is.True);
            Assert.That(snapshot.CanConsume(new InteractionData("changed", Array.Empty<string>())), Is.False);
        }

        [Test, Description("Отсутствующая категория сообщает имя профиля")]
        public void MissingCategoryReportsProfileName()
        {
            InteractionProfile profile = Profile(null);
            var error = Assert.Throws<InvalidOperationException>(() => profile.CreateSnapshot());
            Assert.That(error.Message, Does.Contain("Fixture Profile"));
        }

        [TestCase(null), TestCase(""), TestCase(" ")]
        [Description("Пустые ID категорий диагностируются")]
        public void EmptyCategoryIdsAreDiagnosed(string id)
        {
            InteractionProfile profile = Profile(Category(id));
            Assert.Throws<InvalidOperationException>(() => profile.CreateSnapshot());
        }

        [Test, Description("Null/невалидная съедобная запись диагностируется с индексом")]
        public void NullOrInvalidEdibleEntryIsDiagnosedWithIndex()
        {
            InteractionCategory category = Category("valid");
            InteractionProfile nullEntry = Profile(category, new InteractionCategory[] { null });
            Assert.That(Assert.Throws<InvalidOperationException>(() => nullEntry.CreateSnapshot()).Message,
                Does.Contain("index 0"));
            InteractionProfile emptyId = Profile(category, Category(" "));
            Assert.Throws<InvalidOperationException>(() => emptyId.CreateSnapshot());
        }

        private InteractionCategory Category(string id)
        {
            var category = ScriptableObject.CreateInstance<InteractionCategory>();
            _assets.Add(category);
            SetId(category, id);
            return category;
        }

        private static void SetId(InteractionCategory category, string id)
        {
            var serialized = new SerializedObject(category);
            serialized.FindProperty("_id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private InteractionProfile Profile(InteractionCategory category, params InteractionCategory[] edible)
        {
            var profile = ScriptableObject.CreateInstance<InteractionProfile>();
            profile.name = "Fixture Profile";
            _assets.Add(profile);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("_category").objectReferenceValue = category;
            SerializedProperty array = serialized.FindProperty("_edibleCategories");
            array.arraySize = edible.Length;
            for (var i = 0; i < edible.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = edible[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }
    }
}
