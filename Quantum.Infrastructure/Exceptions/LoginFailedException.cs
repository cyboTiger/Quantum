namespace Quantum.Infrastructure.Exceptions;

public class LoginFailedException(string? message = null) : Exception(message);
