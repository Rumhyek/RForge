using Microsoft.AspNetCore.Components;

namespace RForgeBlazor.Models;

public abstract class RfDgFilterSelect<TType> : RfDgFilterInput<TType>
{
    #region Parameters

    [Parameter]
    public List<RfDgFilterOption<TType>> Options { get; set; }

    #endregion
}
