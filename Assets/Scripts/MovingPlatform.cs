using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField]
    private Vector3 startPos;
    [SerializeField]
    private Vector3 endPos;
    [SerializeField]
    private float moveTime;
    [SerializeField]
    private float rotateSpeed;
    [SerializeField]
    private float returnDelayTime;

    private Vector3 curStartPos;
    private Vector3 curEndPos;
    private float moveTimer;

    private Vector3 moveSpeed;
    private Vector3 rotateEulerAngle;

    private Transform tr;

    void Start()
    {
        tr = transform;
        curStartPos = startPos;
        curEndPos = endPos;
        rotateEulerAngle = Vector3.up * rotateSpeed;
    }

    void Update()
    {
        moveTimer += Time.deltaTime;
        float percent = moveTimer / moveTime;
        if(percent >= 1)
        {
            if(moveTimer >= moveTime + returnDelayTime)
            {
                var tmp = curStartPos;
                curStartPos = curEndPos;
                curEndPos = tmp;
                moveTimer = 0f;
            }
            else
            {
                // Do nothing, wait
            }
        }
        else
        {
            var oldPos = tr.position;
            tr.position = Vector3.Lerp(curStartPos, curEndPos, Mathf.SmoothStep(0, 1f, percent));
            moveSpeed = tr.position - oldPos;
        }
        if(rotateSpeed > 0)
        {
            tr.Rotate(rotateEulerAngle * Time.deltaTime);
        }
    }

    public Vector3 MoveSpeed
    {
        get
        {
            return moveSpeed;
        }
    }

    public Vector3 RotateSpeed
    {
        get
        {
            return rotateEulerAngle * Time.deltaTime;
        }
    }
}
