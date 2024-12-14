<<<<<<< HEAD
﻿using UnityEngine;
using System.Collections;

namespace RootMotion
{

    /// <summary>
    /// The base abstract Singleton class.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {

        private static T sInstance = null;

        public static T instance
        {
            get
            {
                return sInstance;
            }
        }

        public static void Clear()
        {
            sInstance = null;
        }

        protected virtual void Awake()
        {
            if (sInstance != null) Debug.LogError(name + "error: already initialized", this);

            sInstance = (T)this;
        }
    }
}
=======
﻿using UnityEngine;
using System.Collections;

namespace RootMotion
{

    /// <summary>
    /// The base abstract Singleton class.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {

        private static T sInstance = null;

        public static T instance
        {
            get
            {
                return sInstance;
            }
        }

        public static void Clear()
        {
            sInstance = null;
        }

        protected virtual void Awake()
        {
            if (sInstance != null) Debug.LogError(name + "error: already initialized", this);

            sInstance = (T)this;
        }
    }
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
