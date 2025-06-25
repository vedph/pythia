using MessagePack;
using Pythia.Tagger.Lookup;
using System;
using System.IO;
using Xunit;

namespace Pythia.Tagger.Test.Lookup;

public sealed class MessagePackLookupEntrySerializerTest
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MessagePackLookupEntrySerializer(null!));
    }

    [Fact]
    public void Constructor_DefaultOptions_SetsDefaultOptions()
    {
        MessagePackLookupEntrySerializer serializer = new();

        // test is successful if no exception is thrown
        Assert.NotNull(serializer);
    }

    [Fact]
    public void Serialize_NullStream_ThrowsArgumentNullException()
    {
        MessagePackLookupEntrySerializer serializer = new();
        LookupEntry entry = new() { Id = 1, Value = "test" };

        Assert.Throws<ArgumentNullException>(() =>
            serializer.Serialize(null!, entry));
    }

    [Fact]
    public void Serialize_NullEntry_ThrowsArgumentNullException()
    {
        MessagePackLookupEntrySerializer serializer = new();
        using MemoryStream stream = new();

        Assert.Throws<ArgumentNullException>(() =>
            serializer.Serialize(stream, null!));
    }

    [Fact]
    public void Deserialize_NullStream_ThrowsArgumentNullException()
    {
        MessagePackLookupEntrySerializer serializer = new();

        Assert.Throws<ArgumentNullException>(() =>
            serializer.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_EmptyStream_ReturnsNull()
    {
        MessagePackLookupEntrySerializer serializer = new();
        using MemoryStream stream = new();

        LookupEntry? result = serializer.Deserialize(stream);

        Assert.Null(result);
    }

    [Fact]
    public void SerializeDeserialize_SimpleEntry_RoundTripsCorrectly()
    {
        MessagePackLookupEntrySerializer serializer = new();
        using MemoryStream stream = new();
        LookupEntry originalEntry = new()
        {
            Id = 42,
            Value = "test-value",
            Text = "test-text",
            Lemma = "test-lemma",
            Pos = "NOUN"
        };

        serializer.Serialize(stream, originalEntry);
        stream.Position = 0; // reset stream position for reading
        LookupEntry? deserializedEntry = serializer.Deserialize(stream);

        Assert.NotNull(deserializedEntry);
        Assert.Equal(originalEntry.Id, deserializedEntry.Id);
        Assert.Equal(originalEntry.Value, deserializedEntry.Value);
        Assert.Equal(originalEntry.Text, deserializedEntry.Text);
        Assert.Equal(originalEntry.Lemma, deserializedEntry.Lemma);
        Assert.Equal(originalEntry.Pos, deserializedEntry.Pos);
    }

    [Fact]
    public void SerializeDeserialize_MultipleEntries_ReadsCorrectly()
    {
        MessagePackLookupEntrySerializer serializer = new();
        using MemoryStream stream = new();
        LookupEntry[] entries = new[]
        {
            new LookupEntry { Id = 1, Value = "first", Text = "First entry" },
            new LookupEntry { Id = 2, Value = "second", Text = "Second entry" },
            new LookupEntry { Id = 3, Value = "third", Text = "Third entry" }
        };

        foreach (LookupEntry? entry in entries)
            serializer.Serialize(stream, entry);

        // reset position and read them back
        stream.Position = 0;
        LookupEntry[] results = new LookupEntry[3];

        for (int i = 0; i < 3; i++)
        {
            results[i] = serializer.Deserialize(stream)!;
        }

        for (int i = 0; i < 3; i++)
        {
            Assert.NotNull(results[i]);
            Assert.Equal(entries[i].Id, results[i].Id);
            Assert.Equal(entries[i].Value, results[i].Value);
            Assert.Equal(entries[i].Text, results[i].Text);
        }

        // there should be no more entries
        Assert.Null(serializer.Deserialize(stream));
    }

    [Fact]
    public void Deserialize_EndOfStream_ReturnsNull()
    {
        MessagePackLookupEntrySerializer serializer = new();

        // create a stream that will throw EndOfStreamException when read
        MockEndOfStreamExceptionStream mockStream = new();

        LookupEntry? result = serializer.Deserialize(mockStream);
        Assert.Null(result);
    }

    [Fact]
    public void SerializeDeserialize_WithCustomOptions_RoundTripsCorrectly()
    {
        MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        MessagePackLookupEntrySerializer serializer = new(options);

        using MemoryStream stream = new();
        LookupEntry originalEntry = new() { Id = 100, Value = "compressed-value" };

        serializer.Serialize(stream, originalEntry);
        stream.Position = 0;
        LookupEntry? deserializedEntry = serializer.Deserialize(stream);

        Assert.NotNull(deserializedEntry);
        Assert.Equal(originalEntry.Id, deserializedEntry.Id);
        Assert.Equal(originalEntry.Value, deserializedEntry.Value);
    }

    /// <summary>
    /// Helper mock stream that throws EndOfStreamException when Read is called
    /// </summary>
    private sealed class MockEndOfStreamExceptionStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotImplementedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new EndOfStreamException();
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotImplementedException();

        public override void SetLength(long value) =>
            throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotImplementedException();
    }
}