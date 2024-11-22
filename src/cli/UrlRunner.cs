using Arcus.GRPC;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

/// <summary>
/// For grabbing contents from a URL, converting and adding the results
/// to the archive for access through the get command
///
/// FOR NOW: ONLY WORKS ON YOUTUBE :/ 
/// </summary>
/// <param name="logger"></param>
/// <param name="args"></param>
public class UrlRunner(ILogger<UrlRunner> logger, string[] args) 
    : AbstractBaseRunner<UrlRunner>(logger, args)
{
    public override CliCommand Command { get; } = CliCommand.Url;
    public override void Run()
    {
        // arguments:
        // --url={} --convert={None, MP3}
        
        logger.LogDebug("Running url");
        
        if (null == args || args.Length == 0)
            throw new CliArgumentException();
        
        logger.LogDebug($"URL arguments: {string.Join(" ", args)}");
        
        var parser = new ArgumentParser(args);
        
        string url = parser.GetArgument<string>("url");
        ConvertTypes convertType = parser.GetArgument<ConvertTypes>("convert");

        var urlRequest = new UrlRequest()
        {
            Url = url,
            Conversion = ConversionTypes.ConversionTypeUnspecified
        };

        var response = client.Url(urlRequest);
        logger.LogInformation($"URL response: {response.Id} - {response.Status}");
    }
}