using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public class NativeStoredProcedureVisitor : INativeStoredProcedureVisitor
    {
        private readonly IDbSchemaRetriever _dbSchemaRetriever;
        private readonly ISqlGenerator _sqlGenerator;

        public NativeStoredProcedureVisitor(IDbSchemaRetriever dbSchemaRetriever, ISqlGenerator sqlGenerator)
        {
            _dbSchemaRetriever = dbSchemaRetriever;
            _sqlGenerator = sqlGenerator;
        }

        public string GenerateCreateNativeStoredProcedureSql(INativeStoredProcedure storedProcedure)
        {
            Debugger.Launch();
            return $"CREATE PROCEDURE {storedProcedure.Sql};";
        }

        public string GenerateDeleteNativeStoredProcedureSql(string storedProcedureName)//, IEntityType entityType)
        {
            //var tableSchemaPrefix = _sqlGenerator.GetSchemaPrefixSql(entityType.ClrType);

            //return $"DROP PROCEDURE {tableSchemaPrefix}{storedProcedureName};";
            return $"DROP PROCEDURE {storedProcedureName};";
        }
    }
}
