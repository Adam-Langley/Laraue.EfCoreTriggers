using Laraue.EfCoreTriggers.Common.Extensions;
using Laraue.EfCoreTriggers.SqlServer.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MinRisk.EfCoreTriggers.Common.NativeBuilders;

namespace MinRisk.EfCoreTriggers.SqlServer.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Add EF Core triggers SQL Server provider.
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="modifyServices"></param>
        /// <typeparam name="TContext"></typeparam>
        /// <returns></returns>
        public static DbContextOptionsBuilder<TContext> UseNativeSqlServerTriggers<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            Action<IServiceCollection> modifyServices = null)
            where TContext : DbContext
        {
            return optionsBuilder.UseEfCoreTriggers(AddNativeServices, modifyServices)
                .ReplaceService<IMigrationsModelDiffer, NativeMigrationsModelDiffer>();
        }

        /// <summary>
        /// Add EF Core triggers SQL Server provider.
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="modifyServices"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder UseNativeSqlServerTriggers(
            this DbContextOptionsBuilder optionsBuilder,
            Action<IServiceCollection> modifyServices = null)
        {
            return optionsBuilder.UseEfCoreTriggers(AddNativeServices, modifyServices)
                .ReplaceService<IMigrationsModelDiffer, NativeMigrationsModelDiffer>();
        }

        public static void AddNativeServices(this IServiceCollection services)
        {
            services.AddScoped<INativeStoredProcedureModelDiffer, NativeStoredProcedureModelDiffer>()
                    .AddScoped<INativeStoredProcedureVisitor, NativeStoredProcedureVisitor>()
                    .AddSqlServerServices();
        }
    }
}