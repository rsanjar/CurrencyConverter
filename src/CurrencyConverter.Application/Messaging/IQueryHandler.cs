using CurrencyConverter.Application.ResultResponse;
using MediatR;

namespace CurrencyConverter.Application.Messaging;

/// <summary>
/// Handler for queries that always return a typed result wrapped in <see cref="Result{T}"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
