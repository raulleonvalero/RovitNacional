using UnityEngine;

public class RecenterCameraRig : MonoBehaviour
{
    public Transform trackingSpace;   // Pon aquí el objeto "TrackingSpace"
    public Transform hmd;             // Pon aquí el objeto "OVRHmd"

    public void Recenter()
    {
        ResetYaw();
        ResetPosition();
    }

    void ResetYaw()
    {
        float yaw = hmd.eulerAngles.y;
        trackingSpace.Rotate(0, -yaw, 0);
        Debug.Log("[Recenter] Yaw reset.");
    }

    void ResetPosition()
    {
        Vector3 offset = hmd.position - trackingSpace.position;
        trackingSpace.position -= offset;
        Debug.Log("[Recenter] Position reset.");
    }
}
