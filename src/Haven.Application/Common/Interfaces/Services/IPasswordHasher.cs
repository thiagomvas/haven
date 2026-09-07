namespace Haven.Application.Common.Interfaces.Services;

/// <summary>Produces htpasswd-compatible (bcrypt) password hashes, e.g. for Traefik's basicauth middleware.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
}