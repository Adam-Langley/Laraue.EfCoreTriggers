﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

namespace Laraue.EfCoreTriggers.Common.Builders.Providers
{
    public abstract class BaseExpressionProvider
    {
        private readonly Dictionary<MemberInfo, string> _columnNamesCache = new Dictionary<MemberInfo, string>();

        private readonly Dictionary<Type, string> _tableNamesCache = new Dictionary<Type, string>();

        private readonly Dictionary<Type, string> _tableSchemasCache = new Dictionary<Type, string>();

        protected IModel Model { get; }

        protected virtual string NewEntityPrefix { get; } = "NEW";

        protected virtual string OldEntityPrefix { get; } = "OLD";

        protected virtual char Quote { get; } = '\'';

        public BaseExpressionProvider(IModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        protected virtual string GetColumnName(MemberInfo memberInfo)
        {
            if (!_columnNamesCache.ContainsKey(memberInfo))
            {
                var entityType = Model.FindEntityType(memberInfo.DeclaringType);
                _columnNamesCache.Add(memberInfo, entityType.GetProperty(memberInfo.Name).GetColumnName());
            }
            if (!_columnNamesCache.TryGetValue(memberInfo, out var columnName))
                throw new InvalidOperationException($"Column name for member {memberInfo.Name} is not defined in model");
            return columnName;
        }

        protected virtual string GetTableName(MemberInfo memberInfo) => GetTableName(memberInfo.DeclaringType);

        protected virtual string GetTableName(Type entity)
        {
            if (!_tableNamesCache.ContainsKey(entity))
            {
                var entityType = Model.FindEntityType(entity);
                _tableNamesCache.Add(entity, entityType.GetTableName());
            }
            if (!_tableNamesCache.TryGetValue(entity, out var columnName))
                throw new InvalidOperationException($"Table name for entity {entity.FullName} is not defined in model.");
            return columnName;
        }

        protected virtual string GetTableSchemaName(Type entity)
        {
            if (!_tableSchemasCache.ContainsKey(entity))
            {
                var entityType = Model.FindEntityType(entity);
                _tableSchemasCache.Add(entity, entityType.GetSchema());
            }
            if (!_tableSchemasCache.TryGetValue(entity, out var schemaName))
                throw new InvalidOperationException($"Schema for entity {entity.FullName} is not defined in model.");
            return schemaName;
        }

        protected virtual string GetExpressionTypeSql(ExpressionType expressionType) => expressionType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.IsTrue => "is true",
            ExpressionType.IsFalse => "is false",
            ExpressionType.Negate => "-",
            ExpressionType.Not => "is false",
            _ => throw new NotSupportedException($"Unknown sign of {expressionType}"),
        };

        protected virtual SqlBuilder GetExpressionSql(Expression expression, Dictionary<string, ArgumentType> argumentTypes)
        {
            return expression switch
            {
                BinaryExpression binaryExpression => GetBinaryExpressionSql(binaryExpression, argumentTypes),
                MemberExpression memberExpression => GetMemberExpressionSql(memberExpression, argumentTypes),
                UnaryExpression unaryExpression => GetUnaryExpressionSql(unaryExpression, argumentTypes),
                ConstantExpression constantExpression => GetConstantExpressionSql(constantExpression),
                MethodCallExpression methodCallExpression => GetMethodCallExpressionSql(methodCallExpression, argumentTypes),
                _ => throw new NotSupportedException($"Expression of type {expression.GetHashCode()} for {expression} is not supported."),
            };
        }


        protected virtual SqlBuilder GetMemberExpressionSql(MemberExpression memberExpression, Dictionary<string, ArgumentType> argumentTypes)
        {
            argumentTypes ??= new Dictionary<string, ArgumentType>();
            var parameterExpression = (ParameterExpression)memberExpression.Expression;
            var memberName = parameterExpression.Name;
            if (!argumentTypes.TryGetValue(memberName, out var argumentType))
                argumentType = ArgumentType.Default;
            return new SqlBuilder(memberExpression.Member, argumentType)
                .Append(GetMemberExpressionSql(memberExpression, argumentType));
        }

        protected virtual string GetMemberExpressionSql(MemberExpression memberExpression, ArgumentType argumentType)
        {
            return argumentType switch
            {
                ArgumentType.New => $"{NewEntityPrefix}.{GetColumnName(memberExpression.Member)}", 
                ArgumentType.Old => $"{OldEntityPrefix}.{GetColumnName(memberExpression.Member)}", 
                ArgumentType.None => GetColumnName(memberExpression.Member), 
                _ => $"{GetTableName(memberExpression.Member)}.{GetColumnName(memberExpression.Member)}",
            };
        }

        /// <summary>
        /// Get from <see cref="MemberAssignment"/> mapping <see cref="MemberInfo"/> -> assignment SQL.
        /// </summary>
        /// <param name="memberAssignment"></param>
        /// <param name="argumentTypes"></param>
        /// <returns></returns>
        protected virtual (MemberInfo MemberInfo, SqlBuilder AssignmentSqlResult) GetMemberAssignmentParts(
            MemberAssignment memberAssignment, Dictionary<string, ArgumentType> argumentTypes)
        {
            var sqlExtendedResult = GetExpressionSql(memberAssignment.Expression, argumentTypes);
            return (memberAssignment.Member, sqlExtendedResult);
        }

