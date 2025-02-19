using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public interface INativeStoredProcedureModelDiffer
    {
        /// <summary>
        /// Add trigger migration operations to the list of migration operations.
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public IReadOnlyList<MigrationOperation> AddNativeOperations(
            IEnumerable<MigrationOperation> operations,
            IRelationalModel? source,
            IRelationalModel? target);
    }
}
