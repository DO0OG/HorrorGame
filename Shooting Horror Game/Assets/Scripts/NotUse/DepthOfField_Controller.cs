using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class DepthOfField_Controller : MonoBehaviour
{
    [Header("PostProcess")]
    public VolumeProfile volumeProfile;
    DepthOfField depthOfField;

    Ray raycast;
    RaycastHit hit;
    bool isHit;
    float hitDistance;

    public float focusSpeed = 15f;
    public float maxFocusDistance = 50f;

    public GameObject focusedObject;
    private int focusedObjectLayerMask;

    private void Start()
    {
        // VolumeProfile���� DepthOfField ������ ��������
        volumeProfile.TryGet(out depthOfField);

        // focusedObject�� ���̾ �����ͼ� ����ĳ��Ʈ���� ������ ���̾� ����ũ�� ����
        if (focusedObject != null)
        {
            focusedObjectLayerMask = 1 << focusedObject.layer;
        }
    }

    // Update is called once per frame
    void Update()
    {
        raycast = new Ray(transform.position, transform.forward * maxFocusDistance);

        isHit = false;
        // focusedObject�� ���̾ �����ϰ� ����ĳ��Ʈ ����
        if (Physics.Raycast(raycast, out hit, maxFocusDistance, ~focusedObjectLayerMask))
        {
            isHit = true;
            hitDistance = Vector3.Distance(transform.position, hit.point);
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
