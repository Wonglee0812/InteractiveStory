using UnityEngine;
using UnityEditor;

public class AutoColliderTool : EditorWindow
{
    // Tạo một menu tùy chỉnh trên thanh công cụ của Unity
    // Creates a custom menu on Unity's toolbar
    [MenuItem("Tools/Auto Fit Box Colliders")]
    public static void AddCollidersToSelected()
    {
        // Lấy tất cả các object đang được quét chọn trong Hierarchy
        // Get all objects currently selected in the Hierarchy
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Vui lòng chọn ít nhất một object! / Please select at least one object!");
            return;
        }

        int addedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            // Tìm tất cả các MeshRenderer trong object này và các object con bên trong nó
            // Find all MeshRenderers in this object and its children
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer rend in renderers)
            {
                // Kiểm tra xem object này đã có Collider nào chưa để tránh việc thêm đè lên nhau
                // Check if this object already has a Collider to avoid adding duplicates
                if (rend.gameObject.GetComponent<Collider>() == null)
                {
                    // Thêm BoxCollider, sử dụng Undo để bạn có thể bấm Ctrl+Z hoàn tác nếu muốn
                    // Add BoxCollider, using Undo so you can press Ctrl+Z to revert if desired
                    Undo.AddComponent<BoxCollider>(rend.gameObject);
                    addedCount++;
                }
            }
        }

        Debug.Log($"Đã thêm thành công {addedCount} Box Colliders! / Successfully added {addedCount} Box Colliders!");
    }
}