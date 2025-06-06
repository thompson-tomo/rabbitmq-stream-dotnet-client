﻿// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
// Copyright (c) 2017-2023 Broadcom. All Rights Reserved. The term "Broadcom" refers to Broadcom Inc. and/or its subsidiaries.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using Xunit;

namespace Tests
{
    public class Amqp10Tests
    {
        [Fact]
        public void ReadsThrowsExceptionInvalidType()
        {
            var data = new byte[10];
            data[0] = 0x64; // not a valid header  
            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadAny(ref reader, out _);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadBinary(ref reader, out _);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadInt64(ref reader, out _);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadString(ref reader, out _);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadTimestamp(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadUInt64(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadCompositeHeader(ref reader, out var value,
                    out var next);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadListHeader(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadMapHeader(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadUint32(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadUByte(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadUshort(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadUint32(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadInt32(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadFloat(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadDouble(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadSByte(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadBool(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadInt16(ref reader, out var value);
            });

            Assert.Throws<AmqpParseException>(() =>
            {
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
                AmqpWireFormatting.ReadUuid(ref reader, out var value);
            });

            System.Diagnostics.Trace.WriteLine(" test passed");
        }

        [Fact]
        public void ValidateFormatCode()
        {
            const bool boolTrue = true;
            var boolTrueBin = new byte[] { 0x41 };
            RunValidateFormatCode(boolTrue, boolTrueBin);

            const bool boolFalse = false;
            var boolFalseBin = new byte[] { 0x42 };
            RunValidateFormatCode(boolFalse, boolFalseBin);

            const ulong ulong0Value = 0x00;
            var ulong0ValueBin = new byte[] { 0x44 };
            RunValidateFormatCode(ulong0Value, ulong0ValueBin);

            const ulong ulongSmallValue = 0xf2;
            var ulongSmallValueBin = new byte[] { 0x53, 0xf2 };
            RunValidateFormatCode(ulongSmallValue, ulongSmallValueBin);

            const ulong ulongValue = 0x12345678edcba098;
            var ulongValueBin = new byte[] { 0x80, 0x12, 0x34, 0x56, 0x78, 0xed, 0xcb, 0xa0, 0x98 };
            RunValidateFormatCode(ulongValue, ulongValueBin);

            const byte ubyteValue = 0x33;
            var ubyteValueBin = new byte[] { 0x50, 0x33 };
            RunValidateFormatCode(ubyteValue, ubyteValueBin);

            const ushort ushortValue = 0x1234;
            var ushortValueBin = new byte[] { 0x60, 0x12, 0x34 };

            RunValidateFormatCode(ushortValue, ushortValueBin);

            const uint uint0Value = 0x00;
            var uint0ValueBin = new byte[] { 0x43 };
            RunValidateFormatCode(uint0Value, uint0ValueBin);

            const uint uintSmallValue = 0xe1;
            var uintSmallValueBin = new byte[] { 0x52, 0xe1 };
            RunValidateFormatCode(uintSmallValue, uintSmallValueBin);

            const uint uintValue = 0xedcba098;
            var uintValueBin = new byte[] { 0x70, 0xed, 0xcb, 0xa0, 0x98 };
            RunValidateFormatCode(uintValue, uintValueBin);

            const sbyte byteValue = -20;
            var byteValueBin = new byte[] { 0x51, 0xec };

            RunValidateFormatCode(byteValue, byteValueBin);

            const short shortValue = 0x5678;
            var shortValueBin = new byte[] { 0x61, 0x56, 0x78 };
            RunValidateFormatCode(shortValue, shortValueBin);

            // int intSmallValue = -77;
            // byte[] intSmallValueBin = new byte[] {0x54, 0xb3};
            // ValueTest(intSmallValue, intSmallValueBin);
            // //TODO Need to write another kind of the test since the intSmallValue is cast to byte so = 0xb3

            const int intValue = 0x56789a00;
            var intValueBin = new byte[] { 0x71, 0x56, 0x78, 0x9a, 0x00 };
            RunValidateFormatCode(intValue, intValueBin);

            const long longValue64 = -111111111111; //FFFFFFE62142FE39
            var longValueBin64 = new byte[] { 0x81, 0xff, 0xff, 0xff, 0xe6, 0x21, 0x42, 0xfe, 0x39 };
            RunValidateFormatCode(longValue64, longValueBin64);

            const long longValue8 = 127;
            var longValueBin8 = new byte[] { 0x55, 0x7F };
            RunValidateFormatCode(longValue8, longValueBin8);

            const float floatValue = -88.88f;
            var floatValueBin = new byte[] { 0x72, 0xc2, 0xb1, 0xc2, 0x8f };
            RunValidateFormatCode(floatValue, floatValueBin);

            var dtValue = DateTime.Parse("2008-11-01T19:35:00.0000000Z").ToUniversalTime();
            var dtValueBin = new byte[] { 0x83, 0x00, 0x00, 0x01, 0x1d, 0x59, 0x8d, 0x1e, 0xa0 };
            RunValidateFormatCode(dtValue, dtValueBin);

            const double doubleValue = 111111111111111.22222222222;
            var doubleValueBin = new byte[] { 0x82, 0x42, 0xd9, 0x43, 0x84, 0x93, 0xbc, 0x71, 0xce };
            RunValidateFormatCode(doubleValue, doubleValueBin);

            const string str8Value = "amqp";
            var str8Utf8ValueBin = new byte[] { 0xa1, 0x04, 0x61, 0x6d, 0x71, 0x70 };
            RunValidateFormatCode(str8Value, str8Utf8ValueBin);

            var str32Value = "";
            var str32Utf8ValueBin = new byte[290 + 1 + 4];
            str32Utf8ValueBin[0] = 0xb1;
            str32Utf8ValueBin[1] = 0x0;
            str32Utf8ValueBin[2] = 0x0;
            str32Utf8ValueBin[3] = 0x1;
            str32Utf8ValueBin[4] = 0x22;
            for (var i = 0; i < 290; i++)
            {
                str32Value += "a";
                str32Utf8ValueBin[i + 5] = 0x61;
            }

            RunValidateFormatCode(str32Value, str32Utf8ValueBin);

            var bin8Value = new byte[56];
            var bin32Value = new byte[500]; //
            var bin8ValueBin = new byte[1 + 1 + 56];
            var bin32ValueBin = new byte[1 + 4 + 500]; //
            RunValidateFormatCode(bin32Value, bin32ValueBin);
            RunValidateFormatCode(bin8Value, bin8ValueBin);

            // Guid uuidValue = new Guid("f275ea5e-0c57-4ad7-b11a-b20c563d3b71");
            // byte[] uuidValueBin = new byte[] { 0x98, 0xf2, 0x75, 0xea, 0x5e, 0x0c, 0x57, 0x4a, 0xd7, 0xb1, 0x1a, 0xb2, 0x0c, 0x56, 0x3d, 0x3b, 0x71 };
            // ValueTest(uuidValue, uuidValueBin);
        }

        private static void RunValidateFormatCode<T>(T value, byte[] result)
        {
            var array = new byte[result.Length];
            var arraySpan = new Span<byte>(array);
            AmqpWireFormatting.WriteAny(arraySpan, value);
            var arrayRead = new ReadOnlySequence<byte>(arraySpan.ToArray());
            var reader = new SequenceReader<byte>(arrayRead);
            AmqpWireFormatting.ReadAny(ref reader, out var decodeValue);
            if (typeof(T) == typeof(byte[]))
            {
                var b1 = (byte[])(object)value;
                var b2 = (byte[])(object)decodeValue;
                Assert.Equal(b1, b2);
            }
            else
            {
                Assert.Equal(arraySpan.ToArray(), result);
                Assert.Equal(value, decodeValue);
            }
        }

        [Fact]
        public void Validate32Bytes8BytesLists()
        {
            var value32Bin = new byte[] { 0xD0, 0x0, 0x0, 0x0, 0xF, 0x0, 0x0, 0x1, 0xF };
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(value32Bin));
            AmqpWireFormatting.ReadListHeader(ref reader, out var len32);
            Assert.Equal(271, len32);

            var value8Bin = new byte[] { 0xc0, 0xF, 0xF0 };
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(value8Bin));
            AmqpWireFormatting.ReadListHeader(ref reader, out var len8);
            Assert.Equal(240, len8);

            var value0Bin = new byte[] { 0x45 };
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(value0Bin));
            AmqpWireFormatting.ReadListHeader(ref reader, out var len0);
            Assert.Equal(0, len0);

            var valueComposite8Bin = new byte[] { 0x0, 0x53, 0x73, 0xc0, 0xF, 0xF0 };
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(valueComposite8Bin));

            AmqpWireFormatting.ReadCompositeHeader(ref reader,
                out var compositeLen32, out _);
            Assert.Equal(240, compositeLen32);
        }

        [Fact]
        public void ValidateMessagesFromGo()
        {
            // These files are generated with the Go AMQP 1.0 client
            // The idea is to have an external validation for the messages
            // see: https://github.com/rabbitmq/rabbitmq-stream-go-client/tree/main/generate
            // dump messages are included as resources.
            // remove these tests at some point ?? 
            //  body len < 250 bytes
            var body250 = SystemUtils.GetFileContent("message_body_250");
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(body250));
            var msg250 = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msg250);
            Assert.True(msg250.Data.Size <= byte.MaxValue);
            Assert.Null(msg250.ApplicationProperties);
            Assert.Null(msg250.Properties);

            //  body len > 256 bytes important to read the int in different way 
            //  body len >= 700 bytes 
            var body700 = SystemUtils.GetFileContent("message_body_700");
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(body700));
            var msg700 = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msg700);
            Assert.True(msg700.Data.Size > byte.MaxValue);
            Assert.Null(msg700.Properties);

            //  body len >= 300 bytes 
            // also the ApplicationProperties are not null
            var prop300 = SystemUtils.GetFileContent("message_random_application_properties_300");
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(prop300));
            var msgProp300 = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msgProp300);
            Assert.True(msgProp300.Data.Size > byte.MaxValue);
            Assert.NotNull(msgProp300.ApplicationProperties);

            //  body len >= 500 bytes 
            // also the ApplicationProperties are not null
            var prop500 = SystemUtils.GetFileContent("message_random_application_properties_500");
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(prop500));
            var msgProp500 = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msgProp500);
            Assert.NotNull(msgProp500.ApplicationProperties);

