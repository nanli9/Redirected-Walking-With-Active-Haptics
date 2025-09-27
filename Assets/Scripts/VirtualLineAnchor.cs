using UnityEngine;

public class VirtualLineAnchor : MonoBehaviour
{
    public LineRenderer leftLine;
    public LineRenderer rightLine;

    public float lineLength = 5f;   // how far forward to draw
    public float lineSpacing = 0.5f; // distance between left/right lines

    public void DrawLines(Vector3 forward, float lineLength, float lineSpacing)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 startLeft = transform.position - right * lineSpacing * 0.5f;
        Vector3 startRight = transform.position + right * lineSpacing * 0.5f;

        Vector3 endLeft = startLeft + forward * lineLength;
        Vector3 endRight = startRight + forward * lineLength;

        leftLine.positionCount = 2;
        leftLine.SetPosition(0, startLeft);
        leftLine.SetPosition(1, endLeft);

        rightLine.positionCount = 2;
        rightLine.SetPosition(0, startRight);
        rightLine.SetPosition(1, endRight);
    }
}
