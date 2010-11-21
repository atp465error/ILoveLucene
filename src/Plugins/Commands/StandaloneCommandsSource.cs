﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;
using Core.Abstractions;

namespace Plugins.Commands
{
    [Export(typeof (IItemSource))]
    public class StandaloneCommandsSource : IItemSource, IPartImportsSatisfiedNotification
    {
        private readonly CompositionContainer _mefContainer;

        [ImportMany(AllowRecomposition = true)] 
        public IEnumerable<ICommand> Commands;

        [ImportingConstructor]
        public StandaloneCommandsSource(CompositionContainer mefContainer)
        {
            _mefContainer = mefContainer;
            _mefContainer.SatisfyImportsOnce(this);
        }

        public bool NeedsReindexing { get; private set; }

        public Task<IEnumerable<object>> GetItems()
        {
            NeedsReindexing = false;
            var commands = Commands; //FIXME:  this can be recomposed mid-iteration. we don't want that for now
            return Task.Factory.StartNew(() => commands.Cast<Object>());
        }

        public void OnImportsSatisfied()
        {
            NeedsReindexing = true;
        }
    }
}