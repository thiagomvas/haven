namespace Haven.Application.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static readonly Error NotFound = new("NOT_FOUND", "The requested resource was not found.");
    public static readonly Error Conflict = new("CONFLICT", "A resource with that name already exists.");

    public static readonly Error Unauthorized =
        new("UNAUTHORIZED", "You are not authorised to perform this action.");

    public static readonly Error Forbidden =
        new("FORBIDDEN", "You do not have permission to perform this action.");

    public static readonly Error NotSupported = new("NOT_SUPPORTED", "The requested operation is not supported.");
    public static readonly Error CancelledOperation = new("CANCELLED_OPERATION", "The operation was cancelled.");
    public static readonly Error Failed = new("FAILED", "The operation failed due to an unexpected error.");
    public static Error NotFoundFor(string resource, Guid id) => new("NOT_FOUND", $"{resource} '{id}' was not found.");
    public static Error ConflictFor(string resource, string name) =>
        new("CONFLICT", $"{resource} '{name}' already exists.");

    public static Error Validation(string message) => new("VALIDATION", message);
    public static Error InvalidOperation(string message) => new("INVALID_OPERATION", message);

    public static readonly Error InvalidSourceConfig = new("INVALID_SOURCE_CONFIG", "The source configuration is invalid.");
    public static readonly Error ManifestSyncFailed = new("MANIFEST_SYNC_FAILED", "Failed to sync from manifests.");
    public static readonly Error OperationAlreadyDone = new("OPERATION_ALREADY_DONE", "The operation has already been completed.");
    public static class Docker
    {
        public static readonly Error InvalidImage = new("DOCKER_INVALID_IMAGE", "The Docker image is invalid.");
        public static readonly Error InvalidDockerfile = new("DOCKER_INVALID_DOCKERFILE", "The Dockerfile is invalid.");
        public static readonly Error BuildFailed = new("DOCKER_BUILD_FAILED", "The Docker build failed.");
        public static readonly Error FailedToStartContainer = new("DOCKER_FAILED_TO_START_CONTAINER", "Failed to start the Docker container.");
        public static readonly Error ContainerCrashedAfterStart = new("DOCKER_CONTAINER_CRASHED_AFTER_START", "The container crashed immediately after starting.");
        public static readonly Error FailedToCreateNetwork = new("DOCKER_FAILED_TO_CREATE_NETWORK", "Failed to create the Docker network.");
        public static readonly Error ContainerNotFound = new("DOCKER_CONTAINER_NOT_FOUND", "The Docker container was not found.");
        public static readonly Error NetworkNotFound = new("DOCKER_NETWORK_NOT_FOUND", "The Docker network was not found.");
    }

    public static class Git
    {
        public static readonly Error InvalidCredentials = new("GIT_INVALID_CREDENTIALS", "The Git credentials are invalid.");
        public static readonly Error RepositoryNotFound = new("GIT_REPOSITORY_NOT_FOUND", "The Git repository was not found.");
        public static readonly Error CloneFailed = new("GIT_CLONE_FAILED", "Failed to clone the Git repository.");
    }

    public static class Deployment
    {
        public static readonly Error DeploymentNotInProgress = new("DEPLOYMENT_NOT_IN_PROGRESS", "Only in-progress deployments can be cancelled.");
    }
}