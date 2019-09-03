using UnityEngine;

public interface IUpdatable
{
    Bounds Bounds { get; }
    void EveryFrameUpdate(float deltaAuts);

    void FixedFrequencyUpdate(float deltaAuts);
}