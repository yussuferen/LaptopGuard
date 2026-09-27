namespace LaptopGuard.Sensors;

public interface ISensor : IDisposable
{
    string Name { get; }
    bool IsActive { get; }

    void Start();
    void Stop();

    event Action<string>? TriggerDetected;
}
