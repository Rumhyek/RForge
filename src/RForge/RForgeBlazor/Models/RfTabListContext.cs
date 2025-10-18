using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor.Models;

/// <summary>
/// Provides a context for managing tab registration events in the RF Tab system.
/// </summary>
/// <remarks>This class allows subscribers to handle tab registration events asynchronously through the <see
/// cref="RfTabList.Context_OnTabRegister(object, RForgeBlazor.Models.RfTabOnRegisterEventArgs)"/> event. Use the <see cref="RaiseOnTabRegister"/> method to trigger the event and notify all
/// registered handlers.</remarks>
public class RfTabListContext
{
    /// <summary>
    /// Occurs when a tab is registered, providing event data related to the registration process.
    /// </summary>
    /// <remarks>This event is triggered asynchronously and allows subscribers to handle actions or perform
    /// operations  when a tab is successfully registered. The event provides an instance of <see
    /// cref="RfTabOnRegisterEventArgs"/>  containing details about the registration.</remarks>
    public event AsyncEventHandler<RfTabOnRegisterEventArgs> OnTabRegister;
    /// <summary>
    /// Raises the <see cref="OnTabRegister"/> event asynchronously with the specified event arguments.
    /// </summary>
    /// <remarks>This method invokes the <see cref="OnTabRegister"/> event if it has subscribers. The
    /// invocation is performed asynchronously. Ensure that the <paramref name="args"/> parameter is properly
    /// initialized before calling this method.</remarks>
    /// <param name="args">The event arguments containing details for the tab registration event. Cannot be <c>null</c>.</param>
    /// <returns></returns>
    public async Task RaiseOnTabRegister(RfTabOnRegisterEventArgs args)
    {
        if (OnTabRegister != null)
        {
            await OnTabRegister.Invoke(this, args);
        }
    }
}

/// <summary>
/// Provides data for the event that occurs when an <see cref="RfTab"/> is registered.
/// </summary>
/// <param name="Tab">The <see cref="RfTab"/> instance that is being registered.</param>
public record RfTabOnRegisterEventArgs(RfTab Tab);