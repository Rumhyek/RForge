using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace RForge.Blazor.AutoApi.Generator
{
    /// <summary>An immutable array with value equality, so generator models cache correctly.</summary>
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {
        public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

        private readonly ImmutableArray<T> _items;

        public EquatableArray(ImmutableArray<T> items) => _items = items;

        public ImmutableArray<T> Items => _items.IsDefault ? ImmutableArray<T>.Empty : _items;
        public int Length => Items.Length;
        public T this[int index] => Items[index];

        public bool Equals(EquatableArray<T> other) => Items.SequenceEqual(other.Items);
        public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var item in Items) hash = unchecked(hash * 31 + (item?.GetHashCode() ?? 0));
            return hash;
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static class EquatableArrayExtensions
    {
        public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> items) where T : IEquatable<T>
            => new(items.ToImmutableArray());
    }

    internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
    {
        public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);

        public static LocationInfo? From(Location? location)
        {
            if (location is null || location.SourceTree is null) return null;
            return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
        }
    }

    internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> Args)
    {
        public Diagnostic ToDiagnostic()
            => Diagnostic.Create(Descriptor, Location?.ToLocation(), Args.Items.Cast<object>().ToArray());

        public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] args)
            => new(descriptor, location, args.ToEquatableArray());
    }

    /// <summary>Minimal indented text writer for generated source.</summary>
    internal sealed class CodeWriter
    {
        private readonly StringBuilder _sb = new();
        private int _indent;

        public CodeWriter Line(string text = "")
        {
            if (text.Length > 0) _sb.Append(' ', _indent * 4);
            _sb.Append(text).Append('\n');
            return this;
        }

        public CodeWriter Open(string text)
        {
            Line(text);
            Line("{");
            _indent++;
            return this;
        }

        public CodeWriter Close(string suffix = "")
        {
            _indent--;
            Line("}" + suffix);
            return this;
        }

        public CodeWriter Indent() { _indent++; return this; }
        public CodeWriter Outdent() { _indent--; return this; }

        public override string ToString() => _sb.ToString();
    }
}
