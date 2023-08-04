using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public static class IListExtensions {
    /// <summary>
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts) {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}
 

public class Render : MonoBehaviour
{
    /// <summary> User(Center Eye Anchor) </summary>
    public GameObject U;
    /// <summary> Actual Hand </summary>
    public OVRHand AH;
    /// <summary> Right Hand </summary>
    public OVRHand RH;
    /// <summary> Actual Sphere </summary>
    public GameObject AS;
    /// <summary> Haptic Sphere </summary>
    public GameObject HS;    
    /// <summary> Visual Sphere </summary>
    public GameObject VS;
    /// <summary> Haptic Wall </summary>
    public GameObject HW;
    /// <summary> Visual Wall </summary>
    public GameObject VW;
    /// <summary> Visual Floor </summary>
    public GameObject VF; 
    /// <summary> Start Location Indicator </summary>
    public GameObject SL;
    /// <summary> End Location Indicator </summary>
    public GameObject EL;
    /// <summary> Straightness Radius </summary>
    private static readonly float STRAIGHT = 1000000.0f;
    /// <summary> Height Of All Walls </summary>
    private static readonly float h = 4.0f;
    /// <summary> Initial Distance </summary>
    public float d = 0.42f;
    /// <summary> Path User Required To Travel </summary>
    public float p = 5f;
    /// <summary> Allowed Radius </summary>
    private static readonly float[] _r = {5.0f, 7.0f, 10.0f, 14.0f, 19.0f, 25.0f}; 
    /// <summary> [Placeholder] Current Radius </summary>
    private float r;
    /// <summary> [Placeholder] Projected Vector From Center Of Actual Wall(AW) To User(U)</summary>
    private Vector3 V_vec;
    /// <summary> [Placeholder] Normalized Projected Vector From Center Of Actual Wall(AW) To User(U)</summary>
    private Vector3 V_norm;
    /// <summary> [Placeholder] Relative Angle from Projected Vector </summary>
    private float theta;
    /// <summary> [Placeholder] Initial Projected Vector From Center Of Actual Wall(AW) to Start Location (SL)</summary>
    private Vector3 SV;
    /// <summary> [Placeholder] Relative Angle from Initial Projected Vector</summary>
    private float stheta;
    /// <summary> [Placeholder] Travel Distance </summary>
    private float td;
    /// <summary> [Placeholder] Projected Point On The Surface Of Haptic Wall(HW) Towards User(U)</summary>
    private Vector3 P;
    /// <summary> [Placeholder] Tangent Unit Vector On The Surface Of Haptic Wall(HW) Towards User(U) </summary>
    private Vector3 T_hat;
    /// <summary> [Placeholder] Shifting Direction Vector </summary>
    private Vector3 S_vec;
    /// <summary> Remote Servo Condition Switch </summary>
    private bool WithServo;
    
    private List<Tuple<float, bool>> Cases = new List<Tuple<float, bool>>();

    public bool UseRenderPosition = true;
    public bool ViewTestingObjects = true;
    public bool ViewVisualWall = true;
    
    /**
      *  Remote Servo Connection Variables
      */
    private const string remoteIpAddress = "192.168.4.1";
    private const int remotePort = 4210;
    private const int localPort = 4210;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    /**
      *  Testing Objects Display Status Update
      */
    private void TestingObjectsViewUpdate() {
        if (ViewTestingObjects) {
            HW.layer = LayerMask.NameToLayer("Default");
            AS.layer = LayerMask.NameToLayer("Default");
            VS.layer = LayerMask.NameToLayer("Default");
            HS.layer = LayerMask.NameToLayer("Default");
        } else {
            HW.layer = LayerMask.NameToLayer("TestingObject");
            AS.layer = LayerMask.NameToLayer("TestingObject");
            VS.layer = LayerMask.NameToLayer("TestingObject");
            HS.layer = LayerMask.NameToLayer("TestingObject");
        }
    }


