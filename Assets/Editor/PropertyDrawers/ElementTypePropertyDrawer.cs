using System;
using System.Collections.Generic;
using System.Linq;
using SimpleSkills;
using UnityEditor;
using UnityEngine;


//CODE GENERATED WITH CLAUDE 4.5
namespace Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(Element), true)]
    public class ElementDrawer : PropertyDrawer
    {
        private List<Type> _elementTypes;
        private string[] _typeNames;

        private void Init()
        {
            if (_elementTypes != null) return;

            // Find all non-abstract subclasses of Element
            Type baseType = typeof(Element);
            _elementTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            _typeNames = _elementTypes.Select(t => t.Name).ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            this.Init();

            EditorGUI.BeginProperty(position, label, property);

            Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight + 2,
                position.width,
                EditorGUI.GetPropertyHeight(property, true) - EditorGUIUtility.singleLineHeight - 2
            );

            // Current type
            Type currentType = property.managedReferenceValue?.GetType();
            int currentIndex = currentType != null ? _elementTypes.IndexOf(currentType) : -1;

            // Dropdown
            int selectedIndex = EditorGUI.Popup(typeRect, "Element Type", currentIndex, _typeNames);

            // If user changed the type
            if (selectedIndex != currentIndex && selectedIndex >= 0)
            {
                Type newType = _elementTypes[selectedIndex];
                property.managedReferenceValue = Activator.CreateInstance(newType);
            }

            // Draw inner fields
            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(propertyRect, property, label, true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
