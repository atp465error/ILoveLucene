﻿using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Plugins.Commands;

namespace Plugins
{
    [Export(typeof(ICommand))]
    public class EmptyRecycleBin : BaseCommand
    {
        enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        public override void Act()
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, 0);
        }
    }
}