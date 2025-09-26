using UnityEngine;

public class CurvedLine : MonoBehaviour
{
    public Vector3 startPoint = Vector3.zero; // Starting point in world space
    public float curveRadius = 20f;           // Radius of the curve (larger = less curved)
    public float arcLength = 10f;             // Total arc length in meters
    public int pointCount = 20;               // Number of segments in the curve

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        DrawCurvedLine();
    }

    void DrawCurvedLine()
    {
        Vector3[] positions = new Vector3[pointCount];

        float angleTotal = arcLength / curveRadius;       // arcLength = radius * angle (radians)
        float angleStep = angleTotal / (pointCount - 1);

        for (int i = 0; i < pointCount; i++)
        {
            float theta = angleStep * i;

            float z = Mathf.Sin(theta) * curveRadius;
            float x = Mathf.Cos(theta) * curveRadius;

            // Align with curve starting at 0
            z -= Mathf.Sin(0) * curveRadius;
            x -= Mathf.Cos(0) * curveRadius;

            // Now apply the start point offset and axis swap
            Vector3 point = new Vector3(x, 0f, z) + startPoint;
            positions[i] = point;
        }

        lineRenderer.positionCount = pointCount;
        lineRenderer.SetPositions(positions);
    }
}
