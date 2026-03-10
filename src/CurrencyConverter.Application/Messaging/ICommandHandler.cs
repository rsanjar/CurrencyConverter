using CurrencyConverter.Application.ResultResponse;
using MediatR;

namespace CurrencyConverter.Application.Messaging;

/// <summary>
/// Handler for commands that return no value beyond success/failure.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Handler for commands that return a typed result on success.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
