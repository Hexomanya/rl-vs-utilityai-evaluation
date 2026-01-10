using _General.Custom_Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.PropertyDrawers.Custom_Attributes
{
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public class RequiredAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            RequiredAttribute required = attribute as RequiredAttribute;
            VisualElement container = new VisualElement();

            PropertyField field = new PropertyField(property);
            container.Add(field);

            HelpBox errorMessageBox = new HelpBox("", HelpBoxMessageType.Error);
            errorMessageBox.style.display = DisplayStyle.None;
            container.Add(errorMessageBox);

            field.RegisterValueChangeCallback(_ =>
            {
                bool isValid = RequiredAttributeDrawer.ValidateField(property);

                if(!isValid)
                {
                    string errorMessage = property.name + " is required!";

                    if(required != null && !string.IsNullOrEmpty(required.ErrorMessage))
                    {
                        errorMessage = required.ErrorMessage;
                    }
                    errorMessageBox.text = errorMessage;
                    errorMessageBox.style.display = DisplayStyle.Flex;
                }
                else
                {
                    // Hide the error message if valid
                    errorMessageBox.style.display = DisplayStyle.None;
                }
            });

            return container;
        }


        private static bool ValidateField(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    return !string.IsNullOrEmpty(property.stringValue);

                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue is not null;

                case SerializedPropertyType.Generic:
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Hash128:
                case SerializedPropertyType.RenderingLayerMask:
                default:
                    Debug.LogWarning("[Required] attribute does not work with this type!");
                    return true; // We assume it's valid for unsupported types
            }
        }
    }
}
