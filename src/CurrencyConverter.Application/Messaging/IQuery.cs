using CurrencyConverter.Application.ResultResponse;
using MediatR;

namespace CurrencyConverter.Application.Messaging;

/// <summary>
/// Marker for queries that always return a typed result wrapped in <see cref="Result{T}"/>.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
