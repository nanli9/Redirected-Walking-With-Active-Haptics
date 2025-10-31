using UnityEngine;
using System.Collections;
using UnityEngine.XR;

public class ArduinoControlManager : MonoBehaviour
{
    [Header("Hand Settings")]
    public bool isLeft = false;
    public bool isRight = true;   
    //public int idERM = 1;     // ERM ID to send to (1–4)
    private bool leftWasPressed = false; //A button is pressed on Left
    private bool rightWasPressed = false; //A button is pressed on Right
    private int[] leftERMs = {1,2};
    private int[] rightERMs = {3,4};

    [Header("Vibration Parameters")]
    public bool isVibrating = false;     
    private bool wasVibrating = false;     
    public float frequency = 80f;     // Frequency when vibrating
    public float amplitude = 200f;    // Amplitude when vibrating
    public float amplitudeChangeThreshold = 5f; // Minimum delta to trigger re-send

    [Header("Timing Settings")]
    public float sendInterval = 2.0f; // Seconds between sends
    private float timeSinceLastSend = 0f;
    private float lastSentAmplitude = -1f;

    [Header("Collision Settings")]
    public float minAmplitude = 30f;
    public float maxAmplitude = 60f;
    public float maxPenetratingDistance = 0.02f;
    private Vector3 initialContactPoint;
    private float filteredDistance;


    void Update()
    {
        // Throttle sending to every `sendInterval` seconds
        timeSinceLastSend += Time.deltaTime;
        if (timeSinceLastSend >= sendInterval)
        {
            timeSinceLastSend = 0f;
            // Only send if amplitude has changed enough OR reset to zero
            //if (Mathf.Abs(amplitude - lastSentAmplitude) >= amplitudeChangeThreshold)
            if (ArduinoConnectionManager.Instance.isConnected())
            {
                // --- RIGHT HAND ---
                InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPressed))
                {
                    // Toggle when A is pressed (rising edge)
                    if (rightPressed && !rightWasPressed)
                    {
                        isRight = !isRight;
                        Debug.Log($"Right flag toggled: {isRight}");
                    }
                    rightWasPressed = rightPressed;
                }

                // --- LEFT HAND ---
                InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

                if (leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPressed))
                {
                    // Toggle when X is pressed (rising edge)
                    if (leftPressed && !leftWasPressed)
                    {
                        isLeft = !isLeft;
                        Debug.Log($"Left flag toggled: {isLeft}");
                    }
                    leftWasPressed = leftPressed;
                }

                //SendToArduino(idERM,frequency, amplitude);
                if(wasVibrating != isVibrating)
                    ToggleVibration();


                wasVibrating = isVibrating;
                lastSentAmplitude = amplitude;
            }
            
        }
    }


    private void SendToArduino(int id, float freq, float amp)
    {
        /*
        if (isLeft)
        {
            ArduinoConnectionManager.Instance.SendMessageToLeftHandArduino(id, freq, amp);
        }
        if (isRight)
        {
            ArduinoConnectionManager.Instance.SendMessageToRightHandArduino(id, freq, amp);
        }
        */

        //since I am only using one Arduino
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
                {
                    SendToArduino(id, frequency, amplitude);
                }
            }
            if (isRight)
            {
                foreach (int id in rightERMs)
                {
                    SendToArduino(id, frequency, amplitude);
                }
            }
        }
        else
        {
            Debug.Log("Vibration OFF");
            if (isLeft)
            {
                foreach (int id in leftERMs)
                {
                    SendToArduino(id, 0.0f, 0.0f);
                }
            }
            if (isRight)
            {
                foreach (int id in rightERMs)
                {
                    SendToArduino(id, 0.0f, 0.0f);
                }
            }
        }

    }

}
