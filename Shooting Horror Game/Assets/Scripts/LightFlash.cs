using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlash : MonoBehaviour
{
    public float disableTime = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        StartCoroutine(AutoDisable());
    }

    // Update is called once per frame
    void Update()
    {

    }


    /// 0.05�� �Ŀ� ������Ʈ�� ��Ȱ��ȭ �ǵ��� �Ѵ�
    private IEnumerator AutoDisable()
    {
        yield return new WaitForSeconds(disableTime);

        gameObject.SetActive(false);
    }
}
