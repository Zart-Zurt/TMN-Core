#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Core.Utils
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                EditorGUI.BeginProperty(position, label, property);

                var sortingLayerNames = GetSortingLayerNames();
                var sortingLayerID = property.intValue;
                var sortingLayerIndex = GetSortingLayerIndex(sortingLayerID);

                sortingLayerIndex = EditorGUI.Popup(position, label.text, sortingLayerIndex, sortingLayerNames);

                property.intValue = SortingLayer.NameToID(sortingLayerNames[sortingLayerIndex]);

                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use [SortingLayer] with int.");
            }
        }

        private string[] GetSortingLayerNames()
        {
            return SortingLayer.layers.Select(layer => layer.name).ToArray();
        }

        private int GetSortingLayerIndex(int sortingLayerID)
        {
            var sortingLayerNames = GetSortingLayerNames();
            for (int i = 0; i < sortingLayerNames.Length; i++)
            {
                if (SortingLayer.NameToID(sortingLayerNames[i]) == sortingLayerID)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
#endif