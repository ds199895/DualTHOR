#if PRIME_TWEEN_INSTALLED
using PrimeTween;
using UnityEngine;

namespace PrimeTweenDemo {
    public abstract class Animatable : MonoBehaviour {
        public virtual void OnClick() {
        }

        public abstract Sequence Animate(bool toEndValue);
    }
}
#endif