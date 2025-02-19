using System;
using System.Collections.Generic;
using System.Text;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public interface INativeStoredProcedure
    {
        public string Name { get; }

        public string Sql { get; }
    }
}
