namespace ArcusCli;

/// <summary>
/// purpose of this type is to wrap behaviors around reading
/// command line arguments passed in like
/// --id={id} --path={path}
/// </summary>
public class ArgumentParser
{
    private readonly string[] args;
    private Dictionary<string, string> arguments;
    
    public ArgumentParser(string[] args)
    {
        this.args = args;
        arguments = ParseArguments(args);
    }

    /// <summary>
    /// returns value of key in the argument array if it exists, otherwise
    /// returns the default
    /// </summary>
    /// <param name="name">key name</param>
    /// <param name="defaultValue">default</param>
    /// <typeparam name="T">type, value types only</typeparam>
    /// <returns>default or value found</returns>
    public T GetArgument<T>(string name, T defaultValue = default(T))
    {
        T ret = defaultValue;

        if (arguments.ContainsKey(name))
        {
            if (typeof(T).IsEnum)
            {
                ret = (T)Enum.Parse(typeof(T), arguments[name], true);
            }
            else
            {
                ret = (T)Convert.ChangeType(arguments[name], typeof(T));
            }
        }

        return ret;
    }
    
    private Dictionary<string, string> ParseArguments(string[] args)
    {
        var parsedArguments = new Dictionary<string, string>();

        foreach (var arg in args)
        {
            // format must be --key=value.  not supporting key only parameters
            if (arg.StartsWith("--") && arg.Contains("="))
            {
                var splitArg = arg.Split('=', 2); // Split on the first '=' character
                var key = splitArg[0].Replace("--", "");  // "--name"
                var value = splitArg[1]; // "value"

                parsedArguments[key] = value; // Add or update dictionary
            }
        }

        return parsedArguments;
    }
}