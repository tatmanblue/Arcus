namespace ArcusCli;

/// <summary>
/// Thrown by Runners when the arguments provided to the runner are invalid
/// </summary>
public class CliArgumentException() : Exception;

/// <summary>
/// Thrown by runners when the arguements are provided but the values are not valid
/// </summary>
/// <param name="message"></param>
public class CliInvalidInputException(string message) : Exception(message);