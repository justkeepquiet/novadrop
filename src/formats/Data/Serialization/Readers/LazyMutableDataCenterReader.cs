// SPDX-License-Identifier: 0BSD

using Vezel.Novadrop.Data.Nodes;
using Vezel.Novadrop.Data.Serialization.Items;

namespace Vezel.Novadrop.Data.Serialization.Readers;

internal sealed class LazyMutableDataCenterReader : DataCenterReader
{
    public LazyMutableDataCenterReader(DataCenterLoadOptions options)
        : base(options)
    {
    }

    protected override LazyMutableDataCenterNode AllocateNode(
        DataCenterAddress address,
        DataCenterRawNode raw,
        DataCenterNode? parent,
        string name,
        string? value,
        DataCenterKeys keys,
        CancellationToken cancellationToken)
    {
        LazyMutableDataCenterNode node = null!;

        return node = new(
            parent,
            name,
            value,
            keys,
            () =>
            {
                var attributes = new OrderedDictionary<string, DataCenterValue>(
                    raw.AttributeCount - (value != null ? 1 : 0));

                ReadAttributes(
                    raw,
                    attributes,
                    static (attributes, name, value) =>
                        Check.Data(
                            attributes.TryAdd(name, value), $"Attribute named '{name}' was already recorded earlier."));

                return attributes;
            },
            () =>
            {
                var children = new List<DataCenterNode>(raw.ChildCount);

                ReadChildren(
                    raw, node, children, static (children, node) => children.Add(node), CancellationToken.None);

                return children;
            });
    }

    protected override LazyMutableDataCenterNode? ResolveNode(
        DataCenterAddress address, DataCenterNode? parent, CancellationToken cancellationToken)
    {
        return Unsafe.As<LazyMutableDataCenterNode>(CreateNode(address, parent, CancellationToken.None));
    }
}
