using Laraue.EfCoreTriggers.Common.Builders.Providers;
using Laraue.EfCoreTriggers.Common.Builders.Native.Views.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.Indexes
{
    public class IndexTypeBuilder : NativeTypeBuilder, ISqlConvertible
    {
        public IndexTypeBuilder(string name, string rawScript, int order) : base(Constants.NativeViewAnnotationKey, "INDEX_NAME", name, rawScript, order)
        {
        }

        public virtual SqlBuilder BuildSql(INativeDbObjectSqlProvider visitor) => visitor.GetIndexSql(this);
    }
}
