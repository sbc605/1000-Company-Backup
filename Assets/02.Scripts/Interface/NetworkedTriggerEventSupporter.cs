using UnityEngine;
using Fusion;
using System.Collections.Generic;

// 작성자 : 정하윤
public abstract class NetworkedTriggerEventSupporter : NetworkBehaviour
{
    [SerializeField]
    private string targetTag = "Player";

    private HashSet<Collider> targetsInside = new HashSet<Collider>();
    private HashSet<Collider> targetsThisFrame = new HashSet<Collider>();
    private void Awake()
    {
        targetsInside.Clear();

    }
    public override void Spawned()
    {
        targetsInside.Clear();
    }

    protected virtual void FixedUpdate()
    {
        targetsThisFrame.Clear();
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(targetTag))
            return;

        targetsThisFrame.Add(other);

        if (!targetsInside.Contains(other))
        {
            targetsInside.Add(other);
            OnTargetEnter(other);
        }
    }

    protected virtual void LateUpdate()
    {
        var exited = new List<Collider>();

        foreach (var col in targetsInside)
        {
            if (!targetsThisFrame.Contains(col))
                exited.Add(col);
        }

        foreach (var col in exited)
        {
            targetsInside.Remove(col);
            OnTargetExit(col);
        }
    }

    /// <summary>
    /// 자식 클래스가 오버라이드할 부분
    /// </summary>
    /// <param name="other"></param>
    protected virtual void OnTargetEnter(Collider other) { }
    protected virtual void OnTargetExit(Collider other) { }

    public void ChangeTag(string tag)
    {
        targetTag = tag;
    }
}