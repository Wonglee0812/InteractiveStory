using System.Collections.Generic;
using UnityEngine;

public class HelicopterWaypoint : MonoBehaviour
{
    [Header("--- MOVEMENT SETTINGS ---")]
    [Tooltip("VI: Tốc độ bay của trực thăng.\nEN: Flight speed of the helicopter.")]
    public float moveSpeed = 15f;

    [Tooltip("VI: Tốc độ xoay mũi máy bay hướng về điểm đến.\nEN: Speed of rotating the helicopter nose towards destination.")]
    public float turnSpeed = 5f;

    [Tooltip("VI: Tick để máy bay luôn giữ thăng bằng ngang, không cắm mặt xuống đất khi hạ cánh.\nEN: Check to keep the helicopter level, preventing it from nose-diving when landing.")]
    public bool keepLevelFlight = true;

    [Header("--- WAYPOINTS ---")]
    [Tooltip("VI: Kéo thả các điểm Transform mốc (Tọa độ) vào đây.\nEN: Drag and drop target Transform points (Coordinates) here.")]
    public List<Transform> waypoints;

    private int _currentWaypointIndex = 0;
    private bool _isMoving = false;

    private void OnEnable()
    {
        // VI: Tự động bay ngay khi Object này được SetActive(true)
        // EN: Automatically fly as soon as this Object is SetActive(true)
        if (waypoints.Count > 0)
        {
            _isMoving = true;
        }
    }

    private void Update()
    {
        if (!_isMoving || waypoints.Count == 0) return;

        Transform targetWaypoint = waypoints[_currentWaypointIndex];

        // VI: Tiến về phía tọa độ đích
        // EN: Move towards the target coordinate
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, moveSpeed * Time.deltaTime);

        // VI: Tính toán hướng đi
        // EN: Calculate the direction
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;

        // VI: KHÓA TRỤC Y - Bắt máy bay luôn nhìn thẳng song song với mặt đất
        // EN: LOCK Y AXIS - Force the helicopter to always look parallel to the ground
        if (keepLevelFlight)
        {
            direction.y = 0;
        }

        // VI: Mượt mà xoay mũi máy bay (Bỏ qua nếu hướng đi là Vector Zero khi bay thẳng đứng xuống)
        // EN: Smoothly rotate the nose (Ignore if the direction is Vector Zero when flying straight down)
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        // VI: Kiểm tra xem đã chạm đến mốc chưa (sai số 0.1f)
        // EN: Check if the waypoint is reached (0.1f margin)
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            _currentWaypointIndex++;

            // VI: Nếu đã bay hết danh sách điểm mốc thì dừng lại lơ lửng
            // EN: If all waypoints in the list are reached, stop and hover
            if (_currentWaypointIndex >= waypoints.Count)
            {
                _isMoving = false;
            }
        }
    }
}