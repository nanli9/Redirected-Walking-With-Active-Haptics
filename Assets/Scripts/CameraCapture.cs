using System.IO;
using UnityEditor;
using UnityEngine;

public class CameraCapture : EditorWindow
{
    Camera _camera;
    string fileName = "output.png";

    [MenuItem("Custom Tools/Capture Screenshot")]
    public static void ShowWindow()
    {
        GetWindow<CameraCapture>("Capture Screenshot");
    }

    private void OnGUI()
    {
        GUILayout.Label("Capture Screenshot Settings", EditorStyles.boldLabel);

        _camera = (Camera)EditorGUILayout.ObjectField("Camera", _camera, typeof(Camera), true);
        fileName = EditorGUILayout.TextField("Output File Name", fileName);

        if (GUILayout.Button("Capture"))
        {
            Capture();
        }
    }

    void Capture()
    {
        if (_camera == null)
        {
            Debug.LogError("Camera is not selected.");
            return;
        }

        RenderTexture activeRenderTexture = RenderTexture.active;
        Debug.Log(_camera);
        RenderTexture.active = _camera.targetTexture;

        _camera.Render();

        Texture2D image = new Texture2D(_camera.targetTexture.width, _camera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, _camera.targetTexture.width, _camera.targetTexture.height), 0, 0);
        image.Apply();
        RenderTexture.active = activeRenderTexture;

        byte[] bytes = image.EncodeToPNG();
        Object.DestroyImmediate(image);

        Debug.Log(bytes);

        // File.WriteAllBytes(Path.Combine(Application.persistentDataPath, fileName), bytes);
        File.WriteAllBytes(Path.Combine(Application.dataPath, fileName), bytes);

    }
}
