using Laraue.EfCoreTriggers.Common.Builders.Native.Trigger.Base;
using Laraue.EfCoreTriggers.Common.Builders.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.Trigger
{
    public class NativeTriggerTypeBuilder<TTriggerEntity> : NativeTypeBuilder, ISqlConvertible
    {
        public NativeTriggerTypeBuilder(string name, string rawScript, int order) : base(Constants.NativeTriggerAnnotationKey, "TRIGGER_NAME", name, rawScript, order)
        {
        }

        public virtual SqlBuilder BuildSql(INativeDbObjectSqlProvider visitor) => visitor.GetNativeTriggerSql(this);
    }
}
