using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLifeTimeController : MonoBehaviour
{

    public void StartLife(float duration)
    {
        StartCoroutine(LifeTimeRoutine(duration));
    }

    private IEnumerator LifeTimeRoutine(float duration)
    {
        float runTime = 0;
        while (runTime< duration)
        {
            yield return null;
            runTime += Time.deltaTime;
        }
        GameObject.Destroy(gameObject);
    }
}
