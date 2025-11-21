using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;


public static class IListExtensions
{
    /// <summary>
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}


public class Render : MonoBehaviour
{
    //user study
    public ArduinoControlManager acm;
    public enum ConditionType
    {
        LC, LV, RC, RV,
        BLC, BRC, BLV, BRV,
        NR, NL
    }
    [Header("Condition Cases")]
    public ConditionType selectedCondition;   // <-- dropdown in Inspector
    public float staircaseStep;
    private float curvatureGain;
    public int UserNumber;
    private int trialNumber;
    private bool lastIncrease;
    private float minSpeed = 0.05f;   // below this = “not really moving” / noise
    private float maxSpeed = 1.5f;   // normal brisk walk, you can tweak to 1.8f if needed
    private float minVibrationAmplitude = 0f;
    private float maxVibrationAmplitude = 180.0f;
    private bool variationBySpeed;
    private Vector3 lastPos;
    private Vector3 cameraOffset = new Vector3(0f, 0f, 0f);
    private float averageSpeed;
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
    public GameObject VLAnchor;
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
    // private static readonly float[] _r = {5.0f, 7.0f, 10.0f, 14.0f, 19.0f, 25.0f}; 
    private static readonly float[] _r = { 7.0f, 14.0f, 21.0f };
    // private static readonly float[] _r = {7f,-7f};
    /// <summary> [Placeholder] Current Radius </summary>
    public float r;
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

    
    private float sign;


    [SerializeField] public Transform sceneRoot;

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

    private StreamWriter curvatureGain_writer;

    // This stores how much we "twist" the virtual world so current view becomes new forward
    private Quaternion recenterOffset = Quaternion.identity;

    public bool UseRenderPosition = true;
    public bool ViewTestingObjects = true;
    public bool ViewVisualWall = true;

    /**
      *  Remote Servo Connection Variables
      */
    private const string remoteIpAddress = "192.168.4.1";
    // private const string remoteIpAddress = "192.168.88.251";
    private const int remotePort = 4210;
    private const int localPort = 4210;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    private List<Vector3> position;
    private List<float> time;

        // --- Head velocity tracking ---
    public Vector3 filteredVelocity;

    [Range(0f, 1f)]
    public float velocityAlpha = 0.5f; // 'a' in v_new = a*v_now + (1-a)*v_old

    private void IncreaseCurvatureGain()
    {
        if(!lastIncrease)
        {
            staircaseStep = Mathf.Max(staircaseStep/2,0.00625f);
            lastIncrease = !lastIncrease;
        }
        curvatureGain += staircaseStep;
        changeRadius();
    }
    private void DecreaseCurvatureGain()
    {
        if(lastIncrease)
        {
            staircaseStep = Mathf.Max(staircaseStep/2,0.00625f);
            lastIncrease = !lastIncrease;
        }
        curvatureGain -= staircaseStep;
        curvatureGain = Mathf.Max(0.0f, curvatureGain);
        changeRadius();
    }
    private void changeRadius()
    {
        trialNumber++;
        if (Mathf.Approximately(curvatureGain, 0f))
        {
            r = sign * STRAIGHT;                  // your "infinite" straight radius
        }
        else
        {
            r = sign * 1f / curvatureGain; // right = +, left = -
        }

        // 2) Resize haptic wall
        HW.transform.localScale = new Vector3(r * 2f, h, r * 2f);

        // 3) Reposition haptic wall so user is always (r + d) from its centre,
        //    along their current right vector (wall on left/right of user).
        Transform cam = U.centerEyeAnchor.transform;
        Vector3 camPos   = cam.position;
        Vector3 camRight = cam.right;

        HW.transform.position = new Vector3(camPos.x, h, camPos.z) - camRight * (r + d);

        // 4) Reset RDW integrators for the new radius
        V_vec = new Vector3(
            camPos.x - HW.transform.position.x,
            0f,
            camPos.z - HW.transform.position.z
        );
        theta = 0f;
        td    = 0f;
        ptheta = Mathf.Atan2(V_vec.normalized.z, V_vec.normalized.x);
        S_vec  = Vector3.zero;

        // 5) Update virtual path with new geometry, then align it to the real view
        VisionRendering();
        CalibrateVirtualPathToReal();

        curvatureGain_writer.WriteLine(UserNumber + ", "+ trialNumber + "," + selectedCondition.ToString() + ", " + sign * curvatureGain + ", " + averageSpeed);
        curvatureGain_writer.Flush();

        averageSpeed = 0;
    }
    /**
      *  Testing Objects Display Status Update
      */
    private void TestingObjectsViewUpdate()
    {
        if (ViewTestingObjects)
        {
            HW.layer = LayerMask.NameToLayer("Default");
            AS.layer = LayerMask.NameToLayer("Default");
            VS.layer = LayerMask.NameToLayer("Default");
            HS.layer = LayerMask.NameToLayer("Default");
        }
        else
        {
            HW.layer = LayerMask.NameToLayer("TestingObject");
            AS.layer = LayerMask.NameToLayer("TestingObject");
            VS.layer = LayerMask.NameToLayer("TestingObject");
            HS.layer = LayerMask.NameToLayer("TestingObject");
        }
    }

