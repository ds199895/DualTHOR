using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//��¼�ͼ�������״̬�Ľӿ�
public interface IUniqueStateManager
{
    void SaveState(ObjectState state);
    void LoadState(ObjectState state);
}
