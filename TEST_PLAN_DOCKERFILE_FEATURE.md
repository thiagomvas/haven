# Test Plan: Dockerfile Service Deployment Feature

## Feature Overview
This branch implements Dockerfile service support in Haven, allowing services to be deployed from either:
1. **Git source**: Clone/pull a Dockerfile from a remote Git repository
2. **Raw source**: Deploy from raw Dockerfile content provided directly

Key components:
- `DockerfileConfig` value object with source selection
- `DockerfileDeployService` for deployment orchestration
- `GitService` for Git operations (clone, pull, get remote branches)
- `GetRemoteBranches` query for frontend branch autocomplete
- Frontend UI for Dockerfile service creation/editing with Git credential support

---

## Domain Layer Tests

### 1. DockerfileConfig Value Object Tests
**File**: `tests/Haven.Domain.Tests/ValueObjects/DockerfileConfigTests.cs`

#### Test Cases:
- [ ] **DockerfileConfig_WithGitSource_IsValidWhenRepositoryAndBranchProvided**
  - Create config with Git source, repository URL, and branch
  - Assert property values are correctly set
  
- [ ] **DockerfileConfig_WithRawSource_IsValidWhenContentProvided**
  - Create config with Raw source and Dockerfile content
  - Assert property values are correctly set

- [ ] **DockerfileConfig_WithGitSource_CanIncludeCustomFilePath**
  - Create config with Git source and custom FilePath (e.g., "docker/Dockerfile.prod")
  - Assert FilePath is preserved correctly

- [ ] **DockerfileConfig_WithGitSource_CanIncludeGitCredentialId**
  - Create config with Git source and GitCredentialId for authentication
  - Assert GitCredentialId is stored correctly

- [ ] **DockerfileConfig_IsSerializable**
  - Create config, serialize/deserialize
  - Assert all properties are preserved through serialization

### 2. DockerfileSource Enum Tests
**File**: `tests/Haven.Domain.Tests/Enums/DockerfileSourceTests.cs`

#### Test Cases:
- [ ] **DockerfileSource_HasGitValue**
  - Assert DockerfileSource.Git exists and has correct value
  
- [ ] **DockerfileSource_HasRawValue**
  - Assert DockerfileSource.Raw exists and has correct value

---

## Application Layer Tests

### 3. CreateService Validator Tests (Dockerfile support)
**File**: `tests/Haven.Application.Tests/Features/Services/Commands/CreateService/CreateServiceValidatorDockerfileTests.cs`

#### Shared Setup:
- Valid project and environment IDs
- Valid service name (matching `HavenServiceName.ValidPattern`)

#### Test Cases for Dockerfile Type - Git Source:
- [ ] **Validator_DockerfileWithGitSource_RequiresRepository**
  - Command with Dockerfile type, Git source, but no repository
  - Assert validation fails with "Repository URL is required" message

- [ ] **Validator_DockerfileWithGitSource_RequiresBranch**
  - Command with Dockerfile type, Git source, repository, but no branch
  - Assert validation fails with "Branch is required" message

- [ ] **Validator_DockerfileWithGitSource_IsValidWhenRepositoryAndBranchProvided**
  - Command with all required fields for Git source
  - Assert validation passes

- [ ] **Validator_DockerfileWithGitSource_AllowsOptionalFilePath**
  - Command with Git source, repository, branch, and FilePath
  - Assert validation passes

- [ ] **Validator_DockerfileWithGitSource_AllowsOptionalGitCredentialId**
  - Command with Git source and GitCredentialId
  - Assert validation passes

#### Test Cases for Dockerfile Type - Raw Source:
- [ ] **Validator_DockerfileWithRawSource_RequiresContent**
  - Command with Dockerfile type, Raw source, but no content
  - Assert validation fails with "Dockerfile content is required" message

- [ ] **Validator_DockerfileWithRawSource_IsValidWhenContentProvided**
  - Command with Dockerfile type, Raw source, and content
  - Assert validation passes

