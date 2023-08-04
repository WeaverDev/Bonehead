using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowGizmo : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.1f);
        //Gizmos.DrawRay(transform.position, transform.forward);
        //Gizmos.DrawRay(transform.position + transform.forward, Quaternion.AngleAxis(120, transform.up) * transform.forward * 0.2f);
        //Gizmos.DrawRay(transform.position + transform.forward, Quaternion.AngleAxis(-120, transform.up) * transform.forward * 0.2f);
    }
}
