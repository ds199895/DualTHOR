<<<<<<< HEAD
﻿using UnityEngine;
using System.Collections;

namespace RootMotion.Demos {

	/// <summary>
	/// Going slow motion on user input
	/// </summary>
	public class SlowMo : MonoBehaviour {

        public KeyCode[] keyCodes;
        public bool mouse0;
        public bool mouse1;
        public float slowMoTimeScale = 0.3f;

        void Update () {
			Time.timeScale = IsSlowMotion()? slowMoTimeScale: 1f;
		}

		private bool IsSlowMotion() {
			if (mouse0 && Input.GetMouseButton(0)) return true;
			if (mouse1 && Input.GetMouseButton(1)) return true;

			for (int i = 0; i < keyCodes.Length; i++) {
				if (Input.GetKey(keyCodes[i])) return true;
			}
			return false;
		}
	}
}
=======
﻿using UnityEngine;
using System.Collections;

namespace RootMotion.Demos {

	/// <summary>
	/// Going slow motion on user input
	/// </summary>
	public class SlowMo : MonoBehaviour {

        public KeyCode[] keyCodes;
        public bool mouse0;
        public bool mouse1;
        public float slowMoTimeScale = 0.3f;

        void Update () {
			Time.timeScale = IsSlowMotion()? slowMoTimeScale: 1f;
		}

		private bool IsSlowMotion() {
			if (mouse0 && Input.GetMouseButton(0)) return true;
			if (mouse1 && Input.GetMouseButton(1)) return true;

			for (int i = 0; i < keyCodes.Length; i++) {
				if (Input.GetKey(keyCodes[i])) return true;
			}
			return false;
		}
	}
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
