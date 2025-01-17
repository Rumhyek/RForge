using Microsoft.AspNetCore.Components;

namespace RForgeBlazor.Models;

/// <summary>
/// Represents a base class for filter select components in a data grid.
/// </summary>
/// <typeparam name="TType">The type of the filter option value.</typeparam>
public abstract class RfDgFilterSelect<TType> : RfDgFilterInput<TType>
{
    #region Parameters

    /// <summary>
    /// Gets or sets the list of filter options.
    /// </summary>
    [Parameter]
    public List<RfDgFilterOption<TType>> Options { get; set; }

    #endregion
}
