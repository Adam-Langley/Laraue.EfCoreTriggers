using Laraue.EfCoreTriggers.Common.Builders.Providers;
using Laraue.EfCoreTriggers.Common.Builders.Native.Views.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.Views
{
    public class ViewTypeBuilder : NativeTypeBuilder, ISqlConvertible
    {
        public ViewTypeBuilder(string name, string rawScript, int order) : base(Constants.NativeViewAnnotationKey, "VIEW_NAME", name, rawScript, order)
        {
        }

        public virtual SqlBuilder BuildSql(INativeDbObjectSqlProvider visitor) => visitor.GetViewSql(this);
    }
}
