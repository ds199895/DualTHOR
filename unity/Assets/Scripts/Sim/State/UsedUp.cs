<<<<<<< HEAD
﻿using UnityEngine;

public class UsedUp : MonoBehaviour,IUniqueStateManager
{
    [SerializeField]
    private GameObject usedUpObject;
    [SerializeField]
    private bool isUsedUp = false;

    public bool IsUsedUp => isUsedUp;
    public void SaveState(ObjectState objectState)
    {
        objectState.usedUpState = new UsedUpState
        {
            isUsedUp = isUsedUp,
        };
    }

    public void LoadState(ObjectState objectState)
    {
        isUsedUp = objectState.usedUpState.isUsedUp;
        usedUpObject.SetActive(!isUsedUp);
    }

    void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBeUsedUp);
#endif
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!isUsedUp)
            {
                UseUp();
            }
        }
    }

    public void UseUp()
    {
        usedUpObject.SetActive(false);
        isUsedUp = true;
    }
}
=======
﻿using UnityEngine;

public class UsedUp : MonoBehaviour,IUniqueStateManager
{
    [SerializeField]
    private GameObject usedUpObject;
    [SerializeField]
    private bool isUsedUp = false;

    public bool IsUsedUp => isUsedUp;
    public void SaveState(ObjectState objectState)
    {
        objectState.usedUpState = new UsedUpState
        {
            isUsedUp = isUsedUp,
        };
    }

    public void LoadState(ObjectState objectState)
    {
        isUsedUp = objectState.usedUpState.isUsedUp;
        usedUpObject.SetActive(!isUsedUp);
    }

    void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBeUsedUp);
#endif
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!isUsedUp)
            {
                UseUp();
            }
        }
    }

    public void UseUp()
    {
        usedUpObject.SetActive(false);
        isUsedUp = true;
    }
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
