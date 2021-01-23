using Laraue.EfCoreTriggers.Common.Builders.Providers;
using Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedTypes.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedTypes
{
    public class UserDefinedTypeTypeBuilder : NativeTypeBuilder, ISqlConvertible
    {
        public UserDefinedTypeTypeBuilder(string name, string rawScript, int order) : base(Constants.NativeUserDefinedTypeAnnotationKey, "TYPE_NAME", name, rawScript, order)
        {
        }

        public virtual SqlBuilder BuildSql(INativeDbObjectSqlProvider visitor) => visitor.GetUserDefinedTypeSql(this);
    }
}
