// SPDX-License-Identifier: 0BSD

using Vezel.Novadrop.Data.Serialization.Items;

namespace Vezel.Novadrop.Data.Serialization.Regions;

internal sealed class DataCenterSimpleRegion<T>
    where T : struct, IDataCenterItem<T>
{
    private readonly bool _offByOne;

    public List<T> Elements { get; } = new(ushort.MaxValue);

    public DataCenterSimpleRegion(bool offByOne)
    {
        _offByOne = offByOne;
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public async ValueTask ReadAsync(
        DataCenterArchitecture architecture, StreamBinaryReader reader, CancellationToken cancellationToken)
    {
        var count = await reader.ReadInt32Async(cancellationToken).ConfigureAwait(false);

        if (_offByOne)
            count--;

        Check.Data(count >= 0, $"Region length {count} is negative.");

        var length = T.GetSize(architecture) * count;
        var bytes = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            await reader.ReadAsync(bytes.AsMemory(0, length), cancellationToken).ConfigureAwait(false);

            void ReadElements()
            {
                var reader = new SpanReader(bytes);

                for (var i = 0; i < count; i++)
                {
                    T.Read(ref reader, architecture, out var elem);

                    Elements.Add(elem);
                }
            }

            // Cannot use refs in async methods...
            ReadElements();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public async ValueTask WriteAsync(
        DataCenterArchitecture architecture, StreamBinaryWriter writer, CancellationToken cancellationToken)
    {
        var count = Elements.Count;

        await writer.WriteInt32Async(count + (_offByOne ? 1 : 0), cancellationToken).ConfigureAwait(false);

        var length = T.GetSize(architecture) * count;
        var bytes = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            void WriteElements()
            {
                var writer = new SpanWriter(bytes);

                foreach (var elem in Elements)
                    T.Write(ref writer, architecture, elem);
            }

            // Cannot use refs in async methods...
            WriteElements();

            await writer.WriteAsync(bytes.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    public T GetElement(int index)
    {
        Check.Data(index < Elements.Count, $"Region element index {index} is out of bounds (0..{Elements.Count}).");

        return Elements[index];
    }

    public void SetElement(int index, T value)
    {
        Elements[index] = value;
    }
}
