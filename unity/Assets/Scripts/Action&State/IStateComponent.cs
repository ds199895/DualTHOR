using UnityEngine;

/// <summary>
/// The interface that all state change components should implement
/// </summary>
public interface IStateComponent
{
    /// <summary>
    /// The method to execute the state change
    /// </summary>
    void Execute();
} 