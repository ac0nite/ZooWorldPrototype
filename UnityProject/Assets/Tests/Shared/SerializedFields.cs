#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ZooWorld.Tests
{
    public static class SerializedFields
    {
        public static void SetField(UnityEngine.Object target, string field,
            Action<SerializedProperty> change)
        {
            var serialized = new SerializedObject(target);
            change(serialized.FindProperty(field));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(UnityEngine.Object target, string field,
            Action<SerializedProperty> change)
        {
            SetField(target, field, change);
        }

        public static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SetField(target, field, property => property.objectReferenceValue = value);
        }

        public static void SetString(UnityEngine.Object target, string field, string value)
        {
            SetField(target, field, property => property.stringValue = value);
        }

        public static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            SetField(target, field, property => property.floatValue = value);
        }

        public static void SetInt(UnityEngine.Object target, string field, int value)
        {
            SetField(target, field, property => property.intValue = value);
        }

        public static void SetEnum(UnityEngine.Object target, string field, int value)
        {
            SetField(target, field, property => property.intValue = value);
        }

        public static void Set(UnityEngine.Object target, string field, float value)
        {
            SetField(target, field, property => property.floatValue = value);
        }

        public static void SetArray(UnityEngine.Object target, string field, params UnityEngine.Object[] values)
        {
            SetField(target, field, property =>
            {
                property.arraySize = values.Length;
                for (var i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            });
        }
    }
}
#endif
