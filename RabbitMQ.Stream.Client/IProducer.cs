﻿// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQ.Stream.Client;

public interface ISuperStreamProducer : IProducer
{
    public Task ReconnectPartition(StreamInfo streamInfo);
}
// <summary>
// Producer interface for sending messages to a stream.
// There are different types of producers:
// - Standard producer
// - Super-Stream producer
// </summary>

public interface IProducer : IClosable
{
    /// <summary>
    /// Send the message to the stream in asynchronous mode.
    /// The client will aggregate messages and send them in batches.
    /// The batch size is configurable. See IProducerConfig.BatchSize.
    /// </summary>
    /// <param name="publishingId">Publishing id</param>
    /// <param name="message"> Message </param>
    /// <returns></returns>
    public ValueTask Send(ulong publishingId, Message message);

    /// <summary>
    /// Send the messages in batch to the stream in synchronous mode.
    /// The aggregation is provided by the user.
    /// The client will send the messages in the order they are provided.
    /// </summary>
    /// <param name="messages">Batch messages to send</param>
    /// <returns></returns>
    public ValueTask Send(List<(ulong, Message)> messages);

    /// <summary>
    /// Enable sub-batch feature.
    /// It is needed when you need to sub aggregate the messages and compress them.
    /// For example you can aggregate 100 log messages and compress to reduce the space.
    /// One single publishingId can have multiple sub-batches messages.
    /// See also: https://rabbitmq.github.io/rabbitmq-stream-java-client/stable/htmlsingle/#sub-entry-batching-and-compression
    /// </summary>
    /// <param name="publishingId"></param>
    /// <param name="subEntryMessages">Messages to aggregate</param>
    /// <param name="compressionType"> Type of compression. By default the client supports GZIP and none</param>
    /// <returns></returns>
    public ValueTask Send(ulong publishingId, List<Message> subEntryMessages, CompressionType compressionType);

    /// <summary>
    /// Return the last publishing id.
    /// </summary>
    /// <returns></returns>
    public Task<ulong> GetLastPublishingId();

    public bool IsOpen();

    public void Dispose();

    public int MessagesSent { get; }
    public int ConfirmFrames { get; }
    public int IncomingFrames { get; }
    public int PublishCommandsSent { get; }

    public int PendingCount { get; }

    /// <summary>
    /// Info contains the reference and the stream name.
    /// </summary>
    public ProducerInfo Info { get; }
}

public record ProducerFilter
{
    /// <summary>
    /// FilterValue is a function that returns the filter value.
    /// It is executed for each message.
    /// </summary>
    public Func<Message, string> FilterValue { get; set; } = null;
}

public record IProducerConfig : EntityCommonConfig, INamedEntity
{
    public string Reference { get; set; }
    public int MaxInFlight { get; set; } = 1_000;
    public string ClientProvidedName { get; set; } = "dotnet-stream-raw-producer";

    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Number of the messages sent for each frame-send.
    /// High values can increase the throughput.
    /// Low values can reduce the messages latency.
    /// Default value is 100.
    /// </summary>
    public int MessagesBufferSize { get; set; } = 100;

    /// <summary>
    /// Filter enables the chunk filter feature.
    /// </summary>
    public ProducerFilter Filter { get; set; } = null;
}

/// <summary>
/// ProducerInfo contains the reference and the stream name.
/// </summary>
public class ProducerInfo : Info
{
    public string Reference { get; }

    public ProducerInfo(string stream, string reference, string identifier, List<string> partitions) : base(stream, identifier, partitions)
    {
        Reference = reference;
    }

    public override string ToString()
    {
        var partitions = Partitions ?? [];

        return $"ProducerInfo(Stream={Stream}, Reference={Reference}, Identifier={Identifier}, Partitions={string.Join(",", partitions)})";
    }
}
