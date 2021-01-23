﻿using Laraue.EfCoreTriggers.Common.Builders.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedTypes.Base
{
    internal interface ISqlConvertible
    {
        SqlBuilder BuildSql(INativeDbObjectSqlProvider provider);
    }
}
