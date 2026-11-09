using System;
using static ZooWorld.Tests.SerializedFields;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Motion;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class AnimalCatalogTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private AnimalCatalog _catalog;
        private AnimalDefinition _first;
        private AnimalDefinition _second;

        [SetUp]
        public void SetUp()
        {
            _catalog = Asset<AnimalCatalog>("Fixture Catalog");
            _first = Definition("first");
            _second = Definition("second");
            SetArray(_catalog, "_definitions", _first, _second);
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

        [Test, Description("Валидный каталог допускает общие профили и вид с нулевым весом")]
        public void ValidCatalogAllowsSharedProfilesAndZeroWeightEntry()
        {
            SetReference(_second, "_interactionProfile", _first.InteractionProfile);
            SetReference(_second, "_movementProfile", _first.MovementProfile);
            SetFloat(_second, "_spawnWeight", 0);
            Assert.DoesNotThrow(() => _catalog.Validate());
        }

        [TestCase(""), TestCase(" ")]
        [Description("Пустой ID вида диагностируется")]
        public void EmptySpeciesIdIsDiagnosed(string id)
        {
            SetString(_second, "_id", id);
            AssertInvalid("second");
        }

        [Test, Description("Дублирующийся ID вида диагностируется")]
        public void DuplicateSpeciesIdIsDiagnosed()
        {
            SetString(_second, "_id", "first");
            AssertInvalid("first");
        }

        [Test, Description("Null-запись сообщает свой индекс")]
        public void NullEntryReportsItsIndex()
        {
            SetArray(_catalog, "_definitions", _first, null);
            AssertInvalid("1");
        }

        [Test, Description("Пустой каталог отклоняется")]
        public void EmptyCatalogIsRejected()
        {
            SetArray(_catalog, "_definitions");
            Assert.Throws<InvalidOperationException>(() => _catalog.Validate());
        }

        [TestCase(-1f), TestCase(float.NaN), TestCase(float.PositiveInfinity)]
        [Description("Невалидный вес сообщает вид")]
        public void InvalidWeightReportsSpecies(float weight)
        {
            SetFloat(_second, "_spawnWeight", weight);
            AssertInvalid("second");
        }

        [Test, Description("Нулевой суммарный вес отклоняется")]
        public void ZeroTotalWeightIsRejected()
        {
            SetFloat(_first, "_spawnWeight", 0);
            SetFloat(_second, "_spawnWeight", 0);
            Assert.Throws<InvalidOperationException>(() => _catalog.Validate());
        }

        [TestCase("_prefab"), TestCase("_movementProfile"), TestCase("_interactionProfile")]
        [Description("Отсутствующая ссылка сообщает вид")]
        public void MissingReferenceReportsSpecies(string field)
        {
            SetReference(_second, field, null);
            AssertInvalid("second");
        }

        [Test, Description("Невалидные настройки движения сообщают профиль")]
        public void InvalidMovementSettingsReportProfile()
        {
            SetFloat(_second.MovementProfile, "_speed", 0);
            AssertInvalid(_second.MovementProfile.name);
        }

        [Test, Description("Отсутствующая категория взаимодействия сообщает профиль")]
        public void MissingInteractionCategoryReportsProfile()
        {
            SetReference(_second.InteractionProfile, "_category", null);
            AssertInvalid(_second.InteractionProfile.name);
        }

        [Test, Description("Разные ассеты категорий с одним ID отклоняются")]
        public void DifferentCategoryAssetsWithSameIdAreRejected()
        {
            InteractionCategory duplicate = Category("first-category");
            SetReference(_second.InteractionProfile, "_category", duplicate);
            AssertInvalid("first-category");
        }

        [Test, Description("Дубль ID среди съедобных категорий отклоняется")]
        public void DuplicateIdInEdibleOnlyCategoriesIsRejected()
        {
            SetArray(_first.InteractionProfile, "_edibleCategories", Category("edible-only"));
            SetArray(_second.InteractionProfile, "_edibleCategories", Category("edible-only"));
            AssertInvalid("edible-only");
        }

        [Test, Description("Дубль ID между своей и съедобной категорией отклоняется")]
        public void DuplicateIdBetweenOwnAndEdibleCategoryIsRejected()
        {
            SetArray(_second.InteractionProfile, "_edibleCategories", Category("first-category"));
            AssertInvalid("first-category");
        }

        [Test, Description("Один ассет категории допускается в профилях и списках съедобного")]
        public void SameCategoryAssetCanBeReferencedAcrossProfilesAndEdibleLists()
        {
            InteractionCategory shared = Category("shared");
            SetReference(_first.InteractionProfile, "_category", shared);
            SetReference(_second.InteractionProfile, "_category", shared);
            SetArray(_first.InteractionProfile, "_edibleCategories", shared);
            SetArray(_second.InteractionProfile, "_edibleCategories", shared);
            Assert.DoesNotThrow(() => _catalog.Validate());
        }

        [TestCase(""), TestCase(" ")]
        [Description("Пустой ID съедобной категории отклоняется")]
        public void EmptyEdibleCategoryIdIsRejected(string id)
        {
            SetArray(_second.InteractionProfile, "_edibleCategories", Category(id));
            AssertInvalid(_second.InteractionProfile.name);
        }

        [Test, Description("Null съедобной категории отклоняется")]
        public void NullEdibleCategoryIsRejected()
        {
            SetArray(_second.InteractionProfile, "_edibleCategories", new Object[] { null });
            AssertInvalid(_second.InteractionProfile.name);
        }

        [TestCase(typeof(Rigidbody)), TestCase(typeof(BoxCollider)), TestCase(typeof(CatalogTestMotor))]
        [TestCase(typeof(AnimalContactRelay))]
        [Description("Отсутствующий компонент prefab'а сообщает вид")]
        public void MissingPrefabComponentReportsSpecies(Type componentType)
        {
            Object.DestroyImmediate(_second.Prefab.GetComponent(componentType));
            AssertInvalid("second");
        }

        [Test, Description("Коллайдер только на детях не удовлетворяет требованиям prefab'а")]
        public void ChildOnlyColliderDoesNotSatisfyPrefabRequirements()
        {
            Object.DestroyImmediate(_second.Prefab.GetComponent<Collider>());
            var child = new GameObject("Child collider");
            _objects.Add(child);
            child.transform.SetParent(_second.Prefab.transform);
            child.AddComponent<BoxCollider>();
            AssertInvalid("second");
        }

        [Test, Description("Новые определения получают различные GUID ID")]
        public void NewDefinitionsReceiveDistinctGuidIds()
        {
            var first = Asset<AnimalDefinition>("Generated First");
            var second = Asset<AnimalDefinition>("Generated Second");
            Assert.That(Guid.TryParseExact(first.Id, "N", out Guid firstId), Is.True);
            Assert.That(Guid.TryParseExact(second.Id, "N", out Guid secondId), Is.True);
            Assert.That(firstId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(secondId, Is.Not.EqualTo(firstId));
        }

        [Test, Description("Сериализованный ID вида перекрывает сгенерированный")]
        public void SerializedSpeciesIdOverridesGeneratedDefault()
        {
            string serialized = EditorJsonUtility.ToJson(_first);
            var restored = Asset<AnimalDefinition>("Restored");
            EditorJsonUtility.FromJsonOverwrite(serialized, restored);
            Assert.That(restored.Id, Is.EqualTo("first"));
        }

        [Test, Description("Валидация сохраняет существующие ID видов")]
        public void ValidationPreservesExistingSpeciesIds()
        {
            _catalog.Validate();
            _catalog.Validate();
            Assert.That(_first.Id, Is.EqualTo("first"));
            Assert.That(_second.Id, Is.EqualTo("second"));
        }

        [Test, Description("Валидация не чинит дублирующиеся ID")]
        public void ValidationDoesNotRepairDuplicateIds()
        {
            SetString(_second, "_id", "first");
            AssertInvalid("first");
            Assert.That(_second.Id, Is.EqualTo("first"));
        }

        [Test, Description("Большие конечные веса не переполняют проверку суммы")]
        public void LargeFiniteWeightsDoNotOverflowValidationTotal()
        {
            SetFloat(_first, "_spawnWeight", float.MaxValue);
            SetFloat(_second, "_spawnWeight", float.MaxValue);
            Assert.DoesNotThrow(() => _catalog.Validate());
        }

        [Test, Description("Диагностика дублирующейся категории называет оба профиля")]
        public void DuplicateCategoryDiagnosticIdentifiesBothProfiles()
        {
            SetArray(_first.InteractionProfile, "_edibleCategories", Category("duplicate"));
            SetArray(_second.InteractionProfile, "_edibleCategories", Category("duplicate"));
            var error = Assert.Throws<InvalidOperationException>(() => _catalog.Validate());
            Assert.That(error.Message, Does.Contain("duplicate"));
            Assert.That(error.Message, Does.Contain(_first.InteractionProfile.name));
            Assert.That(error.Message, Does.Contain(_second.InteractionProfile.name));
        }

        private void AssertInvalid(string source)
        {
            var error = Assert.Throws<InvalidOperationException>(() => _catalog.Validate());
            Assert.That(error.Message, Does.Contain(source));
        }

        [Test, Description("Каталог валидирует тысячу определений без ошибок")]
        public void CatalogValidatesAThousandSpeciesDefinitions()
        {
            var definitions = new AnimalDefinition[1000];
            for (var i = 0; i < definitions.Length; i++)
            {
                definitions[i] = Definition("species-" + i);
            }

            var catalog = Asset<AnimalCatalog>("Big Catalog");
            var serialized = new SerializedObject(catalog);
            SerializedProperty array = serialized.FindProperty("_definitions");
            array.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.DoesNotThrow(() => catalog.Validate());
        }

        private AnimalDefinition Definition(string id)
        {
            var definition = Asset<AnimalDefinition>(id);
            var root = new GameObject(id + " Fixture");
            root.SetActive(false);
            _objects.Add(root);
            var animal = root.AddComponent<Animal>();
            root.AddComponent<Rigidbody>();
            root.AddComponent<BoxCollider>();
            var motor = root.AddComponent<CatalogTestMotor>();
            root.AddComponent<AnimalContactRelay>();
            Assert.That(motor, Is.Not.Null, "Fixture requires an attachable test-only motor.");
            var movement = Asset<LinearMovementProfile>(id + " Movement");
            var interaction = Asset<InteractionProfile>(id + " Interaction");
            SetReference(interaction, "_category", Category(id + "-category"));
            SetString(definition, "_id", id);
            SetString(definition, "_displayName", id);
            SetReference(definition, "_prefab", animal);
            SetReference(definition, "_movementProfile", movement);
            SetReference(definition, "_interactionProfile", interaction);
            return definition;
        }

        private InteractionCategory Category(string id)
        {
            var category = Asset<InteractionCategory>("Category " + id);
            SetString(category, "_id", id);
            return category;
        }

        private T Asset<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            _objects.Add(asset);
            return asset;
        }
    }
}
