using UnityEngine;

/// <summary>
/// 所有状态变化组件应实现的接口
/// </summary>
public interface IStateComponent
{
    /// <summary>
    /// 执行状态变化的方法
    /// </summary>
    void Execute();
} 