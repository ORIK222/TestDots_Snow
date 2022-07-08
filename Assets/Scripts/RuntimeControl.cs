using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuntimeControl : MonoBehaviour
{
    public static RuntimeControl Instance;

    [SerializeField] private GameObject targetControlObject;
    
    [SerializeField]
    private bool useVRDevice;

    [SerializeField]
    private float mouseSensitivity = 1;

    [SerializeField][Range(2,10)] private float moveSpeed = 3;

    [SerializeField]
    private GameObject[] vrObjects;

    [SerializeField]
    private GameObject[] nonVRObjects;

    private bool lastControlState;
    List<Canvas> canvases = new List<Canvas>();
    public void SetVrControl() => useVRDevice = true;

    public bool IsVR => useVRDevice;

    public void SetLastControl() => useVRDevice = lastControlState; 
    // Start is called before the first frame update

    private void OnValidate()
    {
        if (!Instance)
            Instance = this;

        lastControlState = useVRDevice;
    }

    void Awake()
    {
#if UNITY_EDITOR || UNITY_WEBGL
        canvases.AddRange(GameObject.FindObjectsOfType<Canvas>(true));
        CheckForVRDeviceSetting();
#endif
    }

#if UNITY_EDITOR || UNITY_WEBGL

    private bool isCanvasLocked;
    private bool isRotateLocked;

    private bool IsCanvasHit()
    {
        bool isCanvasHit = false;

        //Code to be place in a MonoBehaviour with a GraphicRaycaster component
        
        foreach (var canvas in canvases)
        {
            GraphicRaycaster gr = canvas.gameObject.GetComponent<GraphicRaycaster>();
            if (gr != null)
            {
                //Create the PointerEventData with null for the EventSystem
                PointerEventData ped = new PointerEventData(null);
                //Set required parameters, in this case, mouse position
                ped.position = Input.mousePosition;
                //Create list to receive all results
                List<RaycastResult> results = new List<RaycastResult>();
                //Raycast it
                gr.Raycast(ped, results);
                if (results.Count > 0)
                {
                    isCanvasHit = true;
                    break;
                }
            }
        }

        return isCanvasHit;
    }


    // Update is called once per frame
    void Update()
    {
        CheckForVRDeviceSetting();
        if (!useVRDevice)
        {
            if (!isCanvasLocked && !isRotateLocked && IsCanvasHit())
            {
                isCanvasLocked = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isCanvasLocked = false;
                isRotateLocked = false;
            }

            if (!isCanvasLocked && Input.GetMouseButton(0))
            {
                float rotationX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float rotationY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                var q = Quaternion.Euler(new Vector3(-rotationY, rotationX, 0));
                targetControlObject.transform.rotation *= q;
                var current = targetControlObject.transform.rotation.eulerAngles;
                // compensate horizontal axis so that camera is always parallel to the ground.
                Camera.main.transform.rotation *= Quaternion.Euler(new Vector3(0, 0, -current.z));
                isRotateLocked = true;
            }

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 move = targetControlObject.transform.right * moveX + targetControlObject.transform.forward * moveZ;
            targetControlObject.GetComponent<CharacterController>().Move(move * moveSpeed * Time.deltaTime);
        }
    }

    private void CheckForVRDeviceSetting()
    {
        Camera vrCamera = null;
        Camera nonVrCamera = null;
        foreach(var vr in vrObjects)
        {
            if (vr != null)
            {
                Camera c= vr.gameObject.GetComponent<Camera>();
                if (c != null)
                {
                    vrCamera = c;
                }
                vr.SetActive(useVRDevice);
            }
        }

        foreach (var nonvr in nonVRObjects)
        {
            if (nonvr != null)
            {
                Camera c = nonvr.gameObject.GetComponent<Camera>();
                if (c != null)
                {
                    nonVrCamera = c;
                }
                nonvr.SetActive(!useVRDevice);
            }
        }

        if (targetControlObject != null)
        {
            targetControlObject.SetActive(!useVRDevice);
        }

        Camera camera = vrCamera;
        if (!useVRDevice)
        {
            camera = nonVrCamera;
        }

        foreach(var canvas in canvases)
        {
            canvas.worldCamera = camera;
        }
    }
#endif
}
