using Laraue.EfCoreTriggers.Common.Migrations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public class NativeMigrationsModelDiffer : MigrationsModelDiffer
    {
        private readonly INativeStoredProcedureModelDiffer _nativeStoredProcedureModelDiffer;

        /// <summary>
        /// Initializes a new instance of <see cref="NativeMigrationsModelDiffer"/>.
        /// </summary>
#if NET9_0_OR_GREATER
        public NativeMigrationsModelDiffer(
            IRelationalTypeMappingSource typeMappingSource,
            IMigrationsAnnotationProvider migrationsAnnotationProvider,
            IRelationalAnnotationProvider relationalAnnotationProvider,
            IRowIdentityMapFactory rowIdentityMapFactory,
            CommandBatchPreparerDependencies commandBatchPreparerDependencies,
            ITriggerModelDiffer triggerModelDiffer,
            INativeStoredProcedureModelDiffer nativeModelDiffer) 
            : base(
                typeMappingSource,
                migrationsAnnotationProvider,
                relationalAnnotationProvider,
                rowIdentityMapFactory,
                commandBatchPreparerDependencies,
                triggerModelDiffer)
        {
            _nativeStoredProcedureModelDiffer = nativeModelDiffer;
        }
#elif NET6_0_OR_GREATER
        public NativeMigrationsModelDiffer(
            IRelationalTypeMappingSource typeMappingSource,
            IMigrationsAnnotationProvider migrationsAnnotationProvider,
            IRowIdentityMapFactory rowIdentityMapFactory,
            CommandBatchPreparerDependencies commandBatchPreparerDependencies,
            ITriggerModelDiffer triggerModelDiffer,
            INativeStoredProcedureModelDiffer nativeModelDiffer) 
            : base(typeMappingSource, migrationsAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies, triggerModelDiffer)
        {
            _nativeStoredProcedureModelDiffer = nativeModelDiffer;
        }
#else
        public NativeMigrationsModelDiffer(
            IRelationalTypeMappingSource typeMappingSource,
            IMigrationsAnnotationProvider migrationsAnnotations,
            IChangeDetector changeDetector,
            IUpdateAdapterFactory updateAdapterFactory,
            ITriggerModelDiffer triggerModelDiffer,
            INativeStoredProcedureModelDiffer nativeModelDiffer,
            CommandBatchPreparerDependencies commandBatchPreparerDependencies)
            : base (typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, triggerModelDiffer, commandBatchPreparerDependencies)
        {
            _nativeStoredProcedureModelDiffer = nativeModelDiffer;
        }
#endif

        public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target)
        {
            var result = base.GetDifferences(source, target);
         
            result = _nativeStoredProcedureModelDiffer.AddNativeOperations(result, source, target);

            return result;
        }
    }
}
