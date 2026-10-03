using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
    [InitializeOnLoad]
    public static class MiddleClickToggle
    {
        static MiddleClickToggle()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 2 && selectionRect.Contains(e.mousePosition))
            {
                var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

                if (obj != null)
                {
                    Undo.RecordObject(obj, "Toggle Active State");
                    obj.SetActive(!obj.activeSelf);
                    e.Use();
                }
            }
        }
    }
}
