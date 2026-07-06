namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Short-lived storage for OAuth CSRF state tokens issued between an authorize redirect and its callback.
/// </summary>
public interface IOAuthStateStore
{
    /// <summary>
    /// Generates a state token. If <paramref name="credentialId"/> is set, the callback will rotate that
    /// existing credential's tokens instead of creating a new one.
    /// </summary>
    string GenerateState(Guid? credentialId = null);

    bool TryConsumeState(string state, out Guid? credentialId);
}