        protected virtual SqlBuilder GetMethodCallExpressionSql(MethodCallExpression methodCallExpression, Dictionary<string, ArgumentType> argumentTypes)
        {
            var parsedArguments = methodCallExpression.Arguments.Select(argumentExpression => GetExpressionSql(argumentExpression, argumentTypes)).ToArray();
            return methodCallExpression.Method.Name switch
            {
                "Concat" => GetMethodConcatCallExpressionSql(parsedArguments),
                "ToLower" => GetMethodToLowerCallExpressionSql(GetExpressionSql(methodCallExpression.Object, argumentTypes)),
                "ToUpper" => GetMethodToUpperCallExpressionSql(GetExpressionSql(methodCallExpression.Object, argumentTypes)),
                _ => throw new NotSupportedException($"Expression {methodCallExpression.Method.Name} is not supported"),
            };
        }

        protected virtual SqlBuilder GetMethodConcatCallExpressionSql(params SqlBuilder[] concatExpressionArgsSql)
            => new SqlBuilder(concatExpressionArgsSql)
                .AppendJoin(" + ", concatExpressionArgsSql.Select(x => x.StringBuilder));

        protected virtual SqlBuilder GetMethodToLowerCallExpressionSql(SqlBuilder lowerSqlExpression)
            => new SqlBuilder(lowerSqlExpression.AffectedColumns, $"LOWER({lowerSqlExpression})");

        protected virtual SqlBuilder GetMethodToUpperCallExpressionSql(SqlBuilder upperSqlExpression)
            => new SqlBuilder(upperSqlExpression.AffectedColumns, $"UPPER({upperSqlExpression})");

        protected virtual SqlBuilder GetUnaryExpressionSql(UnaryExpression unaryExpression, Dictionary<string, ArgumentType> argumentTypes)
        {
            var leftSideExpressionTypes = new[] { ExpressionType.Negate };
            var memberExpression = (MemberExpression)unaryExpression.Operand;
            var sqlBuilder = new SqlBuilder();
            if (leftSideExpressionTypes.Contains(unaryExpression.NodeType))
                sqlBuilder.Append(GetExpressionTypeSql(unaryExpression.NodeType));

            var memberExpressionSqlResult = GetMemberExpressionSql(memberExpression, argumentTypes);
            sqlBuilder.MergeColumnsInfo(memberExpressionSqlResult.AffectedColumns)
                .Append(memberExpressionSqlResult.StringBuilder);

            if (!leftSideExpressionTypes.Contains(unaryExpression.NodeType))
                sqlBuilder.Append($" {GetExpressionTypeSql(unaryExpression.NodeType)}");

            return sqlBuilder;
        }

        protected virtual SqlBuilder[] GetNewExpressionColumnsSql(NewExpression newExpression, Dictionary<string, ArgumentType> argumentTypes)
            => newExpression.Arguments.Select(argument => GetMemberExpressionSql((MemberExpression)argument, argumentTypes)).ToArray();

        protected virtual SqlBuilder GetBinaryExpressionSql(BinaryExpression binaryExpression, Dictionary<string, ArgumentType> argumentTypes)
        {
            Expression[] GetBinaryExpressionParts()
            {
                var parts = new[] { binaryExpression.Left, binaryExpression.Right };
                if (binaryExpression.Method is null)
                {
                    if (binaryExpression.Left is MemberExpression leftMemberExpression && leftMemberExpression.Type == typeof(bool))
                        parts[0] = Expression.IsTrue(binaryExpression.Left);
                    if (binaryExpression.Right is MemberExpression rightMemberExpression && rightMemberExpression.Type == typeof(bool))
                        parts[1] = Expression.IsTrue(binaryExpression.Right);
                }
                return parts;
            };

            if (binaryExpression.Method?.Name == "Concat")
                return GetMethodCallExpressionSql(Expression.Call(null, binaryExpression.Method, binaryExpression.Left, binaryExpression.Right), argumentTypes);
            else
            {
                var binaryParts = GetBinaryExpressionParts().Select(part => GetExpressionSql(part, argumentTypes));
                return new SqlBuilder(binaryParts)
                    .AppendJoin($" {GetExpressionTypeSql(binaryExpression.NodeType)} ", binaryParts.Select(x => x.StringBuilder));
            }
        }

        /// <summary>
        /// Get from <see cref="MemberInitExpression"/> mapping <see cref="MemberInfo"/> -> generated SQL.
        /// </summary>
        /// <param name="memberInitExpression"></param>
        /// <param name="argumentTypes"></param>
        /// <returns></returns>
        protected Dictionary<MemberInfo, SqlBuilder> GetMemberInitExpressionAssignmentParts(MemberInitExpression memberInitExpression, Dictionary<string, ArgumentType> argumentTypes)
        {
            return memberInitExpression.Bindings.Select(memberBinding =>
            {
                var memberAssignmentExpression = (MemberAssignment)memberBinding;
                return GetMemberAssignmentParts(memberAssignmentExpression, argumentTypes);
            }).ToDictionary(x => x.MemberInfo, x => x.AssignmentSqlResult);
        }

        protected virtual SqlBuilder GetConstantExpressionSql(ConstantExpression constantExpression)
        {
            if (constantExpression.Value is string strValue)
                return new SqlBuilder($"{Quote}{strValue}{Quote}");
            else if (constantExpression.Value is Enum enumValue)
                return new SqlBuilder(enumValue.ToString("D"));
            return new SqlBuilder(constantExpression.Value.ToString().ToLower());
        }
    }
}