    /**
      *  Start is called before the first frame update
      */
    void Start()
    {
        // Create Cases
        foreach (var radius in _r)
        {
            Cases.Add(new Tuple<float, bool>(radius, true));
            Cases.Add(new Tuple<float, bool>(radius, false));
        }
        // Shuffle Cases
        Cases.Shuffle();

        // Init Environment
        Initialization();

        // Init UDP
        udpClient = new UdpClient(localPort);
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);
        Debug.Log("UDP server started");
    }

    // Update is called once per frame
    void Update()
    {
        // Testing Variable Update
        VW.SetActive(ViewVisualWall);
        TestingObjectsViewUpdate();

        // Rendering Algorithm Update
        VisionRendering();  
        HapticRendering();  // Dependent on Vision Rendering Algorithm

        if (td > p) {
            // Should have questionaire, which calls Initialization() after questionaire is done.
            // For now, just call Initialization() directly.
            Initialization();
        }
    }

    void Initialization()
    {
        // Pop one at a time
        if (Cases.Count > 0)
        {
            var c = Cases[0];
            r = c.Item1;
            WithServo = c.Item2;
            Debug.Log($"Radius: {r}, On/Off: {WithServo}");
            Cases.RemoveAt(0);
        } else {
            Debug.Log("No more case, terminating experiment");
        }
        HW.transform.localScale = new Vector3(r*2, h, r*2); 
        HW.transform.position = new Vector3(U.transform.position.x, h, U.transform.position.z) - U.transform.right * (r + d);

        /// Start and End Location Indicator Positioning
        SL.transform.position = new Vector3(U.transform.position.x, 0, U.transform.position.z);
        
        Vector3 SV = SL.transform.position-new Vector3(HW.transform.position.x,0,HW.transform.position.z);
        stheta = (Mathf.Atan2(SV.z, SV.x) + 2 * Mathf.PI) % (2 * Mathf.PI);
    }

    void VisionRendering()
    {
        /// Visual Wall and Visual Floor Rendering
        // 1. Get Projected Vector from center of Haptic Wall to position of User
        V_vec = U.transform.position - HW.transform.position;
        V_vec = new Vector3(V_vec.x, 0, V_vec.z);

        // 2. Get Projected Point by projecting Projected Vector from center of Haptic Wall in Radius magnitude.
        V_norm = V_vec / V_vec.magnitude;
        P = HW.transform.position + V_norm * r;
        P = new Vector3(P.x, 0, P.z);

        // 3. Get anti-clockwize Tangent Unit Vector on the surface of Haptic Wall at Projected Point using absolute Up direction, and Projected Vector.
        T_hat = Vector3.Cross(Vector3.up, V_norm);

        // 4. Match Quaternion of Visual Wall and Quaternion of Visual Floor to anti-clockwize Tangent Unit Vector direction using Unity Quaternion.LookRotation method. Please Expand this to actual formula instead of Unity predefinded method
        VW.transform.rotation = Quaternion.LookRotation(V_norm, Vector3.up);
        VF.transform.rotation = VW.transform.rotation;

        // 5. Get User Relative Angle from z component and x component of Project Vector in 0 to 2PI scale.
        theta = (Mathf.Atan2(V_norm.z, V_norm.x) + 2 * Mathf.PI) % (2 * Mathf.PI) - stheta;

        // 6. Get User Travel Distance from User Relative Angle multiplied by sum of Radius of Haptic Wall and Initial Distance.
        td = theta * (r + d);

        // 7. Get the Shifting Direction Vector, whcihc is oppsite to expected walking direction of User, which is same as clockwize Tangent Unit Vector, in Travel Distance Magnitude.
        S_vec = td * T_hat;

        // 8. Set Virtual Wall and Visual Floor position to where Projected Point is shifted with Shifting Direction Vector. so the user is feeling as if they are walking on straight path, event though they were walking along surface of Haptic Wall.
        VW.transform.position = P + S_vec;
        VF.transform.position = VW.transform.position;
        VW.transform.position = new Vector3(VW.transform.position.x, h/2, VW.transform.position.z);

        /// Visual Hand and Sphere Rendering
        // 1. if Visual Wall is in between Actual Left Hand and User position, then Visual Left Hand position is projected on closet point on surface of Visual Wall from Actual Left Hand position.
        // Else, Visual Left Hand stays at Actual Left Hand position.
        Vector3 AP_vec = new Vector3(AH.PointerPose.localPosition.x,0,AH.PointerPose.localPosition.z) - P;
        AH.UseRenderPosition = UseRenderPosition;
        if (Vector3.Dot(AP_vec,V_norm) > 0) {
            AH.RenderPosition = AH.PointerPose.localPosition;
            AS.transform.position = AH.PointerPose.position;
            VS.transform.position = AH.PointerPose.position;
        }
        else {
            AH.RenderPosition = AH.PointerPose.localPosition - Vector3.Dot(AP_vec,V_norm)*V_norm;    
            AS.transform.position = AH.PointerPose.position;
            VS.transform.position = AH.PointerPose.position - Vector3.Dot(AP_vec,V_norm)*V_norm;
        }

        EL.transform.position = new Vector3(HW.transform.position.x,0,HW.transform.position.z) + new Vector3(Mathf.Cos(stheta+p/(r+d)), 0, Mathf.Sin(stheta+p/(r+d))) * (r+d);
        Vector3 ELP_vec = EL.transform.position - P;
        if (Vector3.Dot(ELP_vec,V_norm) < 0) {
            EL.transform.position = EL.transform.position - (Vector3.Dot(ELP_vec,V_norm) + d) * V_norm; 
        }
    } 

    void HapticRendering() {
        /// Visual Hand and Sphere Rendering
        // 1. if Acutal Left Hand is inside Haptic Wall, then Haptic Left Hand position is projected on closet point on surface of Haptic Wall from Actual Left Hand position.
        // Else, Haptic Left Hand stays at Actual Left Hand position.
        Vector3 AW = new Vector3(AH.PointerPose.position.x,0,AH.PointerPose.position.z) - new Vector3(HW.transform.position.x,0,HW.transform.position.z);
        if (AW.magnitude > r) {
            HS.transform.position = AH.PointerPose.position;
        } else {
            HS.transform.position = new Vector3(HW.transform.position.x, AH.PointerPose.position.y, HW.transform.position.z) + AW.normalized * r;
        }
        
        // 2. After Haptic Left Hand calculation done, distance between Actual Left Hand and Haptic Left Hand will be sent to my device to display force based on its magnitude.
        SendServoPosition((HS.transform.position - AS.transform.position).magnitude);
    }

    private void SendServoPosition(float diff) {
        int calibrate = 50;
        int servoPosition = 125 + calibrate;
        if (WithServo) {
            if (0.0f < diff && diff < 0.06f)
            {
                servoPosition = Mathf.RoundToInt(Mathf.Lerp(105f, 85f, diff / 0.06f)) + calibrate;
            }
            else
            {
                servoPosition = 85 + calibrate;
            }
        }
        byte[] servoPositionBytes = BitConverter.GetBytes(servoPosition);
        udpClient.Send(servoPositionBytes, servoPositionBytes.Length, remoteEndPoint);
    }
}