    private void CalibrateVirtualPathToReal()
    {
        if (sceneRoot == null)
        {
            Debug.LogWarning("[Calibrate] sceneRoot is not assigned.");
            return;
        }

        Transform cam = U.centerEyeAnchor.transform;

        // 1. Real-world direction: where the user is currently looking (yaw only)
        Vector3 realDir = cam.forward;
        realDir.y = 0f;
        if (realDir.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[Calibrate] Camera forward too small.");
            return;
        }
        realDir.Normalize();

        // 2. Virtual path direction: how your current virtual path is oriented.
        // If your line/path is drawn along another axis, adjust this accordingly.
        Vector3 virtualDir = VLAnchor.transform.forward;
        virtualDir.y = 0f;
        if (virtualDir.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[Calibrate] VLAnchor forward too small.");
            return;
        }
        virtualDir.Normalize();

        // 3. Compute how much to rotate the VIRTUAL WORLD so that:
        // virtualDir --> realDir
        float angle = Vector3.SignedAngle(virtualDir, realDir, Vector3.up);

        // 4. Rotate entire scene around the user's position
        sceneRoot.RotateAround(cam.position, Vector3.up, angle);

        
        // Slide world so SL is under camera (XZ only)
        Vector3 camPos = U.centerEyeAnchor.transform.position;
        Vector3 slPos  = SL.transform.position;
        Vector3 deltaXZ = new Vector3(slPos.x - camPos.x, 0f, slPos.z - camPos.z);
        sceneRoot.position -= deltaXZ;


        // Recompute current projected vector from HW -> user (XZ)
        V_vec = new Vector3(cam.position.x - HW.transform.position.x, 0f,
                            cam.position.z - HW.transform.position.z);

        // Reset integrators so S_vec starts at 0 this frame
        ptheta = Mathf.Atan2(V_vec.normalized.z, V_vec.normalized.x);
        theta  = 0f;
        td     = 0f;
        S_vec  = Vector3.zero;

        Debug.Log($"[Calibrate] Rotated scene by {angle:F2} degrees to align virtual path with real path.");
    }

    void Start()
    {
        // Init Variables
        //Debug.Log("Persistent Data Path: " + Application.persistentDataPath);
        Debug.Log("U Pos" + U.centerEyeAnchor.transform.position);
        sign = selectedCondition.ToString().Contains("L") ? 1 : -1;
        variationBySpeed = selectedCondition.ToString().Contains("V") ? true : false;

        curvatureGain = 0.0f;
        //right is positive after press the space key
        r = sign * STRAIGHT;
        //DirectionInstructionWindow.SetActive(true);
        trialNumber = 0;
        lastIncrease = true;

        // Debug.Log($"Radius: {r}, On/Off: {WithServo}, Cases Left: {Cases.Count}");
        HW.transform.localScale = new Vector3(r * 2, h, r * 2);
        HW.transform.position = new Vector3(U.centerEyeAnchor.transform.position.x, h, U.centerEyeAnchor.transform.position.z) - U.centerEyeAnchor.transform.right * (r + d);
        // Reset Sum of Travel Angle and Relative Angle from Previous Projected Vector
        theta = 0;
        V_vec = new Vector3(U.centerEyeAnchor.transform.position.x - HW.transform.position.x, 0, U.centerEyeAnchor.transform.position.z - HW.transform.position.z);
        ptheta = Mathf.Atan2(V_vec.normalized.z, V_vec.normalized.x);
        position = new List<Vector3>();
        time = new List<float>();

        //create the file and write title to the file
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd");
        string filename = $"{UserNumber}_{selectedCondition}_{timestamp}.csv";
        string fullPath = Path.Combine(Application.persistentDataPath, filename);
        curvatureGain_writer = new StreamWriter(fullPath, true, new UTF8Encoding());
        if (new FileInfo(fullPath).Length == 0)
            curvatureGain_writer.WriteLine("UserNo, TrialNo, Condition, curvatureGain, average velocity");

        VisionRendering();
        CalibrateVirtualPathToReal();

        filteredVelocity = Vector3.zero;
        lastPos = Vector3.zero;

        averageSpeed = 0;
    }

