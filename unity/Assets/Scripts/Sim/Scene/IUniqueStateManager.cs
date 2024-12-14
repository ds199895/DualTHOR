using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//记录和加载物体状态的接口
public interface IUniqueStateManager
{
    void SaveState(ObjectState state);
    void LoadState(ObjectState state);
}
