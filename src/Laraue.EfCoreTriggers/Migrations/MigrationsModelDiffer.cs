using Laraue.EfCoreTriggers.Common;
using Laraue.EfCoreTriggers.Extensions;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Laraue.EfCoreTriggers.Migrations
{
    public class MigrationsModelDiffer : Microsoft.EntityFrameworkCore.Migrations.Internal.MigrationsModelDiffer
    {
        public MigrationsModelDiffer(
            IRelationalTypeMappingSource typeMappingSource,
            IMigrationsAnnotationProvider migrationsAnnotations,
            IChangeDetector changeDetector,
            IUpdateAdapterFactory updateAdapterFactory,
            CommandBatchPreparerDependencies commandBatchPreparerDependencies)
                : base (typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies)
        {
        }

        public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel source, IRelationalModel target)
        {
            var nativeObjectOperationOrdering = new Dictionary<SqlOperation, int>();

            var deleteNativeObjectOperations = new List<SqlOperation>();
            var createNativeObjectOperations = new List<SqlOperation>();

            var deleteTriggerOperations = new List<SqlOperation>();
            var createTriggerOperations = new List<SqlOperation>();

            var sourceModel = source?.Model;
            var targetModel = target?.Model;

            var oldEntityTypeNames = sourceModel?.GetEntityTypes().Select(x => x.Name) ?? Enumerable.Empty<string>();
            var newEntityTypeNames = targetModel?.GetEntityTypes().Select(x => x.Name) ?? Enumerable.Empty<string>();

            var commonEntityTypeNames = oldEntityTypeNames.Intersect(newEntityTypeNames);

            // Drop all triggers for deleted entities.
            foreach (var deletedTypeName in oldEntityTypeNames.Except(commonEntityTypeNames))
            {
                var deletedEntityType = source.Model.FindEntityType(deletedTypeName);
                foreach (var annotation in deletedEntityType.GetTriggerAnnotations())
                {
                    if (annotation.Name.StartsWith(Constants.TriggerAnnotationKey))
                        deleteTriggerOperations.AddDeleteTriggerSqlMigration(annotation, sourceModel);
                    else
                        deleteTriggerOperations.AddDeleteNativeTriggerSqlMigration(annotation, sourceModel, nativeObjectOperationOrdering);
                }
            }

            // Add all triggers to created entities.
            foreach (var newTypeName in newEntityTypeNames.Except(commonEntityTypeNames))
            {
                foreach (var annotation in targetModel.FindEntityType(newTypeName).GetTriggerAnnotations())
                    createTriggerOperations.AddCreateTriggerSqlMigration(annotation);
            }

            // For existing entities.
            foreach (var entityTypeName in commonEntityTypeNames)
            {
                var oldEntityType = sourceModel.FindEntityType(entityTypeName);
                var newEntityType = targetModel.FindEntityType(entityTypeName);

                var oldAnnotationNames = sourceModel.FindEntityType(entityTypeName)
                    .GetTriggerAnnotations()
                    .Select(x => x.Name);

                var newAnnotationNames = targetModel.FindEntityType(entityTypeName)
                    .GetTriggerAnnotations()
                    .Select(x => x.Name);

                var commonAnnotationNames = oldAnnotationNames.Intersect(newAnnotationNames);

                // If trigger was changed, recreate it.
                foreach (var commonAnnotationName in commonAnnotationNames)
                {
                    var oldValue = sourceModel.FindEntityType(entityTypeName).GetAnnotation(commonAnnotationName);
                    var newValue = targetModel.FindEntityType(entityTypeName).GetAnnotation(commonAnnotationName);
                    if ((string)oldValue.Value != (string)newValue.Value)
                    {
                        if (oldValue.Name.StartsWith(Constants.TriggerAnnotationKey))
                        {
                            deleteTriggerOperations.AddDeleteTriggerSqlMigration(oldValue, sourceModel);
                            createTriggerOperations.AddCreateTriggerSqlMigration(newValue);
                        }
                        else
                        {
                            deleteTriggerOperations.AddDeleteNativeTriggerSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering);
                            createTriggerOperations.AddCreateNativeTriggerSqlMigration(newValue);
                        }
                    }
                }

                // If trigger was removed, delete it.
                foreach (var oldTriggerName in oldAnnotationNames.Except(commonAnnotationNames))
                {
                    var oldTriggerAnnotation = oldEntityType.GetAnnotation(oldTriggerName);
                    if (oldTriggerAnnotation.Name.StartsWith(Constants.TriggerAnnotationKey))
                        deleteTriggerOperations.AddDeleteTriggerSqlMigration(oldTriggerAnnotation, sourceModel); 
                    else
                        deleteTriggerOperations.AddDeleteNativeTriggerSqlMigration(oldTriggerAnnotation, sourceModel, nativeObjectOperationOrdering);
                }

                // If trigger was added, create it.
                foreach (var newTriggerName in newAnnotationNames.Except(commonAnnotationNames))
                {
                    var newTriggerAnnotation = newEntityType.GetAnnotation(newTriggerName);
                    if (newTriggerAnnotation.Name.StartsWith(Constants.TriggerAnnotationKey))
                        createTriggerOperations.AddCreateTriggerSqlMigration(newTriggerAnnotation);
                    else
                        createTriggerOperations.AddCreateNativeTriggerSqlMigration(newTriggerAnnotation);
                }
            }

             // User Defined Functions
            SynchronizeModels(
                sourceModel,
                targetModel,
                x => x.GetUserDefinedFunctionAnnotations(),
                (oldValue) => {
                    deleteNativeObjectOperations.AddDeleteUserDefinedFunctionSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering);
                    var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(oldValue.Name, Constants.NativeUserDefinedFunctionAnnotationKey);
                    nativeObjectOperationOrdering[deleteNativeObjectOperations.Last()] = order;
                },
                (newValue) => createNativeObjectOperations.AddCreateUserDefinedFunctionSqlMigration(newValue, nativeObjectOperationOrdering));

            // User Defined Types
            SynchronizeModels(
                sourceModel,
                targetModel,
                x => x.GetUserDefinedTypeAnnotations(),
                (oldValue) => deleteNativeObjectOperations.AddDeleteUserDefinedTypeSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering),
                (newValue) => createNativeObjectOperations.AddCreateUserDefinedTypeSqlMigration(newValue, nativeObjectOperationOrdering));

            // Views
            SynchronizeModels(
                sourceModel,
                targetModel,
                x => x.GetViewAnnotations(),
                (oldValue) => deleteNativeObjectOperations.AddDeleteViewSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering),
                (newValue) => createNativeObjectOperations.AddCreateViewSqlMigration(newValue, nativeObjectOperationOrdering));

            SynchronizeModels(
                sourceModel,
                targetModel,
                x => x.GetStoredProcedureAnnotations(),
                (oldValue) => deleteNativeObjectOperations.AddDeleteStoredProcedureSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering),
                (newValue) => createNativeObjectOperations.AddCreateStoredProcedureSqlMigration(newValue, nativeObjectOperationOrdering));

            createNativeObjectOperations = createNativeObjectOperations.OrderBy(x => nativeObjectOperationOrdering[x]).ToList();
            deleteNativeObjectOperations = deleteNativeObjectOperations.OrderByDescending(x => nativeObjectOperationOrdering[x]).ToList();

            return MergeOperations(base.GetDifferences(source, target), createTriggerOperations, deleteTriggerOperations, createNativeObjectOperations, deleteNativeObjectOperations);
        }

        void SynchronizeModels(IModel sourceModel, IModel targetModel, Func<IModel, IEnumerable<IAnnotation>> annotationGetter, Action<IAnnotation> deletor, Action<IAnnotation> creator)
        {
            // Stored Procedures
            var previousAnnotationNames = annotationGetter(sourceModel)
                .Select(x => x.Name);

            var currentAnnotationNames = annotationGetter(targetModel)
                .Select(x => x.Name);

            var commonAnnotationNames = previousAnnotationNames.Join(currentAnnotationNames, x => RemoveOrderComponent(x), y => RemoveOrderComponent(y), (Previous, Current) => new { Previous, Current });

            // If stored procedure was changed, recreate it.
            foreach (var commonAnnotationName in commonAnnotationNames)
            {
                var oldValue = sourceModel.GetAnnotation(commonAnnotationName.Previous);
                var newValue = targetModel.GetAnnotation(commonAnnotationName.Current);
                if (!object.Equals(oldValue.Value, newValue.Value))
                {
                    deletor(oldValue);
                    creator(newValue);
                }
            }

            // If stored procedure was removed, delete it.
            foreach (var previousAnnotationName in previousAnnotationNames.Except(commonAnnotationNames.Select(x => x.Previous)))
            {
                var previousAnnotation = sourceModel.GetAnnotation(previousAnnotationName);
                deletor(previousAnnotation);
            }

            // If stored procedure was added, create it.
            foreach (var currentAnnotationName in currentAnnotationNames.Except(commonAnnotationNames.Select(x => x.Current)))
            {
                var currentAnnotation = targetModel.GetAnnotation(currentAnnotationName);
                creator(currentAnnotation);
            }
        }

        private static string RemoveOrderComponent(string name)
        {
            return Regex.Replace(name, "(LC_(SPROC|FUNC|TYPE|VIEW)_)([0-9]+_)", x => x.Groups[1].Value);
        }

        private IReadOnlyList<MigrationOperation> MergeOperations(
            IEnumerable<MigrationOperation> migrationOperations,
            IEnumerable<MigrationOperation> createTriggersOperations,
            IEnumerable<MigrationOperation> deleteTriggersOperation,
            IEnumerable<MigrationOperation> createNativeObjectOperations,
            IEnumerable<MigrationOperation> deleteNativeObjectOperations)
        {
            return new List<MigrationOperation>(deleteTriggersOperation)
                .Concat(deleteNativeObjectOperations)
                .Concat(migrationOperations)
                .Concat(createNativeObjectOperations)
                .Concat(createTriggersOperations)
                .ToList();
        }
    }

    public static class IListExtensions
    {
        public static IEnumerable<IAnnotation> GetTriggerAnnotations(this IEntityType entityType)
        {
            return entityType.GetAnnotations()
                .Where(x => x.Name.StartsWith(Constants.TriggerAnnotationKey))
                .Union(GetNativeTriggerAnnotations(entityType));
        }

        public static IList<SqlOperation> AddCreateTriggerSqlMigration(this IList<SqlOperation> list, IAnnotation annotation)
        {
            var triggerSql = annotation.Value as string;

            list.Add(new SqlOperation 
            {
                Sql = triggerSql,
            });
            return list;
        }

        public static IList<SqlOperation> AddDeleteTriggerSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IModel model)
        {
            list.Add(new SqlOperation
            {
                Sql = TriggerExtensions.GetSqlProvider(model).GetDropTriggerSql(annotation.Name),
            });
            return list;
        }

        private static IEnumerable<IAnnotation> GetNativeAnnotations(string nativeAnnotationKeyFormat, IAnnotatable entityType)
        {
            if (null == entityType)
                return Enumerable.Empty<IAnnotation>();

            return entityType.GetAnnotations()
                .Where(x => Regex.IsMatch(x.Name, "^(" + string.Format(nativeAnnotationKeyFormat, "[0-9]+") + ").+"))
                .OrderBy(x => int.Parse(Regex.Match(x.Name, "^(" + string.Format(nativeAnnotationKeyFormat, "([0-9]+)") + ").+").Groups[2].Value));
        }

        public static IEnumerable<IAnnotation> GetNativeTriggerAnnotations(this IAnnotatable entityType)
        => GetNativeAnnotations(Constants.NativeTriggerAnnotationKey, entityType);

        public static IEnumerable<IAnnotation> GetStoredProcedureAnnotations(this IAnnotatable entityType)
        => GetNativeAnnotations(Constants.NativeStoredProcedureAnnotationKey, entityType);

        public static IEnumerable<IAnnotation> GetUserDefinedTypeAnnotations(this IAnnotatable entityType)
            => GetNativeAnnotations(Constants.NativeUserDefinedTypeAnnotationKey, entityType);

        public static IEnumerable<IAnnotation> GetUserDefinedFunctionAnnotations(this IAnnotatable entityType)
            => GetNativeAnnotations(Constants.NativeUserDefinedFunctionAnnotationKey, entityType);

        public static IEnumerable<IAnnotation> GetViewAnnotations(this IAnnotatable entityType)
        => GetNativeAnnotations(Constants.NativeViewAnnotationKey, entityType);

        public static IEnumerable<IAnnotation> GetNativeObjectAnnotations(this IAnnotatable entityType)
        => GetNativeTriggerAnnotations(entityType)
            .Union(GetStoredProcedureAnnotations(entityType))
            .Union(GetUserDefinedTypeAnnotations(entityType))
            .Union(GetUserDefinedFunctionAnnotations(entityType))
            .Union(GetViewAnnotations(entityType));

        public static IList<SqlOperation> AddCreateStoredProcedureSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IDictionary<SqlOperation, int> ordering)
        {
            var storedProcedureSql = annotation.Value as string;

            var op = new SqlOperation
            {
                Sql = storedProcedureSql,
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeStoredProcedureAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddDeleteStoredProcedureSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IModel model, IDictionary<SqlOperation, int> ordering)
        {
            var op = new SqlOperation
            {
                Sql = NativeDbObjectExtensions.GetSqlProvider(model).GetDropStoredProcedureSql(annotation.Name),
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeStoredProcedureAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddCreateUserDefinedTypeSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IDictionary<SqlOperation, int> ordering)
        {
            var userDefinedTypeSql = annotation.Value as string;

            var op = new SqlOperation
            {
                Sql = userDefinedTypeSql,
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeUserDefinedTypeAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddDeleteUserDefinedTypeSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IModel model, IDictionary<SqlOperation, int> ordering)
        {
            var op = new SqlOperation
            {
                Sql = NativeDbObjectExtensions.GetSqlProvider(model).GetDropUserDefinedTypeSql(annotation.Name),
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeUserDefinedTypeAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddCreateUserDefinedFunctionSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IDictionary<SqlOperation, int> ordering)
        {
            var userDefinedFunctionSql = annotation.Value as string;

            var op = new SqlOperation
            {
                Sql = userDefinedFunctionSql,
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeUserDefinedFunctionAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddDeleteUserDefinedFunctionSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IModel model, IDictionary<SqlOperation, int> ordering)
        {
            var op = new SqlOperation
            {
                Sql = NativeDbObjectExtensions.GetSqlProvider(model).GetDropUserDefinedFunctionSql(annotation.Name),
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeUserDefinedFunctionAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddCreateViewSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IDictionary<SqlOperation, int> ordering)
        {
            var viewSql = annotation.Value as string;

            var op = new SqlOperation
            {
                Sql = viewSql,
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeViewAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddDeleteViewSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IModel model, IDictionary<SqlOperation, int> ordering)
        {
            var op = new SqlOperation
            {
                Sql = NativeDbObjectExtensions.GetSqlProvider(model).GetDropViewSql(annotation.Name),
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeViewAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddCreateNativeTriggerSqlMigration(this IList<SqlOperation> list, IAnnotation annotation)
        {
            var viewSql = annotation.Value as string;

            var op = new SqlOperation
            {
                Sql = viewSql,
            };
            list.Add(op);
            return list;
        }

        public static IList<SqlOperation> AddDeleteNativeTriggerSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IModel model, IDictionary<SqlOperation, int> ordering)
        {
            var op = new SqlOperation
            {
                Sql = NativeDbObjectExtensions.GetSqlProvider(model).GetDropNativeTriggerSql(annotation.Name),
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeTriggerAnnotationKey);
            ordering[op] = order;
            return list;
        }
    }
}
