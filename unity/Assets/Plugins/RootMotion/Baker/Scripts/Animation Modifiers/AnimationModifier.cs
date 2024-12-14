<<<<<<< HEAD
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RootMotion
{

    public abstract class AnimationModifier : MonoBehaviour
    {
        protected Animator animator;
        protected Baker baker;

        public virtual void OnInitiate(Baker baker, Animator animator)
        {
            this.baker = baker;
            this.animator = animator;
        }

        public virtual void OnStartClip(AnimationClip clip) { }

        public virtual void OnBakerUpdate(float normalizedTime) { }
    }
}
=======
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RootMotion
{

    public abstract class AnimationModifier : MonoBehaviour
    {
        protected Animator animator;
        protected Baker baker;

        public virtual void OnInitiate(Baker baker, Animator animator)
        {
            this.baker = baker;
            this.animator = animator;
        }

        public virtual void OnStartClip(AnimationClip clip) { }

        public virtual void OnBakerUpdate(float normalizedTime) { }
    }
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
