using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MinRisk.EfCoreTriggers.Extensions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public class NativeStoredProcedureModelDiffer : INativeStoredProcedureModelDiffer
    {
        private readonly INativeStoredProcedureVisitor _storedProcedureVisitor;

        /// <summary>
        /// Initializes a new instance of <see cref="NativeStoredProcedureModelDiffer"/>.
        /// </summary>
        /// <param name="triggerVisitor"></param>
        public NativeStoredProcedureModelDiffer(INativeStoredProcedureVisitor storedProcedureVisitor)
        {
            _storedProcedureVisitor = storedProcedureVisitor;
        }

        public IReadOnlyList<MigrationOperation> AddNativeOperations(IEnumerable<MigrationOperation> operations, IRelationalModel? source, IRelationalModel? target)
        {
            var nativeObjectOperationOrdering = new Dictionary<SqlOperation, int>();

            var deleteNativeObjectOperations = new List<SqlOperation>();
            var createNativeObjectOperations = new List<SqlOperation>();

            var sourceModel = source?.Model;
            var targetModel = target?.Model;

            //SynchronizeModels(
            //    sourceModel,
            //    targetModel,
            //    x => x.GetStoredProcedureAnnotations(),
            //    (oldValue) => deleteNativeObjectOperations.AddDeleteStoredProcedureSqlMigration(oldValue, sourceModel, nativeObjectOperationOrdering),
            //    (newValue) => createNativeObjectOperations.AddCreateStoredProcedureSqlMigration(newValue, nativeObjectOperationOrdering));

            SynchronizeModels(
                sourceModel,
                targetModel,
                x => x.GetStoredProcedureAnnotations(),
                (oldValue) => _storedProcedureVisitor.AddDeleteNativeStoredProcedureSqlMigration(deleteNativeObjectOperations, oldValue, sourceModel, nativeObjectOperationOrdering),
                (newValue) => createNativeObjectOperations.AddCreateNativeStoredProcedureSqlMigration(newValue, nativeObjectOperationOrdering));

            createNativeObjectOperations = createNativeObjectOperations.OrderBy(x => nativeObjectOperationOrdering[x]).ToList();
            deleteNativeObjectOperations = deleteNativeObjectOperations.OrderByDescending(x => nativeObjectOperationOrdering[x]).ToList();

            return MergeOperations(operations, createNativeObjectOperations, deleteNativeObjectOperations);

        }

        private IReadOnlyList<MigrationOperation> MergeOperations(
            IEnumerable<MigrationOperation> migrationOperations,
            IEnumerable<MigrationOperation> createTriggersOperations,
            IEnumerable<MigrationOperation> deleteTriggersOperation)
        {
            return new List<MigrationOperation>(deleteTriggersOperation)
                .Concat(migrationOperations)
                .Concat(createTriggersOperations)
                .ToList();
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
    }

    /// <summary>
    /// Extensions to create migrations for EF Core triggers.
    /// </summary>
    public static class MigrationsExtensions
    {
#if NET6_0_OR_GREATER
        private static readonly FieldInfo AnnotationsField = typeof(AnnotatableBase)
            .GetField("_annotations", BindingFlags.Instance | BindingFlags.NonPublic)!;
#else
        private static readonly FieldInfo AnnotationsField = typeof(Annotatable)
            .GetField("_annotations", BindingFlags.Instance | BindingFlags.NonPublic)!;
#endif


        /// <summary>
        /// Convert all not translated annotations of <see cref="Microsoft.EntityFrameworkCore.Metadata.ITrigger"/> type to SQL.
        /// </summary>
        /// <param name="storedProcedureVisitor"></param>
        /// <param name="model"></param>
        public static void ConvertNativeStoredProcedureAnnotationsToSql(this INativeStoredProcedureVisitor storedProcedureVisitor, IModel? model)
        {
            //foreach (var entityType in model?.GetEntityTypes() ?? Enumerable.Empty<IEntityType>())
            {
                var annotations = (IDictionary<string, Annotation>?)AnnotationsField.GetValue(model);

                if (annotations is null)
                {
                    return;
                }

                foreach (var key in annotations.Keys.ToArray())
                {
                    if (!key.StartsWith(Constants.NativeStoredProcedureAnnotationKey))
                    {
                        continue;
                    }

                    var annotation = annotations[key];

                    var value = annotation.Value;

                    if (value is not INativeStoredProcedure trigger)
                    {
                        continue;
                    }

                    var sql = storedProcedureVisitor.GenerateCreateNativeStoredProcedureSql(trigger);
                    annotations[key] = new ConventionAnnotation(key, sql, ConfigurationSource.DataAnnotation);
                }
            }
        }

        /// <summary>
        /// Get names of entities in <see cref="IModel"/>.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string[] GetEntityTypeNames(this IModel? model)
        {
            return model?
                .GetEntityTypes()
                .Select(x => x.Name)
                .ToArray()
                   ?? Array.Empty<string>();
        }

        public static IEnumerable<IAnnotation> GetStoredProcedureAnnotations(this IAnnotatable entityType)
            => GetNativeAnnotations(entityType, Constants.NativeStoredProcedureAnnotationKey);

        public static IEnumerable<IAnnotation> GetNativeAnnotations(this IAnnotatable entityType, string nativeAnnotationKeyFormat)
        {
            if (null == entityType)
                return Enumerable.Empty<IAnnotation>();

            return entityType.GetAnnotations()
                .Where(x => Regex.IsMatch(x.Name, $"^({nativeAnnotationKeyFormat}_([0-9]+)_).+"))
                .OrderBy(x => int.Parse(Regex.Match(x.Name, $"^({nativeAnnotationKeyFormat}_([0-9]+)_).+").Groups[2].Value));
        }

        public static IList<SqlOperation> AddCreateNativeStoredProcedureSqlMigration(this IList<SqlOperation> list, IAnnotation annotation, IDictionary<SqlOperation, int> ordering)
        {
            var storedProcedureSql = (annotation.Value as INativeStoredProcedure).Sql;

            var op = new SqlOperation
            {
                Sql = storedProcedureSql,
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeStoredProcedureAnnotationKey);
            ordering[op] = order;
            return list;
        }

        public static IList<SqlOperation> AddDeleteNativeStoredProcedureSqlMigration(this INativeStoredProcedureVisitor nativeStoredProcedureVisitor, IList<SqlOperation> list, IAnnotation annotation, IModel model, IDictionary<SqlOperation, int> ordering)
        {
            var op = new SqlOperation
            {
                Sql = nativeStoredProcedureVisitor.GenerateDeleteNativeStoredProcedureSql(annotation.Name),
            };
            list.Add(op);
            var order = NativeDbObjectExtensions.NativeAnnotationNameToSortOrder(annotation.Name, Constants.NativeStoredProcedureAnnotationKey);
            ordering[op] = order;
            return list;
        }
    }
}
