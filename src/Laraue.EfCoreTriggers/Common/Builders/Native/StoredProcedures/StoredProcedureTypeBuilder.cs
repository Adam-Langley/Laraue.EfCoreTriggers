using Laraue.EfCoreTriggers.Common.Builders.Providers;
using Laraue.EfCoreTriggers.Common.Builders.Native.StoredProcedures.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.StoredProcedures
{
    public class StoredProcedureTypeBuilder : NativeTypeBuilder, ISqlConvertible
    {
        public StoredProcedureTypeBuilder(string name, string rawScript, int order) : base(Constants.NativeStoredProcedureAnnotationKey, "PROCEDURE_NAME", name, rawScript, order)
        {
        }

        public virtual SqlBuilder BuildSql(INativeDbObjectSqlProvider visitor) => visitor.GetStoredProcedureSql(this);

        public StoredProcedureTypeBuilder Templated(IDictionary<string, string> tokens)
        {
            SetTokens(tokens);
            return this;
        }
    }
}
