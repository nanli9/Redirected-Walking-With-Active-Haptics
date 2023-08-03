using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    /// <summary> Straightness Radius </summary>
    private static readonly float STRAIGHT = 1000000.0f;
    /// <summary> Initial Distance </summary>
    public float d = 0.42f;
    /// <summary> Path User Required To Travel </summary>
    public float p = 5f;
    /// <summary> Default Radius Index </summary>
    private static readonly int DEFAULT_ri = 0;
    /// <summary> Allowed Radius </summary>
    private static readonly float[] _r = {5.0f, 7.0f, 10.0f, 14.0f, 19.0f, 25.0f, STRAIGHT}; 
    /// <summary> [Placeholder] Current Radius </summary>
    private float r = _r[DEFAULT_ri];
    /// <summary> [Placeholder] Normalized Vector Towards Left Of The User(U) </summary>
    private Vector3 L;
    /// <summary> [Placeholder] Projected Vector From Center Of Actual Wall(AW) To User(U)</summary>
    private Vector3 V;
    /// <summary> [Placeholder] Projected Point On The Surface Of Haptic Wall(HW) Towards User(U)</summary>
    private Vector3 P;
    /// <summary> Remote Servo Condition Switch </summary>
    private bool WithServo;
    
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

    // Start is called before the first frame update
    void Start()
    {
        // WithServo = UnityEngine.Random.Range(0, 2) == 0;
        WithServo = true;
        
        InitEnvironment();

        // Init UDP
        udpClient = new UdpClient(localPort);
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);
        Debug.Log("UDP server started");
    }

    void InitEnvironment()
    {
        r = 
        L = Vector3.Cross(U.transform.forward,Vector3.up);
        HW.transform.position = U.transform.position + L * d;
    }

    // Update is called once per frame
    void Update()
    {
        TestingObjectsViewUpdate();
        VW.SetActive(ViewVisualWall);


        // SendServoPosition(handProjection());
    }


    private void TestingObjectsViewUpdate() {
        if (ViewTestingObjects) {
            HW.layer = LayerMask.NameToLayer("Default");
            AS.layer = LayerMask.NameToLayer("Default");
            VS.layer = LayerMask.NameToLayer("Default");
        } else {
            HW.layer = LayerMask.NameToLayer("TestingObject");
            AS.layer = LayerMask.NameToLayer("TestingObject");
            VS.layer = LayerMask.NameToLayer("TestingObject");
        }
    }

    // private void SendServoPosition(float handProjectionResult) {
    //     int calibrate = 50;
    //     int servoPosition = 125 + calibrate;
    //     if (WithServo) {
    //         if (handProjectionResult < 0.06f && handProjectionResult > 0f)
    //         {
    //             servoPosition = Mathf.RoundToInt(Mathf.Lerp(105f, 85f, handProjectionResult / 0.06f)) + calibrate;
    //         }
    //         else if (handProjectionResult >= 0.04f)
    //         {
    //             servoPosition = 85 + calibrate;
    //         }
    //     }
    //     byte[] servoPositionBytes = BitConverter.GetBytes(servoPosition);
    //     udpClient.Send(servoPositionBytes, servoPositionBytes.Length, remoteEndPoint);
    // }

    // private void ApplyRadiusChange() {
    //     HW.transform.localScale = new Vector3(radius * 2, HW.transform.localScale.y, radius * 2);
    //     HW.transform.position = step_start_position - step_start_right * (radius + d) + new Vector3(0,2,0);
    //     Vector3 playerPosition = U.transform.position;
    //     playerPosition.y = 0;
    //     Vector3 wallCenter = HW.transform.position;
    //     wallCenter.y = 0;
    //     Vector3 playerToWall = playerPosition - wallCenter;
    //     Vector3 wallNormal = HW.transform.up;
    //     VW.transform.position = HW.transform.position + playerToWall.normalized * radius;
    //     VW.transform.rotation = Quaternion.LookRotation(-wallNormal, playerToWall.normalized);

    //     // Calculate wall shift
    //     float wallAngle = Mathf.Atan2(playerToWall.z, playerToWall.x);  // calculate angle of player from real wall
    //     if (playerToWall.x < 0f) {                                      // cast it to (0, 2PI) System
    //         wallAngle += Mathf.PI * 2f;
    //     }

    //     // if user walked for required distance for the step
    //     // and all screnes are off
    //     // turn on the survery screen
    //     if (Vector3.Distance(playerPosition, step_start_position) > step_meter && !EnjoymentSurveyScreen.activeInHierarchy && !RealismSurveyScreen.activeInHierarchy && !PresenceSurveyScreen.activeInHierarchy && !StraightnessSurveyScreen.activeInHierarchy && !EndingScreen.activeInHierarchy) {
    //         StraightnessSurveyScreen.SetActive(true);
    //     }

    //     // Apply wall shift
    //     Vector3 visualWallRight = Vector3.Cross(playerToWall.normalized, wallNormal);                   // get the shifting direction
    //     VW.transform.position += visualWallRight * (1 - wallAngle) * (radius + d);  // Apply Shift to Opposite Direction of Walking in circunstance scale
    // }

    // private float handProjection() {
    //     Vector3 realHandPosition = AH.PointerPose.position;
    //     Vector3 HWCenter = HW.transform.position; 
    //     HWCenter.y = 0;
    //     AS.transform.position = realHandPosition;
    //     Vector3 realHand2DPosition = new Vector3(realHandPosition.x,0,realHandPosition.z);
    //     Vector3 handToWallCenter = realHand2DPosition - HWCenter;

    //     if (Mathf.Abs(handToWallCenter.magnitude) <= radius)
    //     {
    //         Vector3 handProjectedOnWall = HWCenter + (handToWallCenter.normalized * radius);
    //         handProjectedOnWall.y = realHandPosition.y;
    //         VS.transform.position = handProjectedOnWall;
    //     } else {
    //         VS.transform.position = realHandPosition;
    //     }
    //     if (Mathf.Abs(handToWallCenter.magnitude) <= radius - 0.03f)
    //     {
    //         Vector3 handProjectedOnWall = HWCenter + (handToWallCenter.normalized * (radius - 0.03f));
    //         handProjectedOnWall.y = realHandPosition.y;
    //         AH.UseRenderPosition = UseRenderPosition;
    //         AH.RenderPosition = handProjectedOnWall;
    //     } else {
    //         AH.UseRenderPosition = false;
    //     }
    //     float magnitude = (AS.transform.position - VS.transform.position).magnitude; 
        
    //     if (Mathf.Approximately(magnitude, 0f)) {
    //         return 0f;
    //     } else {
    //         return magnitude;
    //     }

    // }

}
