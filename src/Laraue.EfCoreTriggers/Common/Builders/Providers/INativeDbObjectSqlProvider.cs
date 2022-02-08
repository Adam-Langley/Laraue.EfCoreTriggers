using Laraue.EfCoreTriggers.Common.Builders.Native.Trigger;
using Laraue.EfCoreTriggers.Common.Builders.Native.StoredProcedures;
using Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedFunctions;
using Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedTypes;
using Laraue.EfCoreTriggers.Common.Builders.Native.Views;
using Laraue.EfCoreTriggers.Common.Builders.Triggers.Base;
using Laraue.EfCoreTriggers.Common.Builders.Native.Indexes;

namespace Laraue.EfCoreTriggers.Common.Builders.Providers
{
    public interface INativeDbObjectSqlProvider
    {
        SqlBuilder GetStoredProcedureSql(StoredProcedureTypeBuilder storedProcedure);
        string GetDropStoredProcedureSql(string storedProcedureName);

        SqlBuilder GetUserDefinedTypeSql(UserDefinedTypeTypeBuilder userDefinedType);
        string GetDropUserDefinedTypeSql(string userDefinedTypeName);

        SqlBuilder GetUserDefinedFunctionSql(UserDefinedFunctionTypeBuilder userDefinedFunction);
        string GetDropUserDefinedFunctionSql(string userDefinedFunctionName);

        SqlBuilder GetViewSql(ViewTypeBuilder viewTypeBuilder);
        string GetDropViewSql(string viewName);

        SqlBuilder GetNativeTriggerSql<TTriggerEntity>(NativeTriggerTypeBuilder<TTriggerEntity> viewTypeBuilder);
        string GetDropNativeTriggerSql(string viewName);
        SqlBuilder GetIndexSql(IndexTypeBuilder indexTypeBuilder);
        string GetDropIndexSql(string indexName);
    }
}
