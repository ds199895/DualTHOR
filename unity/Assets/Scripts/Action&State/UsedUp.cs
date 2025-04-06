using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 管理游戏对象在特定状态下的行为变化，特别是当对象被“用尽”时，控制其可见性和物理交互状态
// 这在游戏中可能用于实现消耗品、一次性使用物品或其他需要状态转换的对象。
public class UsedUp : MonoBehaviour {
    [SerializeField]
    protected MeshRenderer usedUpRenderer;

    [SerializeField]
    protected Collider[] usedUpColliders;

    [SerializeField]
    protected Collider[] usedUpTriggerColliders;

    [SerializeField]
    protected Collider[] alwaysActiveColliders;

    [SerializeField]
    protected Collider[] alwaysActiveTriggerColliders;

    public bool isUsedUp = false;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    public void UseUp() {
        usedUpRenderer.enabled = false;

        // disable all colliders that are used up
        foreach (Collider col in usedUpColliders) {
            col.enabled = false;
        }

        // disable all trigger colliders that are used up
        foreach (Collider col in usedUpTriggerColliders) {
            col.enabled = false;
        }

        // reference to SimObjPhysics component to
        SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();

        // set colliders to ones active while used up
        sop.MyColliders = alwaysActiveColliders;

        // set trigger colliders to ones active while used up

        isUsedUp = true;
    }
}
