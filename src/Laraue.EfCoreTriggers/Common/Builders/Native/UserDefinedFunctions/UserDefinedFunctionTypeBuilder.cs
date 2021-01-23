using Laraue.EfCoreTriggers.Common.Builders.Providers;
using Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedFunctions.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedFunctions
{
    public class UserDefinedFunctionTypeBuilder : NativeTypeBuilder, ISqlConvertible
    {
        public UserDefinedFunctionTypeBuilder(string name, string rawScript, int order) : base(Constants.NativeUserDefinedFunctionAnnotationKey, "FUNCTION_NAME", name, rawScript, order)
        {
        }

        public virtual SqlBuilder BuildSql(INativeDbObjectSqlProvider visitor) => visitor.GetUserDefinedFunctionSql(this);

        public UserDefinedFunctionTypeBuilder Templated(IDictionary<string, string> tokens)
        {
            SetTokens(tokens);
            return this;
        }
    }
}
