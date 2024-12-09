using UnityEngine;

//1.在物体破碎后，生成一个爆炸力，使碎片向四周散开
//2.为每个碎片添加一个随机的扭矩（旋转力），使碎片在散开的同时旋转
//3.将所有碎片的刚体组件添加到场景管理器中，以便后续处理
public class Breakdown : MonoBehaviour {
    [SerializeField]
    private float power = 10.0f;
    [SerializeField]
    private  float explosionRadius = 0.25f; // 爆炸力的作用半径

    public void StartBreak() {
        Vector3 explosionPos = transform.position;  // 爆炸的中心位置
        Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius); // 获取爆炸范围内的所有碰撞器 
        foreach (Collider col in colliders) {
            // 如果碰撞器有刚体组件
            if (col.GetComponent<Rigidbody>()) {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                //// 添加爆炸力
                rb.AddExplosionForce(power, gameObject.transform.position, explosionRadius, 0.005f);
                rb.AddTorque(new Vector3(Random.value, Random.value, Random.value)); // 添加随机扭矩
            }
        }
    }

}
