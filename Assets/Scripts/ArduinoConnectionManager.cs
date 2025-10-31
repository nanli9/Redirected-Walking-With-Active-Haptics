using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ArduinoConnectionManager : MonoBehaviour
{
    private TcpClient leftHandClient;
    private TcpClient rightHandClient;
    private NetworkStream leftStream;
    private NetworkStream rightStream;

    private UdpClient udpClient;
    private static ArduinoConnectionManager _instance;
    private bool isLeftConnected = false;
    private bool isRightConnected = false;


    public bool isConnected()
    {
        return isLeftConnected || isRightConnected;
    }

    public static ArduinoConnectionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ArduinoConnectionManager>();
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        StartDiscoveryProcess();
    }

    private void StartDiscoveryProcess()
    {
        if (!isLeftConnected || !isRightConnected)
        {
            //DiscoverArduinoIP();
            Invoke("DiscoverArduinoIP", 1.0f);
        }
    }

    private void DiscoverArduinoIP()
    {
        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        byte[] message = Encoding.ASCII.GetBytes("DISCOVER_ARDUINO");
        IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, 8889);

        udpClient.Send(message, message.Length, broadcastEndPoint);
        udpClient.BeginReceive(ReceiveCallback, null);
        Debug.Log("Sent discovery broadcast.");
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 8889);
        byte[] data = udpClient.EndReceive(ar, ref remoteEndPoint);
        string response = Encoding.ASCII.GetString(data);
        Debug.Log("Received UDP response: " + response);

        if (response.StartsWith("LEFT_HAND") && !isLeftConnected)
        {
            string ipString = response.Split(' ')[1];
            Debug.Log("Discovered Left Hand Arduino IP: " + ipString);
            if (IPAddress.TryParse(ipString, out var arduinoIP))
            {
                InitializeLeftHandConnection(new IPEndPoint(arduinoIP, 8888));
            }
        }
        else if (response.StartsWith("RIGHT_HAND") && !isRightConnected)
        {
            string ipString = response.Split(' ')[1];
            Debug.Log("Discovered Right Hand Arduino IP: " + ipString);
            if (IPAddress.TryParse(ipString, out var arduinoIP))
            {
                InitializeRightHandConnection(new IPEndPoint(arduinoIP, 8888));
            }
        }

        // Continue to discover if not all connections are established
        if (!isLeftConnected || !isRightConnected)
        {
            DiscoverArduinoIP();
        }
    }

    private void InitializeLeftHandConnection(IPEndPoint leftHandEndPoint)
    {
        try
        {
            leftHandClient = new TcpClient();
            leftHandClient.Connect(leftHandEndPoint);
            leftStream = leftHandClient.GetStream();
            isLeftConnected = true;
            Debug.Log("Connected to Left Hand Arduino at " + leftHandEndPoint.Address.ToString());
        }
        catch (Exception e)
        {
            Debug.LogWarning("Connection to Left Hand Arduino failed: " + e.Message);
            isLeftConnected = false;
            Invoke("DiscoverArduinoIP", 2.0f);
        }
    }

    private void InitializeRightHandConnection(IPEndPoint rightHandEndPoint)
    {
        try
        {
            rightHandClient = new TcpClient();
            rightHandClient.Connect(rightHandEndPoint);
            rightStream = rightHandClient.GetStream();
            isRightConnected = true;
            Debug.Log("Connected to Right Hand Arduino at " + rightHandEndPoint.Address.ToString());
        }
        catch (Exception e)
        {
            Debug.LogWarning("Connection to Right Hand Arduino failed: " + e.Message);
            isRightConnected = false;
            Invoke("DiscoverArduinoIP", 2.0f);
        }
    }

    public void SendMessageToLeftHandArduino(int idJoint, float frequency, float amplitude)
    {
        if (leftStream != null && leftHandClient != null && leftHandClient.Connected)
        {
            string message = $"{idJoint} {frequency:F2} {amplitude:F2}\n";
            byte[] data = Encoding.ASCII.GetBytes(message);
            leftStream.Write(data, 0, data.Length);
            Debug.Log("Sent to Left Hand Arduino: " + message);
        }
        else
        {
            Debug.LogWarning("Left Hand Arduino not connected. Cannot send message. Retrying discovery...");
            DiscoverArduinoIP();
        }
    }

    public void SendMessageToRightHandArduino(int idJoint, float frequency, float amplitude)
    {
        if (rightStream != null && rightHandClient != null && rightHandClient.Connected)
        {
            string message = $"{idJoint} {frequency:F2} {amplitude:F2}\n";
            byte[] data = Encoding.ASCII.GetBytes(message);
            rightStream.Write(data, 0, data.Length);
            Debug.Log("Sent to Right Hand Arduino: " + message);
        }
        else
        {
            Debug.LogWarning("Right Hand Arduino not connected. Cannot send message. Retrying discovery...");
            DiscoverArduinoIP();
        }
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();
        leftStream?.Close();
        leftHandClient?.Close();
        rightStream?.Close();
        rightHandClient?.Close();
    }
}