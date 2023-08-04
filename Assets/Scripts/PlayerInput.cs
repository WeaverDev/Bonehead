using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] Transform cam;
    [SerializeField] Transform camPivot;
    [SerializeField] Transform pill;
    [SerializeField] Transform lookTarget;
    [SerializeField] List<Transform> movePoints;

    [SerializeField] float pillAcceleration = 1;
    [SerializeField] float pillSpeed = 1;
    [SerializeField] float keyboardSensitivity = 1;

    Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
    Vector3 pillTargetPoint;
    bool raiseCube;

    Vector3 camTargetAngle;
    SmoothDamp.EulerAngles camAngle;
    float camTargetDist;
    SmoothDamp.Float camDist;

    void Awake()
    {
        camTargetAngle = camPivot.localEulerAngles;
        camTargetDist = cam.localPosition.z;
    }

    // Loop through predefined points and move the pill there
    IEnumerator Start()
    {
        while (true)
        {
            foreach (var movePoint in movePoints)
            {
                pillTargetPoint = movePoint.position;
                yield return new WaitForSeconds(1.35f);
            }
        }
    }

    void Update()
    {
        // Toggle cube
        if (Input.GetMouseButtonDown(1))
        {
            raiseCube = !raiseCube;
        }

        // Set pill destination to mouse position on XZ plane
        if (Input.GetMouseButton(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (xzPlane.Raycast(ray, out float hitDist))
            {
                pillTargetPoint = ray.origin + ray.direction * hitDist;
            }
        }

        // Move pill
        pill.position = Vector3.Lerp(pill.position, pillTargetPoint, 1 - Mathf.Exp(-pillSpeed * Time.deltaTime));

        lookTarget.localPosition = Vector3.Lerp(
            lookTarget.localPosition,
            Vector3.up * (raiseCube ? 3f : 1f),
            1 - Mathf.Exp(-pillAcceleration * Time.deltaTime)
        );

        camTargetAngle += new Vector3(
            -Input.GetAxisRaw("Vertical") * keyboardSensitivity * Time.deltaTime,
            -Input.GetAxisRaw("Horizontal") * keyboardSensitivity * Time.deltaTime,
            0
        );

        // Limit up/down look angle
        camTargetAngle.x = Mathf.Clamp((camTargetAngle.x + 180) % 360 - 180, -80, 80);

        camAngle.Step(camTargetAngle, 8f);
        camPivot.localEulerAngles = camAngle;

        camTargetDist -= Input.mouseScrollDelta.y;
        camDist.Step(camTargetDist, 8f);

        cam.localPosition = Vector3.forward * camDist;
    }
}
