using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class RecenterFromScript : MonoBehaviour
{
    private XRInputSubsystem xrInput;

    void Awake()
    {
        // Get the OpenXR Input Subsystem used by Meta Quest runtime
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        if (subsystems.Count > 0)
        {
            xrInput = subsystems[0];
            Debug.Log("[Recenter] XRInputSubsystem found!");
        }
        else
        {
            Debug.LogError("[Recenter] XRInputSubsystem NOT found.");
        }
    }

    public void RecenterUser()
    {
        if (xrInput == null)
        {
            Debug.LogError("[Recenter] No XRInputSubsystem.");
            return;
        }

        bool ok = xrInput.TryRecenter();

        if (ok)
            Debug.Log("[Recenter] User recentered successfully.");
        else
            Debug.LogWarning("[Recenter] TryRecenter FAILED.");
    }
}
