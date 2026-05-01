using Mediator;

namespace Haven.Application.Common.Messaging;

public interface ICommand : Mediator.ICommand<Result>;
public interface ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>;