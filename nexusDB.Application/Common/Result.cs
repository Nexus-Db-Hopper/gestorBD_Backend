using System;

namespace nexusDB.Application.Common;

/// <summary>
/// Clase base para encapsular el resultado de una operación, siguiendo las mejores prácticas.
/// Proporciona un contrato claro: una operación o tiene éxito, o falla con un error.
/// Esto evita el uso de excepciones para control de flujo y hace el código más predecible.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Un resultado exitoso no puede contener un error.");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Un resultado fallido debe contener un error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

/// <summary>
/// Versión genérica de la clase Result para operaciones que devuelven un valor.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        if (isSuccess && value == null)
            throw new InvalidOperationException("Un resultado exitoso debe contener un valor.");

        Value = value;
    }

    public static new Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error) => new(false, default, error);
}
