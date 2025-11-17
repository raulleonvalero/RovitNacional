//using Piper;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RovitNacional;
using System.Collections;

public class movement : MonoBehaviour
{
    InputAction moveAction;
    InputAction lookAction;
    InputAction VmoveAction;
    InputAction jumpAction;

    bool isSpeaking;

    //public Camera characterCamera;
    public GameObject characterBody;

    //public PiperManager piper;
    public AudioSource audioSpeaker;

    public GameObject rightObject;
    public Transform goal;

    private Character character;

    private int i = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        VmoveAction = InputSystem.actions.FindAction("VerticalMove");
        jumpAction = InputSystem.actions.FindAction("Jump");

        isSpeaking = false;

        character = GameObject.Find("Character1").GetComponent<Character>();

    }
    

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 moveValues = moveAction.ReadValue<Vector2>();
        Vector2 lookValues = lookAction.ReadValue<Vector2>();
        float VmoveValues = VmoveAction.ReadValue<float>();
        bool jumpValues = jumpAction.ReadValue<float>() > 0f;

        lookValues *= 5;
        float speed = 0.1f;
        
        Debug.Log("Move: " + moveValues);
        Debug.Log("Look: " + lookValues);
        Debug.Log("Vmove: " + VmoveValues);
        Debug.Log("jump: " + jumpValues);

        Vector3 rotation = new Vector3(0f, -lookValues.x, 0f);
        Vector3 position = new Vector3(moveValues.x, moveValues.y, VmoveValues);
        

        Quaternion deltaRotation = Quaternion.Euler(-rotation * Time.fixedDeltaTime);

        //characterBody.MoveRotation(characterBody.rotation * deltaRotation);
        //characterCamera.transform.Rotate(transform.right, -lookValues.y * Time.fixedDeltaTime);

        Vector3 forwardmove = (Camera.main.transform.forward * moveValues.y) * speed;
        Vector3 rightMove = (Camera.main.transform.right * moveValues.x) * speed;
        Vector3 upMove = (Camera.main.transform.up * VmoveValues) * speed;

        Vector3 totalMove = forwardmove + rightMove + upMove;

        characterBody.transform.position += totalMove;

        ///characterBody.linearVelocity = position * 10.0f;

        //characterBody.MovePosition(characterBody.position * (position * Time.fixedDeltaTime));

        if (jumpValues)
        {
            character.setMode(Activity.GoStopGo, Mode.Down);
            character.Speak("action_touch_cube_red_2");
            
        }
            

    }
}
