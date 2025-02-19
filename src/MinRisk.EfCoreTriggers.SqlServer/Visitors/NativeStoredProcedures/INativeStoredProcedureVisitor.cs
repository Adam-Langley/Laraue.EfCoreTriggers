using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public interface INativeStoredProcedureVisitor
    {
        /// <summary>
        /// Generates create trigger SQL.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        string GenerateCreateNativeStoredProcedureSql(INativeStoredProcedure storedProcedure);

        /// <summary>
        /// Generates drop trigger SQL.
        /// </summary>
        /// <param name="triggerName"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        string GenerateDeleteNativeStoredProcedureSql(string storedProcedureName);//, IEntityType entityType);
    }
}
