namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Short-lived storage for OAuth CSRF state tokens issued between an authorize redirect and its callback.
/// </summary>
public interface IOAuthStateStore
{
    string GenerateState();

    bool TryConsumeState(string state);
}
