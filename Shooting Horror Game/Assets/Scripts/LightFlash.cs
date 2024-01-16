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


    /// 0.05초 후에 오브젝트가 비활성화 되도록 한다
    private IEnumerator AutoDisable()
    {
        yield return new WaitForSeconds(disableTime);

        gameObject.SetActive(false);
    }
}
