using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics;

namespace MinRisk.EfCoreTriggers.Common.NativeBuilders
{
    public static class EntityTypeBuilderExtensions
    {
        private static ModelBuilder AddStoredProcedureAnnotation<T>(
            this ModelBuilder modelBuilder,
            INativeStoredProcedure storedProcedure) where T : class
        {
            Debugger.Launch();
            //modelBuilder.Model.AddAnnotation(storedProcedure.AnnotationName, storedProcedure.BuildSql(NativeDbObjectExtensions.GetSqlProvider(modelBuilder.Model)).Sql);
            //modelBuilder.Model.AddAnnotation(storedProcedure.AnnotationName, "asd");
            modelBuilder.Model.AddAnnotation(storedProcedure.Name, storedProcedure);
            return modelBuilder;
        }

        private static ModelBuilder AddStoredProcedure<T>(this ModelBuilder modelBuilder, string name, string rawScript, Action<NativeStoredProcedure> configuration)
            where T : class
        {

            var count = (modelBuilder.Model as IAnnotatable).GetNativeAnnotations(Constants.NativeStoredProcedureAnnotationKey).Count();
            var storedProcedure = new NativeStoredProcedure(name, rawScript, count);
            configuration.Invoke(storedProcedure);
            return AddStoredProcedureAnnotation<T>(modelBuilder, storedProcedure);
        }

        public static ModelBuilder StoredProcedure<T>(this ModelBuilder entityTypeBuilder, string name, string rawScript, Action<NativeStoredProcedure> configuration)
            where T : class
            => AddStoredProcedure<T>(entityTypeBuilder, name, rawScript, configuration);
    }
}
