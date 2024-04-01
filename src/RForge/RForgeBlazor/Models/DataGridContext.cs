using RForge.Abstractions.DataGrids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor.Models;

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

public class DataGridContext
{
    /// <summary>
    /// An event that is raised when a the sort changes
    /// </summary>
    public event AsyncEventHandler<DataGridSortBy> OnSortChanged;
    /// <summary>
    /// An event that is raised when a fitler changes.
    /// </summary>
    public event AsyncEventHandler<DataGridFilterBy> OnFilterChanged;


    public async Task SortChanged(DataGridSortBy sort)
    {
        if (OnSortChanged != null)
            await OnSortChanged.Invoke(this, sort);
    }

    public async Task FilterChanged(DataGridFilterBy filter)
    {
        if (OnFilterChanged != null)
            await OnFilterChanged.Invoke(this, filter);
    }
}
