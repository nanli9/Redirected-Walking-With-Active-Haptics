using UnityEngine;
using System.Collections;
using UnityEngine.XR;

public class ArduinoControlManager : MonoBehaviour
{
    [Header("Hand Settings")]
    public bool isLeft = false;
    public bool isRight = true;   
    private bool leftWasPressed = false;
    private bool rightWasPressed = false;
    private int[] leftERMs = {2,4};
    private int[] rightERMs = {1,3};

    [Header("Vibration Parameters")]
    public float frequency = 80f;
    public float amplitude = 200f;
    public float amplitudeChangeThreshold = 5f;

    [Header("Timing Settings")]
    public float sendInterval = 2.0f;
    private float timeSinceLastSend = 0f;

    private bool isVibrating = false;
    private bool wasVibrating = false;
    private float lastAmplitudeSent = -1f;

    void Update()
    {
        timeSinceLastSend += Time.deltaTime;
        if (timeSinceLastSend >= sendInterval)
        {
            timeSinceLastSend = 0f;

            if (ArduinoConnectionManager.Instance.isConnected())
            {
                // --- RIGHT HAND ---
                InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPressed))
                {
                    if ((rightPressed && !rightWasPressed))
                    {
                        isRight = !isRight;
                        Debug.Log($"Right flag toggled: {isRight}");
                        isVibrating = isRight; // vibration tied to right toggle
                        ToggleVibration();
                    }
                    rightWasPressed = rightPressed;
                }

                // --- LEFT HAND ---
                InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                if (leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPressed))
                {
                    if (leftPressed && !leftWasPressed)
                    {
                        isLeft = !isLeft;
                        Debug.Log($"Left flag toggled: {isLeft}");
                        isVibrating = isLeft; // vibration tied to left toggle
                        ToggleVibration();
                    }
                    leftWasPressed = leftPressed;
                }

                wasVibrating = isVibrating;

                if (isVibrating)
                {
                    float ampDelta = Mathf.Abs(amplitude - lastAmplitudeSent);
                    if (ampDelta >= amplitudeChangeThreshold)
                    {
                        Debug.Log($"Amplitude changed ({lastAmplitudeSent} -> {amplitude}), updating Arduino");

                        if (isLeft)
                        {
                            foreach (int id in leftERMs)
                                SendToArduino(id, frequency, amplitude);
                        }
                        if (isRight)
                        {
                            foreach (int id in rightERMs)
                                SendToArduino(id, frequency, amplitude);
                        }

                        lastAmplitudeSent = amplitude;
                    }
                }

            }
        }
    }

    private void SendToArduino(int id, float freq, float amp)
    {
        ArduinoConnectionManager.Instance.SendMessageToRightHandArduino(id, freq, amp);
    }

    private void ToggleVibration()
    {
        if (isVibrating)
        {
            Debug.Log("Vibration ON");
            if (isLeft)
            {
                foreach (int id in leftERMs)
                    SendToArduino(id, frequency, amplitude);
            }
            if (isRight)
            {
                foreach (int id in rightERMs)
                    SendToArduino(id, frequency, amplitude);
            }
        }
        else
        {
            Debug.Log("Vibration OFF");
            if (!isLeft)
            {
                foreach (int id in leftERMs)
                    SendToArduino(id, 0f, 0f);
            }
            if (!isRight)
            {
                foreach (int id in rightERMs)
                    SendToArduino(id, 0f, 0f);
            }
        }
    }
}
