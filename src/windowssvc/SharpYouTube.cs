namespace ArcusWinSvc;

/// <summary>
/// this is a place holder while I figure out what works and what doesn't
/// </summary>
public class SharpYouTube
{
    /*
    public void Run()
    {
    
        var grabber = GrabberBuilder.New()
            .UseDefaultServices()
            .AddYouTube()
            .Build();

        try
        {
            GrabResult result;
            try
            {
                result = await grabber.GrabAsync(new Uri(request.Url));
            }
            catch
            {
                // see https://github.com/dotnettools/SharpGrabber/issues/97
                logger.LogDebug($"Running second attempt for URL: {request.Url}");
                result = await grabber.GrabAsync(new Uri(request.Url));
            }

            var info = result.Resource<GrabbedInfo>();
            logger.LogInformation($"URL got {info}");
            var mediaFiles = result.Resources<GrabbedMedia>().ToArray();
            var bestAudio = mediaFiles.GetHighestQualityAudio();
            logger.LogInformation($"Best audio = {bestAudio}");
            var file = await DownloadMedia(bestAudio, result, request.Url);
            logger.LogInformation($"File = {file}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Url Handler failed: {ex.Message}");
        }
    }
    

    /// <summary>
    /// this is temporary
    /// </summary>
    /// <param name="media"></param>
    /// <param name="grabResult"></param>
    /// <returns></returns>
    private async Task<string> DownloadMedia(GrabbedMedia media, IGrabResult grabResult, string url)
    {
        logger.LogInformation($"Downloading {media.ResourceUri}");
        // using var response = await GrabberServices.Default.GetClient().GetAsync(media.ResourceUri);
        using var response = await MakeClient(MakeHandler(url)).GetAsync(media.ResourceUri);
        response.EnsureSuccessStatusCode();
        using var downloadStream = await response.Content.ReadAsStreamAsync();
        using var resourceStream = await grabResult.WrapStreamAsync(downloadStream);
        var path = "f:\\downloads\\song.mp3";

        using var fileStream = new FileStream(path, FileMode.Create);
        await resourceStream.CopyToAsync(fileStream);
        return path;
    }
    protected HttpMessageHandler MakeHandler(string url)
    {
        // Cookie
        var cookieContainer = new CookieContainer();
        cookieContainer.Add(new Uri(url), new Cookie("CONSENT", "YES+cb", "/", ".youtube.com"));
        // Handler
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer
        };
        if (handler.SupportsAutomaticDecompression)
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        return handler;
    }

    protected HttpClient MakeClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36 Edg/89.0.774.76"
        );
        return new HttpClient(handler);
    }    
    */
}