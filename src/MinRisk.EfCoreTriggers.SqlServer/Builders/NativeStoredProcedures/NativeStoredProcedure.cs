using System;
using System.Collections.Generic;
using System.Text;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public class NativeStoredProcedure : INativeStoredProcedure
    {        
        public string Sql { get; }
        public int Order { get; }

        public string Name { get; private set; }

        public NativeStoredProcedure(String name, string sql, int order)
        {
            Sql = sql;
            Name = name;
            Name = $"{Constants.NativeStoredProcedureAnnotationKey}_{order}_{name}"
                .ToUpper();
        }

    }
}