            //  body len >= 900 bytes 
            // ApplicationProperties are not null 
            // Properties is not null
            var prop900 = SystemUtils.GetFileContent("message_random_application_properties_properties_900");
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(prop900));
            var msgProp900 = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msgProp900);
            Assert.NotNull(msgProp900.ApplicationProperties);
            foreach (var (key, value) in msgProp900.ApplicationProperties)
            {
                Assert.True(((string)key).Length >= 900);
                Assert.True(((string)value).Length >= 900);
            }

            Assert.NotNull(msgProp900.Properties);
            Assert.True(!string.IsNullOrEmpty(msgProp900.Properties.ReplyTo));
            Assert.True(!string.IsNullOrEmpty(msgProp900.Properties.GroupId));
            Assert.Equal((ulong)33333333, msgProp900.Properties.MessageId);
            // UUID value: 00112233-4455-6677-8899-aabbccddeeff
            var uuid_value = Enumerable.Range(0, 16).Select(x => (byte)(x << 4 | x)).ToArray();
            Assert.Equal(uuid_value, msgProp900.Properties.CorrelationId as byte[]);
            Assert.Equal("json", msgProp900.Properties.ContentType);
            Assert.Equal("myCoding", msgProp900.Properties.ContentEncoding);
            Assert.Equal((uint)10, msgProp900.Properties.GroupSequence);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1710440652).Ticks, msgProp900.Properties.CreationTime.Ticks);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1710440652).Ticks, msgProp900.Properties.AbsoluteExpiryTime.Ticks);

            // Test message to check if all the fields with "test" value
            var staticTest = SystemUtils.GetFileContent("static_test_message_compare");
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(staticTest));
            var msgStaticTest = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msgStaticTest);
            Assert.Equal("test", Encoding.Default.GetString(msgStaticTest.Data.Contents.ToArray()));
            Assert.Equal("test", msgStaticTest.Properties.Subject);
            Assert.Equal("test", msgStaticTest.Properties.MessageId);
            Assert.Equal("test", msgStaticTest.Properties.ReplyTo);
            Assert.Equal("test", msgStaticTest.Properties.ContentType);
            Assert.Equal("test", msgStaticTest.Properties.ContentEncoding);
            Assert.Equal("test", msgStaticTest.Properties.GroupId);
            Assert.Equal("test", msgStaticTest.Properties.ReplyToGroupId);
            Assert.Equal("test", Encoding.Default.GetString(msgStaticTest.Properties.UserId));
            Assert.Equal("test", msgStaticTest.ApplicationProperties["test"]);
            Assert.Equal(64.646464, msgStaticTest.ApplicationProperties["double"]);

            Assert.Equal("test", msgStaticTest.Annotations["test"]);
            Assert.Equal((long)1, msgStaticTest.Annotations[(long)1]);
            Assert.Equal((long)100_000, msgStaticTest.Annotations[(long)100_000]);

            var header = SystemUtils.GetFileContent("header_amqpvalue_message");
            reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(header));
            var msgHeader = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msgHeader);
            Assert.NotNull(msgHeader.MessageHeader);
            Assert.NotNull(msgHeader.AmqpValue);
            Assert.True(msgHeader.MessageHeader.Durable);
            Assert.True(msgHeader.MessageHeader.FirstAcquirer);
            Assert.Equal(100, msgHeader.MessageHeader.Priority);
            Assert.Equal((uint)300, msgHeader.MessageHeader.DeliveryCount);
            Assert.True(msgHeader.MessageHeader.Ttl == 0);
            Assert.Equal("amqpValue", msgHeader.AmqpValue);
        }

        [Fact]
        public void ValidateMessagesFromGoUnicode()
        {
            const string ByteString =
                "Alan  Mathison Turing  ( 23 June 1912 – 7 June 1954 ) was an English  mathematician, computer scientist, logician, cryptanalyst,  philosopher, and theoretical biologist. Turing  was   highly  influential in the development of theoretical computer science.";
            const string ChineseStringTest =
                "Alan Mathison Turing（1912 年 6 月 23 日 - 1954 年 6 月 7 日）是英国数学家、计算机科学家、逻辑学家、密码分析家、哲学家和理论生物学家。 [6] 图灵在理论计算机科学的发展中具有很大的影响力，用图灵机提供了算法和计算概念的形式化，可以被认为是通用计算机的模型。[7][8][9] 他被广泛认为是理论计算机科学和人工智能之父。 [10]";

            const string GreekTest =
                "Ο Άλαν Μάθισον Τούρινγκ (23 Ιουνίου 1912 – 7 Ιουνίου 1954) ήταν Άγγλος μαθηματικός, επιστήμονας υπολογιστών, λογικός, κρυπαναλυτής, φιλόσοφος και θεωρητικός βιολόγος. Ο Τούρινγκ είχε μεγάλη επιρροή στην ανάπτυξη της θεωρητικής επιστήμης των υπολογιστών.";

            var staticTest = SystemUtils.GetFileContent("message_unicode_message");
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(staticTest));
            var msgStaticTest = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msgStaticTest);
            Assert.Equal(ByteString, Encoding.Default.GetString(msgStaticTest.Data.Contents.ToArray()));
            Assert.Equal(ChineseStringTest, msgStaticTest.ApplicationProperties["from_go_ch_long"]);
            Assert.Equal(GreekTest, msgStaticTest.ApplicationProperties["from_go_greek"]);
            Assert.Equal("祝您有美好的一天，并享受客户", msgStaticTest.ApplicationProperties["from_go"]);
            Assert.Equal(ByteString, msgStaticTest.ApplicationProperties["from_go_byte"]);
        }

        [Fact]
        public void ValidateUuidMessagesFromGo()
        {
            // UUID value: 00112233-4455-6677-8899-aabbccddeeff
            var uuid_value = Enumerable.Range(0, 16).Select(x => (byte)(x << 4 | x)).ToArray();

            var buffer = SystemUtils.GetFileContent("uuid_message");
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));
            var uuid_message = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(uuid_message);
            Assert.Equal(uuid_value, uuid_message.Properties.MessageId);
            Assert.Equal(uuid_value, uuid_message.Properties.CorrelationId);
        }

        [Fact]
        public void ValidateAnnotationMap()
        {
            // shovel_annotations is a message with a map annotation
            // coming from the Go client and the following configuration:
            // source queue: "form"
            // destination exchange: "to"
            // queue bound to the exchange
            // shovel from the source queue to the destination exchange
            // the annotations will be added to the message

            var buffer = SystemUtils.GetFileContent("shovel_annotations");
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));
            var shovelAnnotation = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(shovelAnnotation);
            Assert.NotNull(shovelAnnotation.Annotations["x-shovelled"]);
            var xShovelled = shovelAnnotation.Annotations["x-shovelled"] as Dictionary<string, object>;
            Assert.NotNull(xShovelled);
            Assert.Equal("hello-key", xShovelled["dest-exchange-key"]);
            Assert.Equal("from", xShovelled["src-queue"]);
            Assert.Equal("to", xShovelled["dest-exchange"]);
            Assert.Equal("dynamic", xShovelled["shovel-type"]);
        }

        [Fact]
        public void ValidateNilMessagesFromGo()
        {
            var nilAndTypes = SystemUtils.GetFileContent("nil_and_types");
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(nilAndTypes));
            var msgNilAndTypes = Message.From(ref reader, (uint)reader.Length);
            Assert.NotNull(msgNilAndTypes);
            Assert.Equal(0, msgNilAndTypes.Data.Contents.Length);
            Assert.Null(msgNilAndTypes.ApplicationProperties["null"]);
            Assert.Equal(91_000_001_001, msgNilAndTypes.ApplicationProperties["long_value"]);
            Assert.Equal((byte)216, msgNilAndTypes.ApplicationProperties["byte_value"]);
            Assert.Equal(true, msgNilAndTypes.ApplicationProperties["bool_value"]);
            Assert.Equal(1, (long)msgNilAndTypes.ApplicationProperties["int_value"]); // int in Go has a platform-dependent size.
            Assert.Equal(1.1, msgNilAndTypes.ApplicationProperties["float"]);
            Assert.Equal(1.1, msgNilAndTypes.ApplicationProperties["double"]);
            // UUID value: 00112233-4455-6677-8899-aabbccddeeff
            var uuid_value = Enumerable.Range(0, 16).Select(x => (byte)(x << 4 | x)).ToArray();
            Assert.Equal(uuid_value, msgNilAndTypes.ApplicationProperties["uuid"] as byte[]);
            Assert.Equal("", msgNilAndTypes.ApplicationProperties["empty"]);
        }

        [Fact]
        public void ValidateMapsType()
        {
            const double DoubleValue = 6665555.34566;
            var dt = DateTime.Now;
            var app = new ApplicationProperties()
            {
                ["double_value"] = DoubleValue,
                ["string_value"] = "test",
                ["bool_value"] = true,
                ["byte_value"] = (byte)1,
                ["short_value"] = (short)1,
                ["int_value"] = 1,
                ["long_value"] = 1L,
                ["ulong_value"] = 1UL,
                ["float_value"] = 1.0f,
                ["date_value"] = dt
            };
            var m = new Message(Encoding.Default.GetBytes("hello")) { ApplicationProperties = app };
            Assert.NotNull(m.ApplicationProperties);

            Assert.Equal(14, AmqpWireFormatting.GetAnySize("string_value"));
            Assert.Equal(9, AmqpWireFormatting.GetAnySize(DoubleValue));
            Assert.Equal(1, AmqpWireFormatting.GetAnySize(true));
            Assert.Equal(2, AmqpWireFormatting.GetAnySize((byte)1));
            Assert.Equal(3, AmqpWireFormatting.GetAnySize((short)1));
            // In this case is a byte
            Assert.Equal(2, AmqpWireFormatting.GetAnySize(1));
            // In this case is a byte less than 128
            Assert.Equal(2, AmqpWireFormatting.GetAnySize(1L));
            // In this case is a long 
            Assert.Equal(9, AmqpWireFormatting.GetAnySize(1000L));

            // byte 
            Assert.Equal(2, AmqpWireFormatting.GetAnySize(1UL));
            // ulong
            Assert.Equal(9, AmqpWireFormatting.GetAnySize(1000UL));

            Assert.Equal(5, AmqpWireFormatting.GetAnySize(1.0f));

            Assert.Equal(9, AmqpWireFormatting.GetAnySize(dt));

            var size = DescribedFormatCode.Size;
            size += sizeof(byte); //FormatCode.List32
            size += sizeof(uint); // field numbers
            size += sizeof(uint); // PropertySize
            size += AmqpWireFormatting.GetAnySize("double_value");
            size += AmqpWireFormatting.GetAnySize(DoubleValue);
            size += AmqpWireFormatting.GetAnySize("string_value");
            size += AmqpWireFormatting.GetAnySize("test");
            size += AmqpWireFormatting.GetAnySize("bool_value");
            size += AmqpWireFormatting.GetAnySize(true);
            size += AmqpWireFormatting.GetAnySize("byte_value");
            size += AmqpWireFormatting.GetAnySize((byte)1);
            size += AmqpWireFormatting.GetAnySize("short_value");
            size += AmqpWireFormatting.GetAnySize((short)1);
            size += AmqpWireFormatting.GetAnySize("int_value");
            size += AmqpWireFormatting.GetAnySize(1);
            size += AmqpWireFormatting.GetAnySize("long_value");
            size += AmqpWireFormatting.GetAnySize(1L);
            size += AmqpWireFormatting.GetAnySize("ulong_value");
            size += AmqpWireFormatting.GetAnySize(1UL);
            size += AmqpWireFormatting.GetAnySize("float_value");
            size += AmqpWireFormatting.GetAnySize(1.0f);
            size += AmqpWireFormatting.GetAnySize("date_value");
            size += AmqpWireFormatting.GetAnySize(dt);

            Assert.Equal(size, m.ApplicationProperties.Size);
        }

        [Fact]
        public void MapEntriesWithAnEmptyKeyShouldNotBeWrittenToTheWire()
        {
            // Given we have an annotation with a valid key
            var annotation = new Annotations { { "valid key", "" } };

            var expectedMapSize = annotation.Size;
            var array = new byte[expectedMapSize];
            var arraySpan = new Span<byte>(array);

            Assert.Equal(expectedMapSize, annotation.Write(arraySpan));

            // when we add a empty key and write the annotation again
            annotation.Add("", "");
            arraySpan.Clear();
            var actualMapSize = annotation.Write(arraySpan);

            // we do not expect the new entry to be written
            Assert.Equal(expectedMapSize, actualMapSize);
        }

        [Theory]
        // AmqpWireFormattingWrite.GetSequenceSize + DescribedFormat.Size
        [InlineData(254, 254 + 1 + 1 + 3)]
        [InlineData(255, 255 + 1 + 1 + 3)]
        [InlineData(256, 256 + 1 + 4 + 3)]
        public void WriteDataEdgeCasesTests(int size, int expectedLengthWritten)
        {
            var buffer = new Span<byte>(new byte[4096]);
            var bytes = new byte[size];
            var data = new Data(new ReadOnlySequence<byte>(bytes));
            var written = data.Write(buffer);
            Assert.Equal(expectedLengthWritten, written);
        }
    }
}
