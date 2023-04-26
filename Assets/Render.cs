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

    public GameObject CenterEyeAnchor;
    public OVRHand LeftHand;
    public OVRHand RightHand;
    public GameObject RealSphere;
    public GameObject VisualSphere;
    public GameObject RealWall;
    public GameObject VisualWall;
    public GameObject Floor;
    public GameObject EnjoymentSurveyScreen;
    public GameObject RealismSurveyScreen;
    public GameObject PresenceSurveyScreen;
    public GameObject StraightnessSurveyScreen;
    public GameObject EndingScreen;
    public TMP_Text ResultScreen;
    public float init_radius = 22f;
    public float init_distance = 0.42f;
    public float step_meter = 5f;
    public bool UseRenderPosition = true;
    public bool ViewTestingObjects = true;
    public bool ViewVisualWall = true;
    
    private bool WithServo;
    
    public string remoteIpAddress = "192.168.4.1";
    public int remotePort = 4210;
    private const int localPort = 4210;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;


    private float min_radius_threshold;
    private float max_radius_threshold;
    private float radius;

    private Vector3 step_start_position = new Vector3(0f,0f,0f);

    private List<float> radius_record;
    private List<float> radius_time_record;
    private List<Vector3> position_record;
    private List<float> position_time_record;

    private StreamWriter writer;
    private bool servo_switched = false;

    // Start is called before the first frame update
    void Start()
    {
        radius_record = new List<float>();
        radius_time_record = new List<float>();
        position_record = new List<Vector3>();
        position_time_record = new List<float>();

        WithServo = UnityEngine.Random.Range(0, 2) == 0;

        string file_path = Application.persistentDataPath + "/" + DateTime.Now + ".csv";
        writer = new StreamWriter(file_path, true);

        ResetExperiment();
        udpClient = new UdpClient(localPort);
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), remotePort);
        Debug.Log("UDP server started");
    }

    // Update is called once per frame
    void Update()
    {
        // if (RightHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) > 0.85f) {
        //     WithServo = !WithServo;
        // }
        position_record.Add(CenterEyeAnchor.transform.position);
        position_time_record.Add(Time.time);
        SurveyScreenUpdate();
        ApplyRadiusChange();
        TestingObjectsViewUpdate();
        VisualWall.SetActive(ViewVisualWall);
        SendServoPosition(handProjection());
    }

    private void SendServoPosition(float handProjectionResult) {
        int calibrate = 35;
        int servoPosition = 125 + calibrate;
        if (WithServo) {
            if (handProjectionResult < 0.06f && handProjectionResult > 0f)
            {
                servoPosition = Mathf.RoundToInt(Mathf.Lerp(105f, 85f, handProjectionResult / 0.06f)) + calibrate;
            }
            else if (handProjectionResult >= 0.04f)
            {
                servoPosition = 85 + calibrate;
            }
        }
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

        // if user walked for required distance for the step
        // turn on the survery screen
        if (Vector3.Distance(playerPosition, step_start_position) > step_meter) {
            StraightnessSurveyScreen.SetActive(true);
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
        EnjoymentSurveyScreen.transform.position = CenterEyeAnchor.transform.position + CenterEyeAnchor.transform.forward * init_distance;
        EnjoymentSurveyScreen.transform.rotation = CenterEyeAnchor.transform.rotation;
        RealismSurveyScreen.transform.position = CenterEyeAnchor.transform.position + CenterEyeAnchor.transform.forward * init_distance;
        RealismSurveyScreen.transform.rotation = CenterEyeAnchor.transform.rotation;
        PresenceSurveyScreen.transform.position = CenterEyeAnchor.transform.position + CenterEyeAnchor.transform.forward * init_distance;
        PresenceSurveyScreen.transform.rotation = CenterEyeAnchor.transform.rotation;
        StraightnessSurveyScreen.transform.position = CenterEyeAnchor.transform.position + CenterEyeAnchor.transform.forward * init_distance;
        StraightnessSurveyScreen.transform.rotation = CenterEyeAnchor.transform.rotation;
        EndingScreen.transform.position = CenterEyeAnchor.transform.position + CenterEyeAnchor.transform.forward * init_distance;
        EndingScreen.transform.rotation = CenterEyeAnchor.transform.rotation;
    }

    private void ResetExperiment() {
        radius = init_radius;
        min_radius_threshold = 0.0f;
        max_radius_threshold = init_radius * 2f;
        step_start_position = CenterEyeAnchor.transform.position;
        step_start_position.y = 0;
        radius_record.Add(radius);
        radius_time_record.Add(Time.time);
    }

    public void SurveyStraightness(int answer) {
        switch (answer) {
            case 1:
                min_radius_threshold = radius;
                radius += 0.30f * (max_radius_threshold - radius);
                break;
            case 2:
                min_radius_threshold = radius;
                radius += 0.15f * (max_radius_threshold - radius);
                break;
            case 3:
                min_radius_threshold = radius;
                radius += 0.07f * (max_radius_threshold - radius);
                break;
            case 5:
                max_radius_threshold = radius;
                radius -= 0.07f * (radius - min_radius_threshold);
                break;
            case 6:
                max_radius_threshold = radius;
                radius -= 0.15f * (radius - min_radius_threshold);
                break;
            case 7:
                max_radius_threshold = radius;
                radius -= 0.30f * (radius - min_radius_threshold);
                break;
            default: // case 4 end experiment
                //store data and clear the record
                writer.WriteLine(WithServo ? "servo_on" : "servo_off");
                ResultScreen.text += (WithServo ? "servo_on" : "servo_off");

                // store radius and radius time data to csv file
                writer.WriteLine("Radius,RadiusTime, Position, PositionTime");
                ResultScreen.text += "Radius,Time\n";
                
                for (int i = 0; i < Math.Max(radius_record.Count, position_record.Count); i++) {
                    if (i < radius_record.Count) {
                        writer.Write(radius_record[i] + "," + radius_time_record[i]);
                        ResultScreen.text += radius_record[i] + "," + radius_time_record[i];
                    } else {
                        writer.Write(",");
                        ResultScreen.text += ",";
                    }
                    if (i < position_record.Count) {
                        writer.WriteLine("," + position_record[i] + "," + position_time_record[i]);
                        ResultScreen.text += "," + position_record[i] + "," + position_time_record[i] + "\n";
                    } else {
                        writer.WriteLine(",");
                        ResultScreen.text += ",\n";
                    }
                }

                // clear the record
                radius_record.Clear();
                radius_time_record.Clear();
                position_record.Clear();
                position_time_record.Clear();

                EnjoymentSurveyScreen.SetActive(true);
                return;
        }
        step_start_position = CenterEyeAnchor.transform.position;
        step_start_position.y = 0;
        radius_record.Add(radius);
        radius_time_record.Add(Time.time);
    }

    public void SurveryEnjoyment(int value) {
        writer.WriteLine("Enjoyment," + value);        
        ResultScreen.text += "Enjoyment," + value + "\n";
    }

    public void SurveyRealism(int value) {
        writer.WriteLine("Realism," + value);
        ResultScreen.text += "Realism," + value + "\n";
    }

    public void SurveyPreference(int value) {
        writer.WriteLine("Preference," + value);
        ResultScreen.text += "Preference," + value + "\n";
        if (servo_switched) {
            writer.Close();
            EndingScreen.SetActive(true);
            return;
        }
        WithServo = !WithServo;
        servo_switched = true;

        ResetExperiment();
    }
}
