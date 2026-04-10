using Mediator;

namespace Haven.Application.Common.Messaging;

// Common/Messaging/ICommand.cs
public interface ICommand : IRequest<Result>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;