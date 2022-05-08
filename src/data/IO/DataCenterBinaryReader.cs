namespace Vezel.Novadrop.Data.IO;

sealed class DataCenterBinaryReader
{
    public long Progress { get; private set; }

    readonly Stream _stream;

    readonly Memory<byte> _buffer = new byte[sizeof(uint)];

    public DataCenterBinaryReader(Stream stream)
    {
        _stream = stream;
    }

    public async ValueTask ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var progress = 0;

        while (progress < buffer.Length)
        {
            var len = await _stream.ReadAsync(buffer[progress..], cancellationToken).ConfigureAwait(false);

            if (len == 0)
                throw new EndOfStreamException();

            progress += len;
        }

        Progress += progress;
    }

    public async ValueTask<ushort> ReadUInt16Async(CancellationToken cancellationToken)
    {
        await ReadAsync(_buffer[..sizeof(ushort)], cancellationToken).ConfigureAwait(false);

        return BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Span);
    }

    public async ValueTask<uint> ReadUInt32Async(CancellationToken cancellationToken)
    {
        await ReadAsync(_buffer[..sizeof(uint)], cancellationToken).ConfigureAwait(false);

        return BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Span);
    }

    public async ValueTask<int> ReadInt32Async(CancellationToken cancellationToken)
    {
        return (int)await ReadUInt32Async(cancellationToken).ConfigureAwait(false);
    }
}
