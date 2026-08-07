namespace Haven.Application.Common.Interfaces;

/// <summary>Builds absolute frontend URLs for links embedded in system emails.</summary>
public interface IFrontendLinkBuilder
{
    string BuildAcceptInviteUrl(string rawToken);
}
