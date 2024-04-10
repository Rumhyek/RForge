﻿using System;
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
        internal static void AddIfTrue(ref string css, bool shouldAdd, Func<string> classToAdd)
        {
            if (shouldAdd == false) return;

            if (css == null)
                css = classToAdd();
            else
                css += $" {classToAdd()}";
        }

        internal static string AddIf(bool  shouldAdd, string addClass)
        {
            if (shouldAdd == false) return null;

            return addClass;
        }
    }
}
