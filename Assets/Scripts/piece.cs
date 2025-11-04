using UnityEngine;

using global::OVR; 

using System;

public class SimpleRespawn : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform spawnPoint;   // Objeto que hace de spawn

    [Header("Opciones")]
    [SerializeField] private bool copyRotation = true;
    [SerializeField] private bool resetOnStart = true;

    [SerializeField] private Material touched;
    [SerializeField] private Material original; 

    Rigidbody rb;

    bool isBeingTouched = false;

    public Transform getSpawnPoint()
    {
        return spawnPoint;
    }

    public bool IsBeingTouched()
    {
        return isBeingTouched;
    }

    public void SetSpawnPoint(Transform newSpawnPoint)
    {
        spawnPoint = newSpawnPoint;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (spawnPoint == null)
            Debug.LogWarning($"[{name}] No hay spawnPoint asignado.");
    }

    void Start()
    {
        if (resetOnStart) Respawn();
    }

    void Update()
    {
        // if object below certain Y level, respawn
        if (transform.position.y < 1.0f) Respawn();
    }

    public void Respawn()
    {
        // Anular inercia antes de mover
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Evita empujones del motor físico durante el teletransporte
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;

            TeleportToSpawn();

            rb.isKinematic = wasKinematic;
        }
        else
        {
            TeleportToSpawn();
        }

        ChangeMaterial(original);
    }

    void ChangeMaterial(Material mat)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = mat;
        }
    }

    void TeleportToSpawn()
    {
        if (!spawnPoint) return;
        transform.position = spawnPoint.position;
        if (copyRotation) transform.rotation = spawnPoint.rotation;
    }

    void OnCollisionEnter(Collision collision)
    {

    }

    void OnCollisionExit(Collision collision)
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "PinchArea" || other.gameObject.name == "PinchPointRange")
        {
            isBeingTouched = true;
            ChangeMaterial(touched);
        } 
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "PinchArea" || other.gameObject.name == "PinchPointRange")
        {
            isBeingTouched = false;
            ChangeMaterial(original);
        }
    }
}
