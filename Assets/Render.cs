using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Render : MonoBehaviour
{

    public GameObject CenterEyeAnchor;
    public OVRHand LeftHand;
    public GameObject RealSphere;
    public GameObject VisualSphere;
    public GameObject RealWall;
    public GameObject VisualWall;
    public GameObject Floor;
    public GameObject SurveyScreen;
    public float radius = 20f;
    public float init_distance = 0.42f;
    public bool UseRenderPosition = true;
    public bool ViewTestingObjects = true;
    public bool ViewVisualWall = true;
    
    public bool WithServo = true;
    
    public string remoteIpAddress = "192.168.4.1";
    public int remotePort = 4210;
    private const int localPort = 4210;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    // Start is called before the first frame update
    void Start()
    {
        udpClient = new UdpClient(localPort);
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);
        Debug.Log("UDP server started");
    }

    // Update is called once per frame
    void Update()
    {
        SurveyScreenUpdate();
        ApplyRadiusChange();
        TestingObjectsViewUpdate();
        VisualWall.SetActive(ViewVisualWall);
        float handProjectionResult = handProjection();
        int servoPosition = 105;
        if (WithServo) {
            if (handProjectionResult < 0.06f && handProjectionResult > 0f)
            {
                servoPosition = Mathf.RoundToInt(Mathf.Lerp(95f, 75f, handProjectionResult / 0.06f));
            }
            else if (handProjectionResult >= 0.04f)
            {
                servoPosition = 75;
            }
        }
        // print(servoPosition);
        byte[] servoPositionBytes = BitConverter.GetBytes(servoPosition);
        udpClient.Send(servoPositionBytes, servoPositionBytes.Length, remoteEndPoint);
    }

    private void ApplyRadiusChange() {
        RealWall.transform.localScale = new Vector3(radius * 2, RealWall.transform.localScale.y, radius * 2);
        RealWall.transform.position = new Vector3(-radius-init_distance, 2, 0);
        Vector3 playerPosition = CenterEyeAnchor.transform.position;
        playerPosition.y = 0;
        Vector3 wallCenter = RealWall.transform.position;
        wallCenter.y = 0;
        Vector3 playerToWall = playerPosition - wallCenter;
        Vector3 wallNormal = RealWall.transform.up;
        VisualWall.transform.position = RealWall.transform.position + playerToWall.normalized * radius;
        VisualWall.transform.rotation = Quaternion.LookRotation(-wallNormal, playerToWall.normalized);

        // Calculate wall shift
        float wallAngle = Mathf.Atan2(playerToWall.z, playerToWall.x);  // calculate angle of player from real wall
        if (playerToWall.x < 0f) {                                      // cast it to (0, 2PI) System
            wallAngle += Mathf.PI * 2f;
        }
        // Apply wall shift
        Vector3 visualWallRight = Vector3.Cross(playerToWall.normalized, wallNormal);                   // get the shifting direction
        VisualWall.transform.position += visualWallRight * (1 - wallAngle) * (radius + init_distance);  // Apply Shift to Opposite Direction of Walking in circunstance scale
    }

    private void TestingObjectsViewUpdate() {
        if (ViewTestingObjects) {
            RealWall.layer = LayerMask.NameToLayer("Default");
            RealSphere.layer = LayerMask.NameToLayer("Default");
            VisualSphere.layer = LayerMask.NameToLayer("Default");
        } else {
            RealWall.layer = LayerMask.NameToLayer("TestingObject");
            RealSphere.layer = LayerMask.NameToLayer("TestingObject");
            VisualSphere.layer = LayerMask.NameToLayer("TestingObject");
        }
    }

    private float handProjection() {
        Vector3 realHandPosition = LeftHand.PointerPose.position;
        Vector3 realWallCenter = RealWall.transform.position; 
        realWallCenter.y = 0;
        RealSphere.transform.position = realHandPosition;
        Vector3 realHand2DPosition = new Vector3(realHandPosition.x,0,realHandPosition.z);
        Vector3 handToWallCenter = realHand2DPosition - realWallCenter;

        if (Mathf.Abs(handToWallCenter.magnitude) <= radius)
        {
            Vector3 handProjectedOnWall = realWallCenter + (handToWallCenter.normalized * radius);
            handProjectedOnWall.y = realHandPosition.y;
            VisualSphere.transform.position = handProjectedOnWall;
        } else {
            VisualSphere.transform.position = realHandPosition;
        }
        if (Mathf.Abs(handToWallCenter.magnitude) <= radius - 0.03f)
        {
            Vector3 handProjectedOnWall = realWallCenter + (handToWallCenter.normalized * (radius - 0.03f));
            handProjectedOnWall.y = realHandPosition.y;
            LeftHand.UseRenderPosition = UseRenderPosition;
            LeftHand.RenderPosition = handProjectedOnWall;
        } else {
            LeftHand.UseRenderPosition = false;
        }
        float magnitude = (RealSphere.transform.position - VisualSphere.transform.position).magnitude; 
        
        if (Mathf.Approximately(magnitude, 0f)) {
            return 0f;
        } else {
            return magnitude;
        }

    }

    private void SurveyScreenUpdate() {
        SurveyScreen.transform.position = CenterEyeAnchor.transform.position + CenterEyeAnchor.transform.forward * init_distance;
        SurveyScreen.transform.rotation = CenterEyeAnchor.transform.rotation;
        // SurveyScreen.SetActive(true);
    }

    public void SurveyStraightness(bool answer) {

    }
}
