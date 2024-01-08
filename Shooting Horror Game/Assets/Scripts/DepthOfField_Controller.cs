using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class DepthOfField_Controller : MonoBehaviour
{
    Ray raycast;
    RaycastHit hit;
    bool isHit;
    float hitDistance;

    public VolumeProfile volumeProfile;

    DepthOfField depthOfField;

    public float focusSpeed = 15f;
    public float maxFocusDistance = 50f;

    private void Start()
    {
        // VolumeProfile에서 DepthOfField 설정을 가져오기
        volumeProfile.TryGet(out depthOfField);
    }

    // Update is called once per frame
    void Update()
    {
        raycast = new Ray(transform.position, transform.forward * maxFocusDistance);

        isHit = false;

        if (Physics.Raycast(raycast, out hit, maxFocusDistance))
        {
            isHit = true;
            hitDistance = Vector3.Distance(transform.position, hit.point);
            //Debug.Log("Hit");
        }
        else
        {
            if (hitDistance < maxFocusDistance)
            {
                hitDistance++;
            }
        }

        SetFocus();
    }

    void SetFocus()
    {
        depthOfField.focusDistance.value = Mathf.Lerp(depthOfField.focusDistance.value, hitDistance, Time.deltaTime * focusSpeed);
    }

    private void OnDrawGizmos()
    {
        if (isHit)
        {
            Gizmos.DrawSphere(hit.point, 0.1f);

            Debug.DrawRay(transform.position, transform.forward * Vector3.Distance(transform.position, hit.point));
        }
        else
        {
            Debug.DrawRay(transform.position, transform.forward * 100f);
        }
    }
}
