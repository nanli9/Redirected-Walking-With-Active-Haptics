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
    /// <summary> User(OVRCameraRig.CenterEyeAnchor) </summary>
    public OVRCameraRig U;
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
    
    /// <summary> Straightness Quesionnaire Window </summary>
    public GameObject StraightnessQuestionnaireWindow;
    /// <summary> Presence Instruction Window </summary>
    public GameObject PresenceInstructionWindow;

    /// <summary> Straightness Radius </summary>
    private static readonly float STRAIGHT = 1000000.0f;
    /// <summary> Height Of All Walls </summary>
    private static readonly float h = 4.0f;
    /// <summary> Initial Distance </summary>
    public float d = 0.42f;
    /// <summary> Path User Required To Travel </summary>
    public float p = 5f;
    /// <summary> Allowed Radius </summary>
    // private static readonly float[] _r = {5.0f, 7.0f, 10.0f, 14.0f, 19.0f, 25.0f}; 
    private static readonly float[] _r = {7.0f,14.0f,21.0f}; 
    /// <summary> [Placeholder] Current Radius </summary>
    private float r;
    /// <summary> [Placeholder] Projected Vector From Center Of Actual Wall(AW) To User(U)</summary>
    private Vector3 V_vec;
    /// <summary> [Placeholder] Sum Of Travel Angle </summary>
    private float theta;
    /// <summary> [Placeholder] Relative Angle from Current Projected Vector </summary>
    private float ctheta;
    /// <summary> [Placeholder] Relative Angle from Previous Projected Vector</summary>
    private float ptheta;
    /// <summary> [Placeholder] Difference Between Current Relative Angle and Previous Relative Angle</summary>
    private float diff;
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
    
    private List<Tuple<float, bool>> Cases;
    private int n_radius;

    private String participant_id;
    private StreamWriter cases_writer;
    private StreamWriter straightness_response_writer;
    private StreamWriter position_writer;
    private StreamWriter engagement_response_writer;
    private StreamWriter presence_response_writer;
    private StreamWriter immersion_response_writer;
    private StreamWriter sickness_response_writer;

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

    private List<Vector3> position;
    private List<float> time;

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

    private void ScreenUpdate() {
        StraightnessQuestionnaireWindow.transform.position = U.centerEyeAnchor.transform.position + U.centerEyeAnchor.transform.forward * 0.4f;
        StraightnessQuestionnaireWindow.transform.rotation = U.centerEyeAnchor.transform.rotation;
        PresenceInstructionWindow.transform.position = U.centerEyeAnchor.transform.position + U.centerEyeAnchor.transform.forward * 0.4f;
        PresenceInstructionWindow.transform.rotation = U.centerEyeAnchor.transform.rotation;
    }


    /**
      *  Start is called before the first frame update
      */
    void Awake()
    {
        // Create Cases
        Cases = new List<Tuple<float, bool>>();
        var Temp = new List<Tuple<float, bool>>();
        n_radius = 0;
        foreach (var radius in _r)
        {
            // randomlly choose bool value
            bool FirstCondition = (UnityEngine.Random.value > 0.5f);
            Cases.Add(new Tuple<float, bool>(radius, FirstCondition));
            Temp.Add(new Tuple<float, bool>(radius, !FirstCondition));
            n_radius += 1;
        }
        // Shuffle Cases
        Cases.Shuffle();
        Temp.Shuffle();
        Cases.AddRange(Temp);

        // Init UDP
        udpClient = new UdpClient(localPort);
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);
        Debug.Log("UDP Client started");

        participant_id = DateTime.Now.ToString("yyyyMMddHHmmss");

        cases_writer = new StreamWriter(Application.persistentDataPath + "/CaseOrder.csv", true);
        if (new FileInfo(Application.persistentDataPath + "/CaseOrder.csv").Length == 0) {
            cases_writer.WriteLine("Participant ID, Radius, Condition");
        }
        foreach (var c in Cases) {
            cases_writer.WriteLine(participant_id + "," + c.Item1 + "," + c.Item2);
        }
        cases_writer.Flush();
        cases_writer.Close();

        straightness_response_writer = new StreamWriter(Application.persistentDataPath + "/StraightnessResponse.csv", true);
        if (new FileInfo(Application.persistentDataPath + "/StraightnessResponse.csv").Length == 0) {
            straightness_response_writer.WriteLine("Participant ID, Radius, Condition, Straightness");
        }
        position_writer = new StreamWriter(Application.persistentDataPath + "/Position.csv", true);
        if (new FileInfo(Application.persistentDataPath + "/Position.csv").Length == 0) {
            position_writer.WriteLine("Participant ID, Radius, Condition, Position, Time");
        }
        // engagement_response_writer = new StreamWriter(Application.persistentDataPath + "/EngagementResponse.csv", true);
        // if (new FileInfo(Application.persistentDataPath + "/EngagementResponse.csv").Length == 0) {
        //     engagement_response_writer.WriteLine("Participant ID, Radius, Condition, Engagement");
        // }
        // presence_response_writer = new StreamWriter(Application.persistentDataPath + "/PresenceResponse.csv", true);
        // if (new FileInfo(Application.persistentDataPath + "/PresenceResponse.csv").Length == 0) {
        //     presence_response_writer.WriteLine("Participant ID, Radius, Condition, Presence");
        // }
        // immersion_response_writer = new StreamWriter(Application.persistentDataPath + "/ImmersionResponse.csv", true);
        // if (new FileInfo(Application.persistentDataPath + "/ImmersionResponse.csv").Length == 0) {
        //     immersion_response_writer.WriteLine("Participant ID, Radius, Condition, Immersion");
        // }
        // sickness_response_writer = new StreamWriter(Application.persistentDataPath + "/SicknessResponse.csv", true);
        // if (new FileInfo(Application.persistentDataPath + "/SicknessResponse.csv").Length == 0) {
        //     sickness_response_writer.WriteLine("Participant ID, Radius, Condition, Sickness");
        // }
    }

    void Start() 
    {
        // Init Variables
        Initialization();
    }

    // Update is called once per frame
    void Update()
    {
        // Testing Variable Update
        VW.SetActive(ViewVisualWall);
        TestingObjectsViewUpdate();

        // Rendering Algorithm Update
        VisionRendering();  
        ScreenUpdate();
        if (td > p && td - p < 0.1f) {
            position_writer.WriteLine(participant_id+", "+r+", "+WithServo+", \"("+String.Join(", ",position)+")\", \"("+String.Join(", ",time)+")\"");
            StraightnessQuestionnaireWindow.SetActive(true);
            return;
        } else {
            position.Add(U.centerEyeAnchor.transform.position);
            time.Add(Time.time);
        }
        HapticRendering();  // Dependent on Vision Rendering Algorithm
    }

    void Initialization()
    {
        // Pop one at a time
        if (Cases.Count > 0)
        {
            var c = Cases[0];
            r = c.Item1;
            WithServo = c.Item2;
            Cases.RemoveAt(0);
            // Debug.Log($"Radius: {r}, On/Off: {WithServo}, Cases Left: {Cases.Count}");
            HW.transform.localScale = new Vector3(r*2, h, r*2); 
            HW.transform.position = new Vector3(U.centerEyeAnchor.transform.position.x, h, U.centerEyeAnchor.transform.position.z) - U.transform.right * (r + d);

            // Reset Sum of Travel Angle and Relative Angle from Previous Projected Vector
            theta = 0;  
            ptheta = Mathf.Atan2(U.centerEyeAnchor.transform.position.z - HW.transform.position.z, U.centerEyeAnchor.transform.position.z - HW.transform.position.z); 
        
            position = new List<Vector3>();
            time = new List<float>();
        } else {
            straightness_response_writer.Flush();
            straightness_response_writer.Close();
            position_writer.Flush();
            position_writer.Close();
            Debug.Log("No more case, terminating experiment");
            Application.Quit();
        }
 }

    void VisionRendering()
    {
        /// Visual Wall and Visual Floor Rendering
        // 1. Get Projected Vector from center of Haptic Wall to position of User
        V_vec = new Vector3(U.centerEyeAnchor.transform.position.x - HW.transform.position.x, 0, U.centerEyeAnchor.transform.position.z - HW.transform.position.z);

        // 2. Get Projected Point by projecting Projected Vector from center of Haptic Wall in Radius magnitude.
        P = new Vector3(HW.transform.position.x, 0, HW.transform.position.z) + V_vec.normalized * r;

        // 3. Get clockwize Tangent Unit Vector on the surface of Haptic Wall at Projected Point using absolute Up direction, and Projected Vector.
        T_hat = Vector3.Cross(Vector3.up, V_vec.normalized);

        // 4. Match Quaternion of Visual Wall and Quaternion of Visual Floor to Projected Unit Vector direction using Unity Quaternion.LookRotation method. Please Expand this to actual formula instead of Unity predefinded method
        VW.transform.rotation = Quaternion.LookRotation(V_vec.normalized, Vector3.up);
        VF.transform.rotation = VW.transform.rotation;

        // 5. Get User Relative Angle from z component and x component of Project Vector in -PI to PI scale.
        ctheta = Mathf.Atan2(V_vec.normalized.z, V_vec.normalized.x);
        diff = ctheta - ptheta;
        diff += (diff > Mathf.PI) ? -2 * Mathf.PI : (diff < -Mathf.PI) ?  2 * Mathf.PI : 0; // Convert to -PI to PI scale
        theta += diff;
        ptheta = ctheta;

        // 6. Get User Travel Distance from Total Traveled Angle multiplied by sum of Radius of Haptic Wall and Initial Distance.
        td = theta * (r + d);

        // 7. Get the Shifting Direction Vector, which is oppsite to expected walking direction of User, which is same as clockwize Tangent Unit Vector, in Travel Distance Magnitude.
        S_vec = td * T_hat;

        // 8. Set Virtual Wall and Visual Floor position to where Projected Point is shifted with Shifting Direction Vector. so the user is feeling as if they are walking on straight path, event though they were walking along surface of Haptic Wall.
        VW.transform.position = P + S_vec;
        VF.transform.position = VW.transform.position;
        VW.transform.position = new Vector3(VW.transform.position.x, h/2, VW.transform.position.z);

        // 9. Set Start Indicator position to Projected Unit Vector direction with initial distance magnitude from Virtual Wall. 
        SL.transform.position = P + V_vec.normalized * d + S_vec;

        // 10. Set End Indicator position to anti-clockwize Tangent Unit Vector direction with path magnitude from Start Indicator.
        EL.transform.position = SL.transform.position - p * T_hat;

        /// Visual Hand and Sphere Rendering
        // 1. if Visual Wall is in between Actual Left Hand and User position, then Visual Left Hand position is projected on closet point on surface of Visual Wall from Actual Left Hand position.
        // Else, Visual Left Hand stays at Actual Left Hand position.
        Vector3 AP_vec = new Vector3(AH.PointerPose.localPosition.x,0,AH.PointerPose.localPosition.z) - P;
        AP_vec += V_vec.normalized * 0.04f;
        AH.UseRenderPosition = UseRenderPosition;
        if (Vector3.Dot(AP_vec,V_vec.normalized) > 0) {
            // AH.RenderPosition is VH position
            AH.RenderPosition = AH.PointerPose.localPosition;
            AS.transform.position = AH.PointerPose.position;
            VS.transform.position = AH.PointerPose.position;
        }
        else {
            // AH.RenderPosition is VH position
            AH.RenderPosition = AH.PointerPose.localPosition - Vector3.Dot(AP_vec,V_vec.normalized)*V_vec.normalized;    
            AS.transform.position = AH.PointerPose.position;
            VS.transform.position = AH.PointerPose.position - Vector3.Dot(AP_vec,V_vec.normalized)*V_vec.normalized;
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
            else if (0.06f <= diff)
            {
                servoPosition = 85 + calibrate;
            }
        }
        byte[] servoPositionBytes = BitConverter.GetBytes(servoPosition);
        udpClient.Send(servoPositionBytes, servoPositionBytes.Length, remoteEndPoint);
    }

    public void StraightnessResponse(float response) {
        // if (STRAIGHT- 0.1 < response && response < STRAIGHT+0.1) {
        // straightness_response_writer.WriteLine(participant_id+", STRAIGHT, "+WithServo+", "+response);
        // } else {
        // straightness_response_writer.WriteLine(participant_id+", "+r+", "+WithServo+", "+response);
        // }
        StraightnessQuestionnaireWindow.SetActive(false);
        straightness_response_writer.WriteLine(participant_id+", "+r+", "+WithServo+", "+response);
        straightness_response_writer.Flush();
        if (Cases.Count % n_radius == 0) {
            // Pop Window For User to Take Off Headset and Take a Presence Survey
            PresenceInstructionWindow.SetActive(true);
        } else {
            Initialization();
        }
    }

    public void PresenceCompletion() {
        PresenceInstructionWindow.SetActive(false);
        Initialization();
    }
}
