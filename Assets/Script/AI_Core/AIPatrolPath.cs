using UnityEngine;

public class AIPatrolPath : MonoBehaviour
{
    [Header("--- PATROL WAYPOINTS ---")]
    public Transform[] waypoints;

    public int WaypointCount => waypoints != null ? waypoints.Length : 0;

    public Vector3 GetWaypointPosition(int index)
    {
        if (waypoints == null || waypoints.Length == 0 || index < 0 || index >= waypoints.Length)
            return transform.position;

        return waypoints[index].position;
    }

    // Vẽ đường đi nối các điểm trên cửa sổ Scene để thiết kế map cực nhàn
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // Vẽ khối cầu tại mỗi điểm waypoint
            Gizmos.DrawSphere(waypoints[i].position, 0.4f);

            // Vẽ đường thẳng nối điểm hiện tại với điểm tiếp theo
            int nextIndex = (i + 1) % waypoints.Length;
            if (waypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
            }
        }
    }
}