    // Update is called once per frame
    void Update()
    {
       
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //CalibrateVirtualPathToReal();
            VisionRendering();
            CalibrateVirtualPathToReal();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            IncreaseCurvatureGain();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            DecreaseCurvatureGain();
        }
        // Testing Variable Update
        VLAnchor.SetActive(ViewVisualWall);
        VF.SetActive(true);
        SL.SetActive(true);
        EL.SetActive(true);
        TestingObjectsViewUpdate();
        
        UpdateHeadVelocity();

        // Rendering Algorithm Update
        VisionRendering();

        /*
        if (td > p && td - p < 0.1f)
        {
            position_writer.WriteLine(participant_id + ", " + r + ", " + WithServo + ", \"(" + String.Join(", ", position) + ")\", \"(" + String.Join(", ", time) + ")\"");
            position_writer.Flush();
            return;
        }
        else
        {
            position.Add(U.centerEyeAnchor.transform.position);
            time.Add(Time.time);
        }
        */

    }

    private void UpdateHeadVelocity()
    {
        Vector3 currentPos = U.centerEyeAnchor.transform.position;

        Vector3 rawVelocity = (currentPos - lastPos) / Time.deltaTime;
        
        // Exponential smoothing:
        // v_new = a * v_now + (1 - a) * v_old
        filteredVelocity = velocityAlpha * rawVelocity + (1f - velocityAlpha) * filteredVelocity;
        averageSpeed = Mathf.Max(averageSpeed,filteredVelocity.magnitude);
        if(variationBySpeed)
        {
            float t = Mathf.InverseLerp(minSpeed, maxSpeed, filteredVelocity.magnitude);  // clamps automatically

            t = Mathf.Min(1.0f,t);

            // 4) Map to vibration intensity [0, 120]
            float vibrationIntensity = Mathf.Lerp(minVibrationAmplitude, maxVibrationAmplitude, t);

            // If your Arduino expects an int:
            int vibrationIntensityInt = Mathf.RoundToInt(vibrationIntensity);

            // Example: send to ArduinoControlManager
            acm.amplitude = vibrationIntensityInt; 
        }

        lastPos = currentPos;
    }

    void VisionRendering()
    {
        float r_d = r / Mathf.Abs(r);
        /// Visual Wall and Visual Floor Rendering
        // 1. Get Projected Vector from center of Haptic Wall to position of User
        V_vec = new Vector3(U.centerEyeAnchor.transform.position.x - HW.transform.position.x, 0, U.centerEyeAnchor.transform.position.z - HW.transform.position.z);

        // 2. Get Projected Point by projecting Projected Vector from center of Haptic Wall in Radius magnitude.
        P = new Vector3(HW.transform.position.x, 0, HW.transform.position.z) + V_vec.normalized * r * r_d;

        // 3. Get clockwize Tangent Unit Vector on the surface of Haptic Wall at Projected Point using absolute Up direction, and Projected Vector.
        T_hat = Vector3.Cross(r_d * Vector3.up, V_vec.normalized);

        // 4. Match Quaternion of Visual Wall and Quaternion of Visual Floor to Projected Unit Vector direction using Unity Quaternion.LookRotation method. Please Expand this to actual formula instead of Unity predefinded method
        VLAnchor.transform.rotation = Quaternion.LookRotation(T_hat.normalized, Vector3.up);
        VF.transform.rotation = VLAnchor.transform.rotation;

        // 5. Get User Relative Angle from z component and x component of Project Vector in -PI to PI scale.
        ctheta = Mathf.Atan2(V_vec.normalized.z, V_vec.normalized.x);
        diff = ctheta - ptheta;
        diff += (diff > Mathf.PI) ? -2 * Mathf.PI : (diff < -Mathf.PI) ? 2 * Mathf.PI : 0; // Convert to -PI to PI scale
        theta += diff;
        ptheta = ctheta;

        // 6. Get User Travel Distance from Total Traveled Angle multiplied by sum of Radius of Haptic Wall and Initial Distance.
        td = theta * (r + d);

        // 7. Get the Shifting Direction Vector, which is oppsite to expected walking direction of User, which is same as clockwize Tangent Unit Vector, in Travel Distance Magnitude.
        S_vec = td * T_hat;

        // 8. Set Virtual Wall and Visual Floor position to where Projected Point is shifted with Shifting Direction Vector. so the user is feeling as if they are walking on straight path, event though they were walking along surface of Haptic Wall.
        VLAnchor.transform.position = P + S_vec;
        VF.transform.position = VLAnchor.transform.position;
        VLAnchor.transform.position = new Vector3(VLAnchor.transform.position.x, 0.0f, VLAnchor.transform.position.z);


        //VLAnchor.GetComponent<VirtualLineAnchor>().DrawLines(-T_hat, 100.0f, 2.0f);

        Vector3 anchorPos = P + S_vec;  // anchored at wall surface tangent
        Quaternion anchorRot = Quaternion.LookRotation(T_hat, Vector3.up);

        // Apply to VLAnchor
        VLAnchor.transform.SetPositionAndRotation(anchorPos, anchorRot);
        VF.transform.SetPositionAndRotation(anchorPos, anchorRot);

        VLAnchor.transform.position +=  cameraOffset;

        // 9. Set Start Indicator position to Projected Unit Vector direction with initial distance magnitude from Virtual Wall. 
        //SL.transform.position = P + r_d * V_vec.normalized * d + S_vec;

        // 10. Set End Indicator position to anti-clockwize Tangent Unit Vector direction with path magnitude from Start Indicator.
        //EL.transform.position = SL.transform.position - p * T_hat;

        // Start = just in front of anchor along tangent
        //SL.transform.position = VLAnchor.transform.position + T_hat.normalized * d;

        // End = path length forward along tangent
        //EL.transform.position = SL.transform.position - T_hat.normalized * 30.0f;

        //SL.transform.position += new Vector3(0.0f, 0.5f, 0f);
        //EL.transform.position += new Vector3(0.0f, 0.5f, 0f);


        // 3) Place SL/EL in WORLD space using **consistent forward sign**
        Vector3 SL_world = P + r_d * V_vec.normalized * d + S_vec;
        Vector3 EL_world = SL_world + (/* choose sign */ +1f) * T_hat * p * 1.0f; // use +T_hat for “ahead”

        // Optional lift
        SL_world += Vector3.up * 0.5f;
        EL_world += Vector3.up * 0.5f;

        SL.transform.position = SL_world;
        SL.transform.rotation = VLAnchor.transform.rotation;
        EL.transform.position = EL_world;
        EL.transform.rotation = VLAnchor.transform.rotation;

        /// Visual Hand and Sphere Rendering
        // 1. if Visual Wall is in between Actual Left Hand and User position, then Visual Left Hand position is projected on closet point on surface of Visual Wall from Actual Left Hand position.
        // Else, Visual Left Hand stays at Actual Left Hand position.
        Vector3 AP_vec = new Vector3(AH.PointerPose.localPosition.x, 0, AH.PointerPose.localPosition.z) - P;
        // AP_vec += V_vec.normalized * 0.04f;
        AH.UseRenderPosition = UseRenderPosition;
        if (Vector3.Dot(AP_vec, V_vec.normalized) * r_d > 0)
        {
            // AH.RenderPosition is VH position
            AH.RenderPosition = AH.PointerPose.localPosition;
            AS.transform.position = AH.PointerPose.position;
            VS.transform.position = AH.PointerPose.position;
        }
        else
        {
            // AH.RenderPosition is VH position
            AH.RenderPosition = AH.PointerPose.localPosition - Vector3.Dot(AP_vec, V_vec.normalized) * V_vec.normalized;
            AS.transform.position = AH.PointerPose.position;
            VS.transform.position = AH.PointerPose.position - Vector3.Dot(AP_vec, V_vec.normalized) * V_vec.normalized;
        }

    }
    void OnApplicationQuit()
    {

    }
}