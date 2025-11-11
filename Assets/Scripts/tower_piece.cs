using Oculus.Interaction;

using UnityEngine;

using global::OVR; 

using System;

public class Tower_Piece : MonoBehaviour
{

    [Header("Refs")]
    [SerializeField] private Transform spawnPoint;

    [Header("Opciones")]
    [SerializeField] private bool copyRotation = true;
    [SerializeField] private bool resetOnStart = true;

    public bool IsGrabbed { get; private set; }
    public bool MoveAvatar { get; set; } = false;
    public bool OnBase { get; set; } = false;

    private GameObject avatarHandPose;
    private Vector3 piece_hand_offset = new Vector3(0.0f, 0.05f, 0.1f);

    [SerializeField] private Material touched;
    [SerializeField] private Material original;

    Rigidbody rb;

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
    }

    void TeleportToSpawn()
    {
        if (!spawnPoint) return;
        transform.position = spawnPoint.position;
        if (copyRotation) transform.rotation = spawnPoint.rotation;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (resetOnStart) Respawn();
    }

    private void Update()
    {
        if (IsGrabbed)
        {
            ChangeMaterial(touched);
        }
        else
        {
            ChangeMaterial(original);
        }

        if (MoveAvatar && avatarHandPose != null)
        {
            this.transform.position = avatarHandPose.transform.position - piece_hand_offset;
            this.transform.rotation = avatarHandPose.transform.rotation;
        }

        if (transform.position.y < 0.9f) Respawn();
    }

    public void ChangeMaterial(Material mat)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = mat;
        }
    }

    public void Disable()
    {
        //this.enabled = false;
        Destroy(this.gameObject);
    }

    public void Enable()
    {
        Respawn();
        this.enabled = true;
    }

    public void SetAvatarHandPose(GameObject handPose)
    {
        avatarHandPose = handPose;
        MoveAvatar = true;
    }

    public void ClearAvatarHandPose()
    {
        avatarHandPose = null;
        MoveAvatar = false;
    }
}
