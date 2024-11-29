using Laraue.EfCoreTriggers.Common.Builders.Providers;
using Laraue.EfCoreTriggers.Common.Builders.Native.Views.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.Indexes
{
    public class NativeIndexTypeBuilder<TTriggerEntity> : NativeTypeBuilder, ISqlConvertible
    {
        public NativeIndexTypeBuilder(string name, string rawScript, int order) : base(Constants.NativeIndexAnnotationKey, "INDEX_NAME", name, rawScript, order)
        {
        }

        public virtual SqlBuilder BuildSql(INativeDbObjectSqlProvider visitor) => visitor.GetNativeIndexSql(this);
    }
}
