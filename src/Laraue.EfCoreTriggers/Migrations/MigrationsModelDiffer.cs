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
            var oldUserDefinedFunctionAnnotationNames = sourceModel.GetUserDefinedFunctionAnnotations()
                .Select(x => x.Name);

            var newUserDefinedFunctionAnnotationNames = targetModel.GetUserDefinedFunctionAnnotations()
                .Select(x => x.Name);

            var commonUserDefinedFunctionAnnotationNames = oldUserDefinedFunctionAnnotationNames.Intersect(newUserDefinedFunctionAnnotationNames);

            // If user-defined Functions was changed, recreate it.
            foreach (var commonAnnotationName in commonUserDefinedFunctionAnnotationNames)
            {
                var oldValue = sourceModel.GetAnnotation(commonAnnotationName);
                var newValue = targetModel.GetAnnotation(commonAnnotationName);
                if ((string)oldValue.Value != (string)newValue.Value)
                {
                    deleteNativeObjectOperations.AddDeleteUserDefinedFunctionSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering);
                    createNativeObjectOperations.AddCreateUserDefinedFunctionSqlMigration(newValue, nativeObjectOperationOrdering);
                }
            }

            // If user-defined Functions was removed, delete it.
            foreach (var oldUserDefinedFunctionName in oldUserDefinedFunctionAnnotationNames.Except(commonUserDefinedFunctionAnnotationNames))
            {
                var oldUserDefinedFunctionAnnotation = sourceModel.GetAnnotation(oldUserDefinedFunctionName);
                deleteNativeObjectOperations.AddDeleteUserDefinedFunctionSqlMigration(oldUserDefinedFunctionAnnotation, sourceModel, nativeObjectOperationOrdering);
                var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(oldUserDefinedFunctionAnnotation.Name, Constants.NativeUserDefinedFunctionAnnotationKey);
                nativeObjectOperationOrdering[deleteNativeObjectOperations.Last()] = order;
            }

            // If user-defined Functions was added, create it.
            foreach (var newUserDefinedFunctionName in newUserDefinedFunctionAnnotationNames.Except(commonUserDefinedFunctionAnnotationNames))
            {
                var newUserDefinedFunctionAnnotation = targetModel.GetAnnotation(newUserDefinedFunctionName);
                createNativeObjectOperations.AddCreateUserDefinedFunctionSqlMigration(newUserDefinedFunctionAnnotation, nativeObjectOperationOrdering);
            }

            /* native objects */
            //var oldNativeObjectAnnotationNames = sourceModel.GetNativeObjectAnnotations()
            //    .Select(x => RawDbObjectExtensions.NativeAnnotationKeyToNativeObjectNamePattern(x.Name));

            //var newNativeObjectAnnotationNames = targetModel.GetNativeObjectAnnotations()
            //    .Select(x => RawDbObjectExtensions.NativeAnnotationKeyToNativeObjectNamePattern(x.Name))
            //    .OrderBy;

            //var commonUserDefinedTypeAnnotationNames = oldNativeObjectAnnotationNames.Intersect(newNativeObjectAnnotationNames);

            //// If user-defined type was changed, recreate it.
            //foreach (var commonAnnotationName in commonUserDefinedTypeAnnotationNames)
            //{
            //    var oldValue = sourceModel.GetAnnotation(commonAnnotationName);
            //    var newValue = targetModel.GetAnnotation(commonAnnotationName);
            //    if ((string)oldValue.Value != (string)newValue.Value)
            //    {
            //        deleteNativeObjectOperations.AddDeleteUserDefinedTypeSqlMigration(oldValue, sourceModel);
            //        createNativeObjectOperations.AddCreateUserDefinedTypeSqlMigration(newValue);
            //    }
            //}

            //// If user-defined type was removed, delete it.
            //foreach (var oldUserDefinedTypeName in oldUserDefinedTypeAnnotationNames.Except(commonUserDefinedTypeAnnotationNames))
            //{
            //    var oldUserDefinedTypeAnnotation = sourceModel.GetAnnotation(oldUserDefinedTypeName);
            //    deleteNativeObjectOperations.AddDeleteUserDefinedTypeSqlMigration(oldUserDefinedTypeAnnotation, sourceModel);
            //}

            //// If user-defined type was added, create it.
            //foreach (var newUserDefinedTypeName in newUserDefinedTypeAnnotationNames.Except(commonUserDefinedTypeAnnotationNames))
            //{
            //    var newUserDefinedTypeAnnotation = targetModel.GetAnnotation(newUserDefinedTypeName);
            //    createNativeObjectOperations.AddCreateUserDefinedTypeSqlMigration(newUserDefinedTypeAnnotation);
            //}
            /* native objects */



            // User Defined Types
            var oldUserDefinedTypeAnnotationNames = sourceModel.GetUserDefinedTypeAnnotations()
                .Select(x => x.Name);

            var newUserDefinedTypeAnnotationNames = targetModel.GetUserDefinedTypeAnnotations()
                .Select(x => x.Name);

            var commonUserDefinedTypeAnnotationNames = oldUserDefinedTypeAnnotationNames.Intersect(newUserDefinedTypeAnnotationNames);

            // If user-defined type was changed, recreate it.
            foreach (var commonAnnotationName in commonUserDefinedTypeAnnotationNames)
            {
                var oldValue = sourceModel.GetAnnotation(commonAnnotationName);
                var newValue = targetModel.GetAnnotation(commonAnnotationName);
                if ((string)oldValue.Value != (string)newValue.Value)
                {
                    deleteNativeObjectOperations.AddDeleteUserDefinedTypeSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering);
                    createNativeObjectOperations.AddCreateUserDefinedTypeSqlMigration(newValue, nativeObjectOperationOrdering);
                }
            }

            // If user-defined type was removed, delete it.
            foreach (var oldUserDefinedTypeName in oldUserDefinedTypeAnnotationNames.Except(commonUserDefinedTypeAnnotationNames))
            {
                var oldUserDefinedTypeAnnotation = sourceModel.GetAnnotation(oldUserDefinedTypeName);
                deleteNativeObjectOperations.AddDeleteUserDefinedTypeSqlMigration(oldUserDefinedTypeAnnotation, sourceModel, nativeObjectOperationOrdering);
            }

            // If user-defined type was added, create it.
            foreach (var newUserDefinedTypeName in newUserDefinedTypeAnnotationNames.Except(commonUserDefinedTypeAnnotationNames))
            {
                var newUserDefinedTypeAnnotation = targetModel.GetAnnotation(newUserDefinedTypeName);
                createNativeObjectOperations.AddCreateUserDefinedTypeSqlMigration(newUserDefinedTypeAnnotation, nativeObjectOperationOrdering);
            }

            // Views
            var oldViewAnnotationNames = sourceModel.GetViewAnnotations()
                .Select(x => x.Name);

            var newViewAnnotationNames = targetModel.GetViewAnnotations()
                .Select(x => x.Name);

            var commonViewAnnotationNames = oldViewAnnotationNames.Intersect(newViewAnnotationNames);

            // If View was changed, recreate it.
            foreach (var commonAnnotationName in commonViewAnnotationNames)
            {
                var oldValue = sourceModel.GetAnnotation(commonAnnotationName);
                var newValue = targetModel.GetAnnotation(commonAnnotationName);
                if ((string)oldValue.Value != (string)newValue.Value)
                {
                    deleteNativeObjectOperations.AddDeleteViewSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering);
                    createNativeObjectOperations.AddCreateViewSqlMigration(newValue, nativeObjectOperationOrdering);
                }
            }

            // If View was removed, delete it.
            foreach (var oldViewName in oldViewAnnotationNames.Except(commonViewAnnotationNames))
            {
                var oldViewAnnotation = sourceModel.GetAnnotation(oldViewName);
                deleteNativeObjectOperations.AddDeleteViewSqlMigration(oldViewAnnotation, sourceModel, nativeObjectOperationOrdering);
            }

            // If View was added, create it.
            foreach (var newViewName in newViewAnnotationNames.Except(commonViewAnnotationNames))
            {
                var newViewAnnotation = targetModel.GetAnnotation(newViewName);
                createNativeObjectOperations.AddCreateViewSqlMigration(newViewAnnotation, nativeObjectOperationOrdering);
            }

            // Stored Procedures
            var oldStoredProcedureAnnotationNames = sourceModel.GetStoredProcedureAnnotations()
                .Select(x => x.Name);

            var newStoredProcedureAnnotationNames = targetModel.GetStoredProcedureAnnotations()
                .Select(x => x.Name);

            var commonStoredProcedureAnnotationNames = oldStoredProcedureAnnotationNames.Intersect(newStoredProcedureAnnotationNames);

            // If stored procedure was changed, recreate it.
            foreach (var commonAnnotationName in commonStoredProcedureAnnotationNames)
            {
                var oldValue = sourceModel.GetAnnotation(commonAnnotationName);
                var newValue = targetModel.GetAnnotation(commonAnnotationName);
                if ((string)oldValue.Value != (string)newValue.Value)
                {
                    deleteNativeObjectOperations.AddDeleteStoredProcedureSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering);
                    createNativeObjectOperations.AddCreateStoredProcedureSqlMigration(newValue, nativeObjectOperationOrdering);
                }
            }

            // If stored procedure was removed, delete it.
            foreach (var oldStoredProcedureName in oldStoredProcedureAnnotationNames.Except(commonStoredProcedureAnnotationNames))
            {
                var oldStoredProcedureAnnotation = sourceModel.GetAnnotation(oldStoredProcedureName);
                deleteNativeObjectOperations.AddDeleteStoredProcedureSqlMigration(oldStoredProcedureAnnotation, sourceModel, nativeObjectOperationOrdering);
            }

            // If stored procedure was added, create it.
            foreach (var newStoredProcedureName in newStoredProcedureAnnotationNames.Except(commonStoredProcedureAnnotationNames))
            {
                var newStoredProcedureAnnotation = targetModel.GetAnnotation(newStoredProcedureName);
                createNativeObjectOperations.AddCreateStoredProcedureSqlMigration(newStoredProcedureAnnotation, nativeObjectOperationOrdering);
            }

            createNativeObjectOperations = createNativeObjectOperations.OrderBy(x => nativeObjectOperationOrdering[x]).ToList();
            deleteNativeObjectOperations = deleteNativeObjectOperations.OrderByDescending(x => nativeObjectOperationOrdering[x]).ToList();

            return MergeOperations(base.GetDifferences(source, target), createTriggerOperations, deleteTriggerOperations, createNativeObjectOperations, deleteNativeObjectOperations);
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