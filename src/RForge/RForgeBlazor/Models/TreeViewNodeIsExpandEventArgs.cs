using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor.Models;

/// <summary>
/// Provides data for the <see cref="RfTreeNode.NodeExpandChange" /> event callback.
/// </summary>
public class TreeViewNodeIsExpandEventArgs
{
    /// <summary>
    /// The reference to the instance of <see cref="RfTreeNode" />.
    /// </summary>
    public RfTreeNode NodeReference { get; internal set; }

    /// <summary>
    /// A value indicating whether the node is expanded.
    /// </summary>
    public bool IsExpanded { get; internal set; }
}
