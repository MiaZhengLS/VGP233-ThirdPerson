using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncySurface : MonoBehaviour
{
    [SerializeField]
    private float maxSpeedIncrease;
    [SerializeField]
    private float maxDistance;
    [SerializeField]
    private AnimationCurve distFromCenterCurve;

    private Vector2 pos;

    private void Start()
    {
        pos = new Vector2(transform.position.x, transform.position.z);
    }

    public float GetSpeedIncrease(Vector2 characterPos)
    {
        var dist = Vector2.Distance(characterPos, pos);
        var percent = distFromCenterCurve.Evaluate(dist / maxDistance);
        return maxSpeedIncrease * percent;
    }
}
