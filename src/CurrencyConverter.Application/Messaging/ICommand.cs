using CurrencyConverter.Application.ResultResponse;
using MediatR;

namespace CurrencyConverter.Application.Messaging;

/// <summary>
/// Marker for commands that return no value beyond success/failure.
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// Marker for commands that return a typed result on success.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
