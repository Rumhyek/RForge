using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeBlazor.Services
{
    internal static class CssHelper
    {

        internal static void AddIfTrue(ref string css, bool shouldAdd, string addClass)
        {
            if (shouldAdd == false) return;

            if (css == null) 
                css = addClass;
            else 
                css += $" {addClass}";
        }
    }
}
