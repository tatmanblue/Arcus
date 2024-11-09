namespace ArcusCli;

/// <summary>
/// Thrown by Runners when the arguments provided to the runner do not
/// include the required parameters
/// </summary>
public class CliArgumentException() : Exception;

/// <summary>
/// Thrown by runners when the arguments are provided but the values are not valid
/// </summary>
/// <param name="message">indicates which parameter value was invalid</param>
public class CliInvalidInputException(string message) : Exception(message);