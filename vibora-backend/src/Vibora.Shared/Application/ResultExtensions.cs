using Ardalis.Result;

namespace Vibora.Shared.Application;

/// <summary>
/// Extension methods to facilitate Railway-Oriented Programming with Result pattern
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a nullable entity to a Result, returning NotFound if null
    /// </summary>
    public static Result<T> ToResultOrNotFound<T>(this T? entity, string errorMessage) where T : class
    {
        return entity == null 
            ? Result<T>.NotFound(errorMessage) 
            : Result.Success(entity);
    }

    /// <summary>
    /// Converts a nullable entity to a Result, returning NotFound with a formatted message if null
    /// </summary>
    public static Result<T> ToResultOrNotFound<T>(this T? entity, string entityName, object id) where T : class
    {
        return entity == null 
            ? Result<T>.NotFound($"{entityName} with ID '{id}' not found") 
            : Result.Success(entity);
    }

    /// <summary>
    /// Converts a boolean to a Result, returning Error if false
    /// </summary>
    public static Result ToResultOrError(this bool condition, string errorMessage)
    {
        return condition 
            ? Result.Success() 
            : Result.Error(errorMessage);
    }

    /// <summary>
    /// Converts a nullable value to a Result, returning Invalid if null
    /// </summary>
    public static Result<T> ToResultOrInvalid<T>(this T? entity, string errorMessage) where T : class
    {
        return entity == null 
            ? Result<T>.Invalid(new ValidationError(errorMessage)) 
            : Result.Success(entity);
    }

    /// <summary>
    /// Helper method to convert a failed Result to Result&lt;T&gt; while preserving the status
    /// </summary>
    private static Result<T> PropagateFailure<T>(Result actionResult)
    {
        return actionResult.Status switch
        {
            ResultStatus.NotFound => Result<T>.NotFound(string.Join(", ", actionResult.Errors)),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(),
            ResultStatus.Forbidden => Result<T>.Forbidden(),
            ResultStatus.Invalid => Result<T>.Invalid(actionResult.ValidationErrors),
            ResultStatus.Conflict => Result<T>.Conflict(string.Join(", ", actionResult.Errors)),
            _ => Result<T>.Error(string.Join(", ", actionResult.Errors))
        };
    }

    /// <summary>
    /// Helper method to convert a failed Result&lt;TSource&gt; to Result&lt;T&gt; while preserving the status
    /// </summary>
    private static Result<T> PropagateFailure<T, TSource>(Result<TSource> actionResult)
    {
        return actionResult.Status switch
        {
            ResultStatus.NotFound => Result<T>.NotFound(string.Join(", ", actionResult.Errors)),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(),
            ResultStatus.Forbidden => Result<T>.Forbidden(),
            ResultStatus.Invalid => Result<T>.Invalid(actionResult.ValidationErrors),
            ResultStatus.Conflict => Result<T>.Conflict(string.Join(", ", actionResult.Errors)),
            _ => Result<T>.Error(string.Join(", ", actionResult.Errors))
        };
    }

    /// <summary>
    /// Executes an async action that returns a Result without changing the value flowing through the pipeline.
    /// If the action fails, propagates the error. Otherwise, continues with the original value.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result>> action)
    {
        var result = await resultTask;
        if (!result.IsSuccess)
            return result;
            
        var actionResult = await action(result.Value);
        return actionResult.IsSuccess 
            ? result 
            : PropagateFailure<T>(actionResult);
    }

    /// <summary>
    /// Executes an async action that returns a Result&lt;TIgnored&gt; without changing the value flowing through the pipeline.
    /// If the action fails, propagates the error. Otherwise, continues with the original value.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T, TIgnored>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TIgnored>>> action)
    {
        var result = await resultTask;
        if (!result.IsSuccess)
            return result;
            
        var actionResult = await action(result.Value);
        return actionResult.IsSuccess 
            ? result 
            : PropagateFailure<T, TIgnored>(actionResult);
    }

    /// <summary>
    /// Executes an async action that returns a Result without changing the value flowing through the pipeline (synchronous Result version).
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, Task<Result>> action)
    {
        if (!result.IsSuccess)
            return result;
            
        var actionResult = await action(result.Value);
        return actionResult.IsSuccess 
            ? result 
            : PropagateFailure<T>(actionResult);
    }

    /// <summary>
    /// Executes an async action that returns a Result&lt;TIgnored&gt; without changing the value flowing through the pipeline (synchronous Result version).
    /// </summary>
    public static async Task<Result<T>> TapAsync<T, TIgnored>(
        this Result<T> result,
        Func<T, Task<Result<TIgnored>>> action)
    {
        if (!result.IsSuccess)
            return result;
            
        var actionResult = await action(result.Value);
        return actionResult.IsSuccess 
            ? result 
            : PropagateFailure<T, TIgnored>(actionResult);
    }
}