- [ ] **Validator_DockerfileWithRawSource_IgnoresRepositoryField**
  - Command with Raw source and repository provided (shouldn't be required)
  - Assert validation passes

- [ ] **Validator_DockerfileWithRawSource_IgnoresBranchField**
  - Command with Raw source and branch provided (shouldn't be required)
  - Assert validation passes

#### Test Cases for Source Type Switching:
- [ ] **Validator_SwitchingFromGitToRaw_OnlyRequiresContent**
  - Update command changing source from Git to Raw
  - Assert only Content validation is applied, not Repository/Branch

- [ ] **Validator_SwitchingFromRawToGit_RequiresRepositoryAndBranch**
  - Update command changing source from Raw to Git
  - Assert Repository and Branch are now required

### 4. UpdateService Validator Tests (Dockerfile support)
**File**: `tests/Haven.Application.Tests/Features/Services/Commands/UpdateService/UpdateServiceValidatorDockerfileTests.cs`

#### Test Cases (parallel to CreateService tests, but using Optional pattern):
- [ ] **Validator_UpdateDockerfileGitSource_RequiresRepositoryIfProvided**
  - Command with DockerfileConfig.HasValue, Git source, but empty repository
  - Assert validation fails

- [ ] **Validator_UpdateDockerfileGitSource_RequiresBranchIfProvided**
  - Command with DockerfileConfig.HasValue, Git source, but empty branch
  - Assert validation fails

- [ ] **Validator_UpdateDockerfileRawSource_RequiresContentIfProvided**
  - Command with DockerfileConfig.HasValue, Raw source, but empty content
  - Assert validation fails

- [ ] **Validator_UpdateDockerfilePartialUpdate_AllowsOnlyUpdatingRepository**
  - Update only the repository URL while keeping other config
  - Assert validation passes (Optional pattern allows partial updates)

- [ ] **Validator_UpdateDockerfilePartialUpdate_AllowsOnlyUpdatingBranch**
  - Update only the branch while keeping other config
  - Assert validation passes

### 5. GetRemoteBranches Query Tests
**File**: `tests/Haven.Application.Tests/Features/Git/Queries/GetRemoteBranches/GetRemoteBranchesTests.cs`

#### Test Cases:
- [ ] **Handler_GetRemoteBranches_WithoutCredentials_ReturnsSuccess**
  - Query with repository URL but no credentials
  - Assert handler returns success result with branch list
  - *Note: Current handler returns hardcoded ["Foo", "bar", "fizz"] - this is incomplete*

- [ ] **Handler_GetRemoteBranches_WithCredentials_LoadsCredentialsFromRepository**
  - Query with repository URL and GitCredentialId
  - Mock repository to return valid GitCredentials
  - Assert handler fetches branches with credentials

- [ ] **Handler_GetRemoteBranches_WithInvalidCredentialId_ReturnsNotFound**
  - Query with non-existent GitCredentialId
  - Assert handler returns NotFound error

- [ ] **Validator_GetRemoteBranches_RequiresRepositoryUrl**
  - Query with empty repository URL
  - Assert validation fails

- [ ] **Validator_GetRemoteBranches_AllowsEmptyCredentialId**
  - Query with repository URL but no credentials ID
  - Assert validation passes

---

## Infrastructure Layer Tests

### 6. GitService Tests
**File**: `tests/Haven.Infrastructure.Tests/Deployment/Git/GitServiceTests.cs`

#### Mock Dependencies:
- `IGitRepositoryPathProvider` - provides repository paths
- `IGitCredentialsRepository` - provides stored credentials
- `IGitProviderFactory` - creates Git provider instances
- `ILogger<GitService>`

#### Test Cases:

##### Clone Repository:
- [ ] **CloneServiceRepository_WithValidUrl_CreatesDirectoryAndClones**
  - Mock git provider to succeed
  - Assert directory is created and repository path is returned
  - Assert LogInformation called with correct path

- [ ] **CloneServiceRepository_WithInvalidUrl_ReturnsFailure**
  - Mock git provider to throw exception
  - Assert returns Error.Failure with appropriate message
  - Assert LogError called

- [ ] **CloneServiceRepository_WhenParentDirectoryDoesNotExist_CreatesIt**
  - Service ID with non-existent parent path
  - Assert parent directory is created before cloning

- [ ] **CloneServiceRepository_WhenDestinationExistsButEmpty_DeletesAndClones**
  - Pre-create empty destination directory
  - Assert directory is deleted and fresh clone occurs

- [ ] **CloneServiceRepository_WhenDestinationExistsWithFiles_DoesNotDelete**
  - Pre-create destination with files (simulating partial failure)
  - Assert clone attempt proceeds (or appropriate handling)

##### Pull Repository:
- [ ] **PullServiceRepository_WithExistingRepository_PullsFromBranch**
  - Mock git provider to succeed
  - Assert repository is pulled
  - Assert LogInformation called with service ID and branch

- [ ] **PullServiceRepository_WithNonExistentRepository_ReturnsNotFound**
  - Mock repository path provider to indicate missing directory
  - Assert returns Error.NotFoundFor("Repository", serviceId)

- [ ] **PullServiceRepository_WithPullFailure_ReturnsFailure**
  - Mock git provider to throw exception
  - Assert returns Error.Failure
  - Assert LogError called

##### Get Remote Branches:
- [ ] **GetRemoteBranches_WithoutCredentials_CallsProviderAndReturnsBranches**
  - Mock git provider to return branch list
  - Assert returns success result with branches
  - Assert credentials passed as null

- [ ] **GetRemoteBranches_WithCredentials_PassesCredentialsToProvider**
  - Create GitCredentials, pass to service
  - Assert git provider is called with credentials
  - Assert returns branch list

- [ ] **GetRemoteBranches_WhenProviderThrows_ReturnsFailure**
  - Mock git provider to throw exception
  - Assert returns Error.Failure
  - Assert LogError called with repository URL

##### Repository Path and Existence:
- [ ] **GetServiceRepositoryPath_WhenRepositoryExists_ReturnsPath**
  - Mock repository exists
  - Assert returns correct path

- [ ] **GetServiceRepositoryPath_WhenRepositoryNotExists_ReturnsNull**
  - Mock repository does not exist
  - Assert returns null

- [ ] **ServiceRepositoryExists_WhenRepositoryExists_ReturnsTrue**
  - Mock repository exists
  - Assert returns true

- [ ] **ServiceRepositoryExists_WhenRepositoryNotExists_ReturnsFalse**
  - Mock repository does not exist
  - Assert returns false

##### Delete Repository:
- [ ] **DeleteServiceRepository_WhenRepositoryExists_DeletesDirectory**
  - Create mock directory
  - Assert directory is deleted
  - Assert LogInformation called

- [ ] **DeleteServiceRepository_WhenRepositoryNotExists_DoesNotThrow**
  - Mock non-existent directory
  - Assert no exception thrown
  - Assert returns completed task

- [ ] **DeleteServiceRepository_WhenDeleteFails_CatchesExceptionAndLogs**
  - Mock exception during delete
  - Assert exception is caught
  - Assert LogError called

### 7. GitProviderFactory Tests
**File**: `tests/Haven.Infrastructure.Tests/Deployment/Git/GitProviderFactoryTests.cs`

#### Test Cases:
- [ ] **Factory_CreateGenericProvider_ReturnsGenericGitProvider**
  - Create with GitProviderType.Generic
  - Assert returns IGitProvider instance

- [ ] **Factory_CreateGenericProviderWithCredentials_PassesCredentialsToProvider**
  - Create with credentials
  - Assert provider is initialized with credentials

- [ ] **Factory_CreateWithUnsupportedType_ThrowsNotSupportedException**
  - Create with invalid provider type
  - Assert throws NotSupportedException

### 8. GenericGitProvider Tests
**File**: `tests/Haven.Infrastructure.Tests/Deployment/Git/GenericGitProviderTests.cs`

#### Mock Dependencies:
- System Git/LibGit2Sharp operations
- `IEncryptionService` for credential decryption
- `ILogger<GenericGitProvider>`

#### Test Cases:

##### Clone:
- [ ] **Clone_WithValidRepositoryUrl_ClonesSuccessfully**
  - Valid repository URL, destination path
  - Assert Repository.Clone is called
  - Assert LogInformation called

- [ ] **Clone_WithAuthentication_UsesCredentialsProvider**
  - Clone with GitCredentials (token auth)
  - Assert CloneOptions includes credentials provider
  - Assert credentials are decrypted before use

- [ ] **Clone_WithException_ThrowsAndLogs**
  - LibGit2Sharp throws exception
  - Assert exception is re-thrown
  - Assert LogError called

##### Pull:
- [ ] **Pull_HasNotImplementedBehavior**
  - Call Pull method
  - Assert NotImplementedException is thrown
  - *Note: This method needs implementation*

##### GetBranches:
- [ ] **GetBranches_HasNotImplementedBehavior**
  - Call GetBranches method
  - Assert NotImplementedException is thrown
  - *Note: This method needs implementation*

### 9. DockerfileDeployService Tests
**File**: `tests/Haven.Infrastructure.Tests/Deployment/DockerfileDeployServiceTests.cs`

#### Mock Dependencies:
- `ILogger<DockerfileDeployService>`
- `HavenDbContext` (via TestDbContextFactory)
- `IDockerClient`
- `INetworkingService`
- `IEnvironmentVariableService`
- `IFeatureFlagService`
- `IGitService`
- `IGitCredentialsRepository`

#### Shared Setup:
- Create test project, environment, and service with DockerfileConfig
- Mock Docker client and networking service

#### Test Cases:

##### Deploy - Git Source:
- [ ] **Deploy_WithGitSource_FirstTime_ClonesAndBuilds**
  - Service with Git-sourced Dockerfile
  - Mock: repository doesn't exist, clone succeeds, docker build succeeds
  - Assert: Clone is called, tar archive created, image built, container created
  - Assert: ServiceType matches DockerfileDeployService.ServiceType

- [ ] **Deploy_WithGitSource_RepositoryExists_PullsAndBuilds**
  - Service with Git-sourced Dockerfile
  - Mock: repository exists, pull succeeds, docker build succeeds
  - Assert: Clone is not called, Pull is called with correct branch
  - Assert: Image is built and deployed

- [ ] **Deploy_WithGitSource_PullFails_ProceedsWithExistingCode**
  - Service with Git-sourced Dockerfile
  - Mock: repository exists, pull fails
  - Assert: LogWarning is called with appropriate message
  - Assert: Build proceeds with existing code

- [ ] **Deploy_WithGitSource_CustomFilePath_UsesProvidedPath**
  - Service with custom FilePath in DockerfileConfig
  - Mock: repository exists, build succeeds
  - Assert: ImageBuildParameters.Dockerfile is set to custom path

- [ ] **Deploy_WithGitSource_DefaultBranch_UsesMainWhenNotSpecified**
  - Service with Git source but no branch specified
  - Mock: pull succeeds with "main" branch
  - Assert: Pull is called with "main" branch

##### Deploy - Raw Source:
- [ ] **Deploy_WithRawSource_CreatesContentArchiveAndBuilds**
  - Service with Raw-sourced Dockerfile
  - Mock: docker build succeeds
  - Assert: CreateTarArchiveFromContentAsync is called with content
  - Assert: Image is built and deployed

- [ ] **Deploy_WithRawSource_DockerfilePathAlwaysUsesDefault**
  - Service with Raw source
  - Assert: ImageBuildParameters.Dockerfile is always "Dockerfile"

##### Deploy - General:
- [ ] **Deploy_BuildsImageWithCorrectTag**
  - Deploy with any source
  - Assert: ImageBuildParameters.Tags contains correct image tag (haven-service-{serviceId})

- [ ] **Deploy_CreatesContainerWithLabels**
  - Deploy with any source
  - Assert: Container is created with all expected labels
  - Labels should include service name, environment name, project name, service ID

- [ ] **Deploy_SetsEnvironmentVariablesCorrectly**
  - Service with environment variables configured
  - Mock: _environmentVariableService returns variables
  - Assert: Container parameters include all environment variables

- [ ] **Deploy_SetsListenAddressForInternalExposure**
  - Service with ExposureMode.Internal
  - Assert: LISTEN_ADDRESS environment variable is set to 127.0.0.1

- [ ] **Deploy_SetsListenAddressForExternalExposure**
  - Service with ExposureMode.External
  - Assert: LISTEN_ADDRESS environment variable is set to 0.0.0.0

- [ ] **Deploy_DoesNotSetListenAddressForNoneExposure**
  - Service with ExposureMode.None
  - Assert: LISTEN_ADDRESS is not added to environment variables

- [ ] **Deploy_RemovesExistingImage**
  - Service with existing image in Docker
  - Assert: Images.DeleteImageAsync is called with Force=true

- [ ] **Deploy_RemovesExistingContainer**
  - Service with existing container
  - Assert: StopAndRemoveContainersAsync is called before build

- [ ] **Deploy_DisconnectsFromNetworksBeforeDeploy**
  - Service with existing network connections
  - Assert: DisconnectServiceFromAllNetworksAsync is called at start

- [ ] **Deploy_ConnectsToEnvironmentNetwork**
  - Service with environment network
  - Assert: ConnectServiceToNetworksAsync is called after container creation

- [ ] **Deploy_UpdatesProjectDeploymentState**
  - Service deployment completes successfully
  - Assert: project.DeployService(environmentId, serviceId) is called
  - Assert: DbContext.SaveChangesAsync is called

- [ ] **Deploy_WithMissingEnvironment_ReturnsNotFound**
  - Service with null Environment reference
  - Assert: Returns Error.NotFoundFor(nameof(Environment), serviceId)

- [ ] **Deploy_WithMissingProject_ReturnsNotFound**
  - Service with null Project reference
  - Assert: Returns Error.NotFoundFor(nameof(Project), projectId)

- [ ] **Deploy_WithMissingDockerfileConfig_ReturnsValidationError**
  - Service with null DockerfileConfig
  - Assert: Returns Error.Validation

- [ ] **Deploy_WithGitSourceButMissingRepository_ReturnsValidationError**
  - Service with Git source but no repository URL
  - Assert: Returns Error.Validation

- [ ] **Deploy_WithRawSourceButMissingContent_ReturnsValidationError**
  - Service with Raw source but no content
  - Assert: Returns Error.Validation

- [ ] **Deploy_WhenBuildFails_ReturnsFailure**
  - Mock: ImageBuildFromDockerfileAsync throws exception
  - Assert: Exception is caught and logged
  - Assert: Returns appropriate error result

- [ ] **Deploy_WhenContainerCreationFails_ReturnsFailure**
  - Mock: CreateContainerAsync throws exception
  - Assert: Returns Error.Validation

- [ ] **Deploy_WhenContainerStartFails_ReturnsFailure**
  - Mock: Containers.StartContainerAsync returns false
  - Assert: Returns Error.Validation
  - Assert: LogError called

##### Stop:
- [ ] **Stop_WithExistingContainer_StopsAndRemovesIt**
  - Service with running container
  - Assert: Containers.StopContainerAsync is called
  - Assert: Containers.RemoveContainerAsync is called with Force=true

- [ ] **Stop_WithNoContainers_ReturnsNotFound**
  - Service with no containers
  - Assert: Returns Error.NotFoundFor("Docker Container", serviceId)

- [ ] **Stop_WithNoContainersButServiceRunning_UpdatesServiceStatus**
  - Service marked as Running but no container found
  - Assert: project.StopService is called
  - Assert: DbContext.SaveChangesAsync is called

- [ ] **Stop_WhenStopTimesOut_ContinuesWithRemoval**
  - Mock: StopContainerAsync throws OperationCanceledException
  - Assert: LogDebug called with timeout message
  - Assert: Removal proceeds

##### Restart:
- [ ] **Restart_WithGitSource_PullsLatestAndRebuild**
  - Service with Git-sourced Dockerfile
  - Assert: PullServiceRepositoryAsync is called with correct branch
  - Assert: Image is rebuilt and container restarted

- [ ] **Restart_WithGitSource_PullFailure_ProceedsWithExistingCode**
  - Pull fails
  - Assert: LogWarning called
  - Assert: Build proceeds

- [ ] **Restart_RemovesOldContainer**
  - Existing container is running
  - Assert: RemoveExistingContainerAsync is called

- [ ] **Restart_UpdatesProjectRestartState**
  - Restart completes successfully
  - Assert: project.RestartService(environmentId, serviceId) is called

- [ ] **Restart_WithMissingDockerfileConfig_ReturnsValidationError**
  - Service with null DockerfileConfig
  - Assert: Returns Error.Validation

### 10. DockerUtils Tests
**File**: `tests/Haven.Infrastructure.Tests/Utils/DockerUtilsTests.cs`

#### Test Cases:

##### BuildImageTag:
- [ ] **BuildImageTag_CreatesCorrectFormat**
  - ServiceId: 550e8400-e29b-41d4-a716-446655440000
  - Expected: "haven-service-550e8400e29b41d4a716446655440000"

##### BuildContainerName:
- [ ] **BuildContainerName_WithValidInput_CreatesNormalizedName**
  - Service name: "My Service", ID: 550e8400-e29b-41d4-a716-446655440000
  - Assert: Contains "haven-" prefix and service ID suffix
  - Assert: Length <= 63

- [ ] **BuildContainerName_WithInvalidCharacters_Normalizes**
  - Service name: "My@Service#123"
  - Assert: Invalid characters are replaced with hyphens

- [ ] **BuildContainerName_VeryLongName_Truncates**
  - Service name with 100 characters
  - Assert: Final container name length <= 63

- [ ] **BuildContainerName_OnlyNumbers_IsValid**
  - Service name: "12345"
  - Assert: Valid container name is created

##### BuildContainerLabels:
- [ ] **BuildContainerLabels_IncludesAllMetadata**
  - Service with environment and project
  - Assert: Labels include service name, environment name, project name, service ID

- [ ] **BuildContainerLabels_WithNullEnvironment_OmitsEnvironmentLabel**
  - Service with null Environment
  - Assert: Labels do not include environment.name

- [ ] **BuildContainerLabels_IncludesHavenManagedLabel**
  - Any service
  - Assert: Labels include haven.managed=true

##### CreateTarArchiveFromDirectoryAsync:
- [ ] **CreateTarArchive_FromDirectory_IncludesAllFiles**
  - Directory with multiple files and subdirectories
  - Assert: Tar stream contains all files
  - Assert: Relative paths are preserved

- [ ] **CreateTarArchive_FromDirectory_PreservesFileContent**
  - Directory with known file content
  - Extract and verify content matches

- [ ] **CreateTarArchive_FromDirectory_ForwardSlashPaths**
  - Directory with nested structure
  - Assert: All paths use forward slashes

- [ ] **CreateTarArchive_FromDirectory_NonExistentDirectory_Throws**
  - Non-existent path
  - Assert: Throws DirectoryNotFoundException

- [ ] **CreateTarArchive_FromDirectory_EmptyDirectory_Throws**
  - Empty directory
  - Assert: Throws InvalidOperationException

##### CreateTarArchiveFromContentAsync:
- [ ] **CreateTarArchive_FromContent_CreatesDockerfileEntry**
  - Content: "FROM ubuntu:22.04\nRUN echo hello"
  - Assert: Tar contains file named "Dockerfile"

- [ ] **CreateTarArchive_FromContent_PreservesContent**
  - Content: known multi-line Dockerfile
  - Assert: Content is preserved correctly in tar

- [ ] **CreateTarArchive_FromContent_UTF8Encoding**
  - Content: Contains UTF-8 characters
  - Assert: Content is properly encoded

##### Normalize:
- [ ] **Normalize_ValidInput_ReturnsLowercase**
  - Input: "MyService"
  - Expected: "myservice"

- [ ] **Normalize_InvalidCharacters_ReplacedWithHyphens**
  - Input: "My@Service#123"
  - Expected: "my-service-123"

- [ ] **Normalize_ConsecutiveHyphens_Collapsed**
  - Input: "My---Service"
  - Expected: "my-service"

- [ ] **Normalize_NullOrEmpty_ReturnsUnknown**
  - Input: null or ""
  - Expected: "unknown"

---

## Integration Tests

### 11. Dockerfile Service Creation Integration Tests
**File**: `tests/Haven.Integration.Tests/Features/Services/CreateDockerfileServiceTests.cs`

#### Setup:
- Create project and environment using mediator
- Real DbContext with in-memory SQLite

#### Test Cases:

##### Create with Git Source:
- [ ] **CreateDockerfileService_WithGitSource_Success**
  - Command: Valid CreateServiceCommand with Git-sourced Dockerfile
  - Assertions:
    - Service is created in database
    - Service.Type == ServiceType.Dockerfile
    - SourceConfig is DockerfileConfig with Git source
    - Repository, Branch are persisted
    - Service has correct environment and project relationships

- [ ] **CreateDockerfileService_WithGitSourceAndCustomFilePath_Success**
  - Include custom FilePath in config
  - Assert: FilePath is persisted correctly

- [ ] **CreateDockerfileService_WithGitSourceAndCredentials_Success**
  - Include GitCredentialId
  - Assert: GitCredentialId is persisted

- [ ] **CreateDockerfileService_WithGitSourceMissingRepository_Fails**
  - Repository is empty
  - Assert: Validation fails with appropriate message

- [ ] **CreateDockerfileService_WithGitSourceMissingBranch_Fails**
  - Branch is empty
  - Assert: Validation fails with appropriate message

##### Create with Raw Source:
- [ ] **CreateDockerfileService_WithRawSource_Success**
  - Command: Valid with Raw source and Dockerfile content
  - Assertions:
    - Service is created
    - SourceConfig is DockerfileConfig with Raw source
    - Content is persisted

- [ ] **CreateDockerfileService_WithRawSourceMissingContent_Fails**
  - Content is empty
  - Assert: Validation fails with appropriate message

- [ ] **CreateDockerfileService_WithRawSource_IgnoresRepositoryField**
  - Provide repository value (shouldn't be required)
  - Assert: Validation passes

##### Common:
- [ ] **CreateDockerfileService_WithExposureMode_Success**
  - Create with ExposureMode.Internal and ExposureMode.External
  - Assert: ExposureMode is persisted

- [ ] **CreateDockerfileService_WithReservedName_Fails**
  - Service name: "haven", "dns", "localhost", etc.
  - Assert: Validation fails

- [ ] **CreateDockerfileService_NameValidation_Follows_HavenServiceName_Pattern**
  - Various valid and invalid names
  - Assert: Only names matching `HavenServiceName.ValidPattern` pass

### 12. Dockerfile Service Update Integration Tests
**File**: `tests/Haven.Integration.Tests/Features/Services/UpdateDockerfileServiceTests.cs`

#### Setup:
- Create Dockerfile service in database
- Real DbContext

#### Test Cases:

##### Update Git Source Fields:
- [ ] **UpdateDockerfileService_ChangeRepository_Success**
  - Change repository URL
  - Assert: New repository is persisted

- [ ] **UpdateDockerfileService_ChangeBranch_Success**
  - Change branch
  - Assert: New branch is persisted

- [ ] **UpdateDockerfileService_UpdateFilePath_Success**
  - Update FilePath
  - Assert: New FilePath is persisted

- [ ] **UpdateDockerfileService_UpdateMultipleFields_Success**
  - Update repository and branch together
  - Assert: All fields updated

##### Switch Sources:
- [ ] **UpdateDockerfileService_SwitchFromGitToRaw_Success**
  - Change source from Git to Raw, provide content
  - Assert: Source is updated, content is stored
  - Assert: Old repository/branch fields are cleared or ignored

- [ ] **UpdateDockerfileService_SwitchFromRawToGit_Success**
  - Change source from Raw to Git, provide repository/branch
  - Assert: Source is updated, repository/branch stored
  - Assert: Old content is cleared or ignored

- [ ] **UpdateDockerfileService_SwitchToGitMissingRepository_Fails**
  - Change to Git source without providing repository
  - Assert: Validation fails

- [ ] **UpdateDockerfileService_SwitchToRawMissingContent_Fails**
  - Change to Raw source without providing content
  - Assert: Validation fails

##### Partial Updates (Optional pattern):
- [ ] **UpdateDockerfileService_UpdateNameOnly_PreservesDockerfileConfig**
  - Update only service name
  - Assert: Dockerfile config is unchanged

- [ ] **UpdateDockerfileService_UpdateExposureModeOnly_PreservesDockerfileConfig**
  - Update only exposure mode
  - Assert: Dockerfile config is unchanged

### 13. Dockerfile Service Deployment Integration Tests
**File**: `tests/Haven.Integration.Tests/Features/Services/DeployDockerfileServiceTests.cs`

#### Setup:
- Create Dockerfile service
- Mock Docker client (cannot run real Docker in test environment)
- Real DbContext, real GitService (can mock Git provider)

#### Test Cases:

##### Deploy Git Source:
- [ ] **DeployDockerfileService_GitSource_FirstDeploy_Clones**
  - Mock: Git clone succeeds, Docker build succeeds
  - Assert: Service is marked as deployed
  - Assert: Repository is cloned to service directory

- [ ] **DeployDockerfileService_GitSource_SecondDeploy_Pulls**
  - First deployment done, now second deployment
  - Mock: Git pull succeeds, Docker build succeeds
  - Assert: Pull is executed instead of clone

- [ ] **DeployDockerfileService_GitSource_InvalidRepository_Fails**
  - Mock: Git clone fails
  - Assert: Deployment fails
  - Assert: Service status is updated to failure

##### Deploy Raw Source:
- [ ] **DeployDockerfileService_RawSource_BuildsFromContent**
  - Mock: Docker build succeeds
  - Assert: Content is used to build image
  - Assert: Service is marked as deployed

- [ ] **DeployDockerfileService_RawSource_EachDeployUsesFreshBuild**
  - Deploy, then deploy again with same content
  - Assert: Both builds use the same content (no file I/O needed)

### 14. GetRemoteBranches Query Integration Tests
**File**: `tests/Haven.Integration.Tests/Features/Git/GetRemoteBranchesTests.cs`

#### Setup:
- Real DbContext, real GitService (mock Git provider)
- Create Git credentials in database

#### Test Cases:
- [ ] **GetRemoteBranches_WithoutCredentials_Success**
  - Query public repository
  - Mock: Git provider returns ["main", "develop", "feature-1"]
  - Assert: Query returns success with branches

- [ ] **GetRemoteBranches_WithCredentials_Success**
  - Query with stored credential ID
  - Mock: Git provider receives credentials
  - Assert: Query returns success with branches

- [ ] **GetRemoteBranches_WithNonExistentCredentialId_ReturnsNotFound**
  - Query with invalid credential ID
  - Assert: Query returns NotFound error

- [ ] **GetRemoteBranches_WithRepositoryNotFound_ReturnsFailure**
  - Query non-existent repository
  - Mock: Git provider throws
  - Assert: Query returns failure

---

## Frontend/API Integration Tests

### 15. CreateServiceModal API Integration Tests
**File**: `src/Presentation/Haven.Web/src/components/services/__tests__/CreateServiceModal.dockerfile.test.tsx`

#### Test Cases:

##### Service Type Selection:
- [ ] **Modal_DisplaysDockerfileServiceType**
  - Modal renders
  - Assert: Dockerfile option is visible with description

- [ ] **Modal_SelectingDockerfile_ShowsDockerfileFields**
  - Select Dockerfile service type
  - Assert: Git vs Raw source selector appears
  - Assert: DockerImage fields are hidden

##### Git Source:
- [ ] **Modal_DockerfileGitSource_RequiresRepository**
  - Git source selected, try to submit without repository
  - Assert: Validation error shown

- [ ] **Modal_DockerfileGitSource_RequiresBranch**
  - Repository provided, try to submit without branch
  - Assert: Validation error shown

- [ ] **Modal_DockerfileGitSource_BranchAutocomplete_Shows**
  - Repository entered
  - Assert: Branch autocomplete dropdown appears with suggestions

- [ ] **Modal_DockerfileGitSource_SubmitsWithAllRequired**
  - All fields provided, submit
  - Assert: API call made with correct payload
  - Assert: Modal closes on success

- [ ] **Modal_DockerfileGitSource_OptionalFilePath**
  - Provide custom FilePath
  - Assert: Can submit successfully
  - Assert: FilePath is in API payload

- [ ] **Modal_DockerfileGitSource_WithCredentials**
  - Select Git credentials from dropdown
  - Assert: Credentials are included in API request

##### Raw Source:
- [ ] **Modal_DockerfileRawSource_RequiresContent**
  - Raw source selected, try to submit without content
  - Assert: Validation error shown

- [ ] **Modal_DockerfileRawSource_EditorAppears**
  - Raw source selected
  - Assert: Dockerfile content editor/textarea appears

- [ ] **Modal_DockerfileRawSource_SubmitsWithContent**
  - Content provided, submit
  - Assert: API call made with content
  - Assert: Modal closes on success

##### Error Handling:
- [ ] **Modal_APIError_DisplaysMessage**
  - API request fails
  - Assert: Error message displayed in modal

- [ ] **Modal_NetworkError_ShowsRetry**
  - Network error occurs
  - Assert: Retry option available

---

## Summary of Coverage

| Layer | Component | Unit Tests | Integration Tests | Status |
|-------|-----------|------------|-------------------|--------|
| Domain | DockerfileConfig | ✓ | - | Ready |
| Domain | DockerfileSource | ✓ | - | Ready |
| Application | CreateServiceValidator | ✓ | ✓ | Ready |
| Application | UpdateServiceValidator | ✓ | ✓ | Ready |
| Application | GetRemoteBranchesQuery | ✓ | ✓ | **Incomplete Handler** |
| Infrastructure | GitService | ✓ | ✓ | Ready |
| Infrastructure | GitProviderFactory | ✓ | - | Ready |
| Infrastructure | GenericGitProvider | ✓ | - | **Incomplete Implementation** |
| Infrastructure | DockerfileDeployService | ✓ | ✓ | Ready |
| Infrastructure | DockerUtils | ✓ | - | Ready |
| Frontend | CreateServiceModal | - | ✓ | Ready |

---

## Known Incomplete Items

1. **GetRemoteBranchesHandler**: Currently returns hardcoded branches `["Foo", "bar", "fizz"]`
   - Needs to call `gitService.GetRemoteBranchesAsync()`
   - Needs actual Git provider implementation

2. **GenericGitProvider.PullAsync()**: Not implemented
   - Uses LibGit2Sharp to pull from remote branch
   - Handle authentication with credentials

3. **GenericGitProvider.GetBranchesAsync()**: Not implemented
   - Fetch remote branches without cloning
   - Use LibGit2Sharp or libgit2 API

---

## Test Execution Strategy

1. **Run domain tests first** - No dependencies, fast feedback
2. **Run application tests** - With mocked infrastructure
3. **Run infrastructure tests** - With mocked external services (Docker, Git)
4. **Run integration tests** - With real DbContext, mocked external services
5. **Run frontend tests** - With mocked API responses
6. **Manual testing** - E2E with actual Docker and Git (not automated)

All tests should follow the project's NUnit + NSubstitute + Shouldly conventions.
