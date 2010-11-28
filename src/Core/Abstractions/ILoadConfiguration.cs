﻿using System.ComponentModel.Composition.Hosting;

namespace Core.Abstractions
{
    public interface ILoadConfiguration
    {
        void Load(CompositionContainer container);
        void Reload();
    }
}