﻿// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class ProducerSystemTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public ProducerSystemTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task CreateRawProducer()
        {
            var testPassed = new TaskCompletionSource<bool>();
            var stream = Guid.NewGuid().ToString();
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);
            await system.CreateStream(new StreamSpec(stream));

            var createProducerTask = system.CreateRawProducer(
                new RawProducerConfig(stream)
                {
                    Reference = "producer",
                    ConfirmHandler = conf =>
                    {
                        testOutputHelper.WriteLine($"CreateProducer Confirm Handler #{conf.Code}");
                        testPassed.SetResult(conf.Code == ResponseCode.Ok);
                    }
                });

            if (await Task.WhenAny(createProducerTask, Task.Delay(5000)) != createProducerTask)
            {
                // timeout to avoid infinite await
                Assert.Fail("timeout awaiting for CreateRawProducer");
                return;
            }

            var rawProducer = await createProducerTask;

            var readonlySequence = "apple".AsReadonlySequence();
            var message = new Message(new Data(readonlySequence));
            await rawProducer.Send(1, message);

            new Utils<bool>(testOutputHelper).WaitUntilTaskCompletes(testPassed);

            Assert.True(await testPassed.Task);
            rawProducer.Dispose();
            Assert.False(rawProducer.IsOpen());
            await system.DeleteStream(stream);
            await system.Close();
        }

        [Fact]
        public async Task CreateProducerStreamDoesNotExist()
        {
            const string stream = "StreamNotExist";
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);

            await Assert.ThrowsAsync<CreateProducerException>(() => system.CreateRawProducer(
                new RawProducerConfig(stream) { Reference = "producer" }));

            await system.Close();
        }

        [Fact]
        public async Task CloseProducerTwoTimesShouldReturnOk()
        {
            var stream = Guid.NewGuid().ToString();
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);
            await system.CreateStream(new StreamSpec(stream));
            var rawProducer = await system.CreateRawProducer(
                new RawProducerConfig(stream) { Reference = "producer" });

            Assert.Equal(ResponseCode.Ok, await rawProducer.Close());
            Assert.Equal(ResponseCode.Ok, await rawProducer.Close());
            rawProducer.Dispose();
            await system.DeleteStream(stream);
            await system.Close();
        }

        [Fact]
        public async Task ProducerShouldRaiseAnExceptionIfStreamOrBatchSizeAreNotValid()
        {
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);

            await Assert.ThrowsAsync<ArgumentException>(() => system.CreateRawProducer(
                new RawProducerConfig("") { Reference = "producer" }));

            await Assert.ThrowsAsync<CreateProducerException>(() => system.CreateRawProducer(
                new RawProducerConfig("TEST") { Reference = "producer", MessagesBufferSize = -1, }));

            await system.Close();
        }

        [Fact]
        public async Task NotifyProducerClose()
        {
            var stream = Guid.NewGuid().ToString();
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);
            await system.CreateStream(new StreamSpec(stream));
            var testPassed = new TaskCompletionSource<bool>();
            var rawProducer = await system.CreateRawProducer(
                new RawProducerConfig(stream)
                {
                    Reference = "producer",
                    ConnectionClosedHandler = async s =>
                    {
                        testOutputHelper.WriteLine("NotifyProducerClose true");
                        testPassed.SetResult(true);
                        await Task.CompletedTask;
                    }
                });

            Assert.Equal(ResponseCode.Ok, await rawProducer.Close());
            new Utils<bool>(testOutputHelper).WaitUntilTaskCompletes(testPassed);
            await system.DeleteStream(stream);
            await system.Close();
        }

        [Fact]
        public async Task ProducerMessagesListLenValidation()
        {
            // by protocol the subEntryList is ushort
            var messages = new List<Message>();
            for (var i = 0; i < (ushort.MaxValue + 1); i++)
            {
                messages.Add(new Message(Encoding.UTF8.GetBytes($"data_{i}")));
            }

            var stream = Guid.NewGuid().ToString();
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);
            await system.CreateStream(new StreamSpec(stream));
            var rawProducer = await system.CreateRawProducer(
                new RawProducerConfig(stream) { Reference = "producer" });

            await Assert.ThrowsAsync<OutOfBoundsException>(() =>
                rawProducer.Send(1, messages, CompressionType.Gzip).AsTask());

            Assert.Equal(ResponseCode.Ok, await rawProducer.Close());
            await system.DeleteStream(stream);
            await system.Close();
        }

        [Fact]
        public async Task ProducerMixingDifferentConfirmations()
        {
            // we send 150 messages using three different ways:
            // 1- 50 messages subEntry with compress None 
            // 2- 50 messages with standard send 
            // 3- subEntry with compress Gzip
            // we should receive only 52 conformations
            // one for the point 1
            // 50 for the point 2
            // one for the point 3
            var messages = new List<Message>();
            for (var i = 0; i < 50; i++)
            {
                messages.Add(new Message(Encoding.UTF8.GetBytes($"sub{i}")));
            }

            var testPassed = new TaskCompletionSource<bool>();

            var stream = Guid.NewGuid().ToString();
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);
            await system.CreateStream(new StreamSpec(stream));
            ulong count = 0;
            var rawProducer = await system.CreateRawProducer(
                new RawProducerConfig(stream)
                {
                    Reference = Guid.NewGuid().ToString(),
                    ConfirmHandler = conf =>
                    {
                        if (conf.Code != ResponseCode.Ok)
                        {
                            testOutputHelper.WriteLine($"error confirmation {conf.Code}");
                            testPassed.SetResult(false);
                        }

                        if (Interlocked.Increment(ref count) == 52)
                        {
                            testPassed.SetResult(conf.Code == ResponseCode.Ok);
                        }
                    }
                });

            ulong pid = 0;
            await rawProducer.Send(++pid, messages, CompressionType.None);

            foreach (var message in messages)
            {
                await rawProducer.Send(++pid, message);
            }

            await rawProducer.Send(++pid, messages, CompressionType.Gzip);
            await SystemUtils.WaitAsync();
            new Utils<bool>(testOutputHelper).WaitUntilTaskCompletes(testPassed);
            Assert.Equal((ulong)52, count);
            Assert.Equal(ResponseCode.Ok, await rawProducer.Close());
            await system.DeleteStream(stream);
            await system.Close();
        }

        [Fact]
        public async Task ProducerMetadataHandlerUpdate()
        {
            // test the producer metadata update
            // metadata update can happen when a stream is deleted
            // or when the stream topology is changed.
            // here we test the deleted part
            // with the event: ProducerConfig:MetadataHandler/1
            var stream = Guid.NewGuid().ToString();
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);
            await system.CreateStream(new StreamSpec(stream));
            var testPassed = new TaskCompletionSource<bool>();
            var rawProducer = await system.CreateRawProducer(
                new RawProducerConfig(stream)
                {
                    Reference = "producer",
                    MetadataHandler = update =>
                    {
                        if (update.Stream == stream)
                        {
                            testPassed.SetResult(true);
                        }

                        return Task.CompletedTask;
                    }
                });
            await SystemUtils.WaitAsync();
            await system.DeleteStream(stream);
            new Utils<bool>(testOutputHelper).WaitUntilTaskCompletes(testPassed);
            Assert.False(((RawProducer)rawProducer).IsOpen());
            await rawProducer.Close();
            await system.Close();
        }

        [Fact]
        public async Task ProducerQuerySequence()
        {
            // test the producer sequence
            // with an empty stream the QuerySequence/2 return must be = 0
            var stream = Guid.NewGuid().ToString();
            const string ProducerName = "myProducer";
            const int NumberOfMessages = 10;
            var config = new StreamSystemConfig();
            var system = await StreamSystem.Create(config);
            await system.CreateStream(new StreamSpec(stream));
            var res = await system.QuerySequence(ProducerName, stream);
            Assert.True(res == 0);
            await SystemUtils.PublishMessages(system, stream, NumberOfMessages,
                ProducerName, testOutputHelper);
            await SystemUtils.WaitAsync();
            var resAfter = await system.QuerySequence(ProducerName, stream);
            // sequence start from zero
            Assert.True(resAfter == (NumberOfMessages - 1));

            var rawProducer =
                await system.CreateRawProducer(new RawProducerConfig(stream) { Reference = ProducerName });
            Assert.True(await rawProducer.GetLastPublishingId() == (NumberOfMessages - 1));
            await system.DeleteStream(stream);
            await system.Close();
        }

        [Fact]
        public async Task ProducerSendValidate()
        {
            SystemUtils.InitStreamSystemWithRandomStream(out var system, out var stream);
            // validate that the messages batch size is not greater than the MaxInFlight

            var rawProducer = await system.CreateRawProducer(new RawProducerConfig(stream)
            {
                Reference = "producer",
                MaxInFlight = 100, // in this case the batch send can't be greater than 100
            });
            var messages = new List<(ulong, Message)>();
            // 101 > MaxInFlight so it must raise an exception
            for (var i = 0; i < 101; i++)
            {
                messages.Add(((ulong)i, new Message(Encoding.UTF8.GetBytes($"data_{i}"))));
            }

            await Assert.ThrowsAsync<InvalidOperationException>(() => rawProducer.Send(messages).AsTask());
            messages.Clear();

            // MaxFrameSize by default is 1048576
            // we can't send messages greater than 1048576 bytes
            messages.Add((1, new Message(new byte[1048576 * 2]))); // 2MB >  MaxFrameSize so it must raise an exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => rawProducer.Send(messages).AsTask());

            var messagetoBig = new Message(new byte[1048576 * 2]); // 2MB >  MaxFrameSize so it must raise an exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => rawProducer.Send(2, messagetoBig).AsTask());

            await system.DeleteStream(stream);
            await system.Close();
        }

        [Fact]
        public async Task ProducerBatchConfirmNumberOfMessages()
        {
            // test the batch confirm number of messages for batch send
            // we send 100 messages using the batch send
            // we should receive only 100 conformations
            SystemUtils.InitStreamSystemWithRandomStream(out var system, out var stream);
            var testPassed = new TaskCompletionSource<bool>();
            const int NumberOfMessages = 100;
            var rawProducer = await system.CreateRawProducer(new
                RawProducerConfig(stream)
            {
                Reference = "producer",
                ConfirmHandler = confirmation =>
                {
                    if (confirmation.PublishingId == NumberOfMessages)
                    {
                        testPassed.SetResult(true);
                    }
                }
            }
            );
            Assert.Equal(
                $"ProducerInfo(Stream={stream}, Reference=producer, Identifier=, Partitions=)",
                rawProducer.Info.ToString());
            var messages = new List<(ulong, Message)>();
            for (var i = 1; i <= NumberOfMessages; i++)
            {
                messages.Add(((ulong)i, new Message(Encoding.UTF8.GetBytes($"data_{i}"))));
            }

            await rawProducer.Send(messages);
            messages.Clear();
            new Utils<bool>(testOutputHelper).WaitUntilTaskCompletes(testPassed);
            await system.DeleteStream(stream);
            await system.Close();
        }

        private class EventLengthTestCases : IEnumerable<object[]>
        {
            private readonly Random _random = new(3895);

            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { GetRandomBytes(254) };
                yield return new object[] { GetRandomBytes(255) };
                yield return new object[] { GetRandomBytes(256) };
                // just to test an event greater than 256 bytes
                yield return new object[] { GetRandomBytes(654) };
            }

            private ReadOnlySequence<byte> GetRandomBytes(ulong length)
            {
                var arr = new byte[length];
                _random.NextBytes(arr);
                return new ReadOnlySequence<byte>(arr);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Theory]
        [ClassData(typeof(EventLengthTestCases))]
        public async Task ProducerSendsArrays255Bytes(ReadOnlySequence<byte> @event)
        {
            // Test the data around 255 bytes
            // https://github.com/rabbitmq/rabbitmq-stream-dotnet-client/issues/160
            // We test if the data is correctly sent and received
            SystemUtils.InitStreamSystemWithRandomStream(out var system, out var stream);
            var testPassed = new TaskCompletionSource<bool>();
            var rawProducer = await system.CreateRawProducer(new
                RawProducerConfig(stream)
            {
                Reference = "producer",
                ConfirmHandler = _ => { testPassed.SetResult(true); }
            }
            );

            const ulong PublishingId = 0;
            var msg = new Message(new Data(@event))
            {
                ApplicationProperties = new ApplicationProperties { { "myArray", @event.First.ToArray() } }
            };

            await rawProducer.Send(PublishingId, msg);
            new Utils<bool>(testOutputHelper).WaitUntilTaskCompletes(testPassed);

            var testMessageConsumer = new TaskCompletionSource<Message>();

            var consumer = await system.CreateRawConsumer(new RawConsumerConfig(stream)
            {
                // Consume the stream from the Offset
                OffsetSpec = new OffsetTypeOffset(),
                // Receive the messages
                MessageHandler = (_, _, message) =>
                {
                    testMessageConsumer.SetResult(message);
                    return Task.CompletedTask;
                }
            });

            new Utils<Message>(testOutputHelper).WaitUntilTaskCompletes(testMessageConsumer);
            // at this point the data length _must_ be the same
            var result = await testMessageConsumer.Task;
            Assert.Equal(@event.Length, result.Data.Contents.Length);

            Assert.Equal(@event.Length,
                ((byte[])result.ApplicationProperties["myArray"]).Length);
            await consumer.Close();
            await system.DeleteStream(stream);
            await system.Close();
        }
    }
}
