﻿using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using Arcus.GRPC;

namespace ArcusCli;

/// <summary>
/// Since most runners will be accessing the service, the base class
/// can contain some of the share behaviors
/// </summary>
public abstract class AbstractBaseRunner<T> : IArgumentRunner where T: IArgumentRunner
{
    protected ILogger<T> logger;
    protected readonly GrpcChannel channel;
    protected readonly ActionsService.ActionsServiceClient client;    
    public abstract CliCommand Command { get; }

    public AbstractBaseRunner(ILogger<T> logger)
    {
        this.logger = logger;
        channel = GrpcChannel.ForAddress("http://localhost:5001");
        client = new ActionsService.ActionsServiceClient(channel);
    }

    public abstract void Run();

}