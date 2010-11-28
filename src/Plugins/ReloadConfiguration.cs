﻿using System.ComponentModel.Composition;
using Core.Abstractions;

namespace Plugins
{
    [Export(typeof (ICommand))]
    public class ReloadConfiguration : ICommand
    {
        [Import(AllowRecomposition = true)]
        public ILoadConfiguration LoadConfiguration { get; set; }

        public string Text
        {
            get { return "Reload configuration"; }
        }

        public string Description
        {
            get { return "Reload configuration from disk"; }
        }

        public void Execute()
        {
            LoadConfiguration.Reload();
        }
    }
}