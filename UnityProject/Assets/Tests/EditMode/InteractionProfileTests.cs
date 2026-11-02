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
        readonly List<ScriptableObject> assets = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [Test, Category("AC03"), Category("AC04")]
        public void InitialProfilesProduceTaskPermissions()
        {
            var preyCategory = Category("prey");
            var predatorCategory = Category("predator");
            var prey = Profile(preyCategory).CreateSnapshot();
            var predator = Profile(predatorCategory, preyCategory, predatorCategory).CreateSnapshot();
            Assert.That(prey.CanConsume(prey), Is.False);
            Assert.That(prey.CanConsume(predator), Is.False);
            Assert.That(predator.CanConsume(prey), Is.True);
            Assert.That(predator.CanConsume(predator), Is.True);
        }

        [Test, Category("AC03"), Category("AC04")]
        public void SavedInitialAssetsReproduceTaskPermissions()
        {
            var frog = AssetDatabase.LoadAssetAtPath<InteractionProfile>(
                "Assets/Settings/Interactions/FrogInteraction.asset");
            var snake = AssetDatabase.LoadAssetAtPath<InteractionProfile>(
                "Assets/Settings/Interactions/SnakeInteraction.asset");
            Assert.That(frog, Is.Not.Null);
            Assert.That(snake, Is.Not.Null);
            var prey = frog.CreateSnapshot();
            var predator = snake.CreateSnapshot();
            Assert.That(prey.CategoryId, Is.EqualTo("prey"));
            Assert.That(predator.CategoryId, Is.EqualTo("predator"));
            Assert.That(prey.CanConsume(prey), Is.False);
            Assert.That(prey.CanConsume(predator), Is.False);
            Assert.That(predator.CanConsume(prey), Is.True);
            Assert.That(predator.CanConsume(predator), Is.True);
        }

        [Test, Category("AC09")]
        public void SnapshotSurvivesLaterChangesToSourceAssets()
        {
            var preyCategory = Category("prey");
            var profile = Profile(Category("predator"), preyCategory);
            var snapshot = profile.CreateSnapshot();
            SetId(preyCategory, "changed");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("category").objectReferenceValue = preyCategory;
            serialized.FindProperty("edibleCategories").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(snapshot.CategoryId, Is.EqualTo("predator"));
            Assert.That(snapshot.CanConsume(new InteractionData("prey", Array.Empty<string>())), Is.True);
            Assert.That(snapshot.CanConsume(new InteractionData("changed", Array.Empty<string>())), Is.False);
        }

        [Test, Category("AC08")]
        public void MissingCategoryReportsProfileName()
        {
            var profile = Profile(null);
            var error = Assert.Throws<InvalidOperationException>(() => profile.CreateSnapshot());
            Assert.That(error.Message, Does.Contain("Fixture Profile"));
        }

        [TestCase(null), TestCase(""), TestCase(" "), Category("AC08")]
        public void EmptyCategoryIdsAreDiagnosed(string id)
        {
            var profile = Profile(Category(id));
            Assert.Throws<InvalidOperationException>(() => profile.CreateSnapshot());
        }

        [Test, Category("AC08")]
        public void NullOrInvalidEdibleEntryIsDiagnosedWithIndex()
        {
            var category = Category("valid");
            var nullEntry = Profile(category, new InteractionCategory[] { null });
            Assert.That(Assert.Throws<InvalidOperationException>(() => nullEntry.CreateSnapshot()).Message,
                Does.Contain("index 0"));
            var emptyId = Profile(category, Category(" "));
            Assert.Throws<InvalidOperationException>(() => emptyId.CreateSnapshot());
        }

        InteractionCategory Category(string id)
        {
            var category = ScriptableObject.CreateInstance<InteractionCategory>();
            assets.Add(category);
            SetId(category, id);
            return category;
        }

        static void SetId(InteractionCategory category, string id)
        {
            var serialized = new SerializedObject(category);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        InteractionProfile Profile(InteractionCategory category, params InteractionCategory[] edible)
        {
            var profile = ScriptableObject.CreateInstance<InteractionProfile>();
            profile.name = "Fixture Profile";
            assets.Add(profile);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("category").objectReferenceValue = category;
            var array = serialized.FindProperty("edibleCategories");
            array.arraySize = edible.Length;
            for (var i = 0; i < edible.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = edible[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }
    }
}
