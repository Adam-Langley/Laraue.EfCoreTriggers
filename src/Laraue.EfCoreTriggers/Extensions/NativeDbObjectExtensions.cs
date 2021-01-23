using Laraue.EfCoreTriggers.Common;
using Laraue.EfCoreTriggers.Common.Builders.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Laraue.EfCoreTriggers.Extensions
{
    public static class NativeDbObjectExtensions
    {
        private static DbProvider _activeProvider;

        /// <summary>
        /// Bad solution, but have no idea yet, how to register current provider using DbContextOptionsBuilder.
        /// </summary>
        /// <param name="builder"></param>
        public static void RememberActiveDbProvider(DbContextOptionsBuilder builder)
        {
            _activeProvider = builder.GetActiveDbProvider();
        }

        public static INativeDbObjectSqlProvider GetSqlProvider(IModel model)
        {
            return _activeProvider switch
            {
                DbProvider.SqlServer => new SqlServerProvider(model),
                _ => throw new NotSupportedException($"Provider {_activeProvider} is not supported!"),
            };
        }

        public static int NativeAnnotationNameToSortOrder(string annotationName, string keyFormat)
            => int.Parse(Regex.Match(annotationName, "^(" + string.Format(keyFormat, "([0-9]+)") + ")(.+)")?.Groups[2]?.Value);

        public static string NativeAnnotationKeyToNativeObjectNamePattern(string annotationName, string keyFormat)
            => Regex.Match(annotationName, "^(" + string.Format(keyFormat, "[0-9]+") + ")(.+)")?.Groups[2]?.Value;

        public static string NativeAnnotationKeyToNativeObjectNamePattern(string annotationName, out string matchingKeyFormat)
        {
            matchingKeyFormat = Constants.NativeStoredProcedureAnnotationKey;
            var result = NativeAnnotationKeyToNativeObjectNamePattern(annotationName, matchingKeyFormat);
            if (null == result)
            {
                matchingKeyFormat = Constants.NativeTriggerAnnotationKey;
                result = NativeAnnotationKeyToNativeObjectNamePattern(annotationName, matchingKeyFormat);
            }
            if (null == result)
            {
                matchingKeyFormat = Constants.NativeUserDefinedFunctionAnnotationKey;
                result = NativeAnnotationKeyToNativeObjectNamePattern(annotationName, matchingKeyFormat);
            }
            if (null == result)
            {
                matchingKeyFormat = Constants.NativeUserDefinedTypeAnnotationKey;
                result = NativeAnnotationKeyToNativeObjectNamePattern(annotationName, matchingKeyFormat);
            }
            if (null == result)
            {
                matchingKeyFormat = Constants.NativeViewAnnotationKey;
                result = NativeAnnotationKeyToNativeObjectNamePattern(annotationName, matchingKeyFormat);
            }
            return result;
        }
    }
}
