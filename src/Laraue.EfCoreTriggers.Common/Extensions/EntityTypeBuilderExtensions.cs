using Laraue.EfCoreTriggers.Common.Builders.Native.Indexes;
using Laraue.EfCoreTriggers.Common.Builders.Native.StoredProcedures;
using Laraue.EfCoreTriggers.Common.Builders.Native.Trigger;
using Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedFunctions;
using Laraue.EfCoreTriggers.Common.Builders.Native.UserDefinedTypes;
using Laraue.EfCoreTriggers.Common.Builders.Native.Views;
using Laraue.EfCoreTriggers.Common.Builders.Triggers.OnDelete;
using Laraue.EfCoreTriggers.Common.Builders.Triggers.OnInsert;
using Laraue.EfCoreTriggers.Common.Builders.Triggers.OnUpdate;
using Laraue.EfCoreTriggers.Common.TriggerBuilders;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.Abstractions;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq;

namespace Laraue.EfCoreTriggers.Common.Extensions
{
    /// <summary>
    /// Extensions to configure triggers.
    /// </summary>
    public static class EntityTypeBuilderExtensions
    {
        /// <summary>
        /// Generate SQL for configured via <see cref="EntityTypeBuilder{T}"/> <see cref="ITrigger"/>
        /// and append it as annotation to the entity.
        /// </summary>
        /// <param name="entityTypeBuilder"></param>
        /// <param name="configuredTrigger"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static EntityTypeBuilder<T> AddTriggerAnnotation<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            ITrigger configuredTrigger) where T : class
        {
            var entityTypeName = typeof(T).FullName!;
            var entityType = entityTypeBuilder.Metadata.Model.FindEntityType(entityTypeName)
                ?? throw new InvalidOperationException($"Entity {entityTypeName} metadata was not found");

#if NET6_0_OR_GREATER
            entityTypeBuilder.ToTable(tb => tb.HasTrigger(configuredTrigger.Name));
#endif
            entityType.AddAnnotation(configuredTrigger.Name, configuredTrigger);

            return entityTypeBuilder;
        }

        private static EntityTypeBuilder<T> AddTriggerAnnotation<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Builders.Triggers.Base.Trigger<T> configuredTrigger) where T : class
        {
            var entityTypeName = typeof(T).FullName!;
            var entityType = entityTypeBuilder.Metadata.Model.FindEntityType(entityTypeName)
                ?? throw new InvalidOperationException($"Entity {entityTypeName} metadata was not found");

            entityType.AddAnnotation(configuredTrigger.Name, configuredTrigger.BuildSql(TriggerExtensions.GetSqlProvider(entityTypeBuilder.Metadata.Model)).Sql);
            return entityTypeBuilder;
        }

        private static ModelBuilder AddStoredProcedureAnnotation(
            this ModelBuilder modelBuilder,
            StoredProcedureTypeBuilder storedProcedure)
        {
            modelBuilder.Model.AddAnnotation(storedProcedure.AnnotationName, storedProcedure.BuildSql(NativeDbObjectExtensions.GetSqlProvider(modelBuilder.Model)).Sql);
            return modelBuilder;
        }

        private static ModelBuilder AddUserDefinedTypeAnnotation(
            this ModelBuilder modelBuilder,
            UserDefinedTypeTypeBuilder userDefinedType)
        {
            modelBuilder.Model.AddAnnotation($"{userDefinedType.AnnotationName}", userDefinedType.BuildSql(NativeDbObjectExtensions.GetSqlProvider(modelBuilder.Model)).Sql);
            return modelBuilder;
        }

        private static ModelBuilder AddUserDefinedFunctionAnnotation(
            this ModelBuilder modelBuilder,
            UserDefinedFunctionTypeBuilder userDefinedFunction)
        {
            modelBuilder.Model.AddAnnotation(userDefinedFunction.AnnotationName, userDefinedFunction.BuildSql(NativeDbObjectExtensions.GetSqlProvider(modelBuilder.Model)).Sql);
            return modelBuilder;
        }

        private static ModelBuilder AddViewAnnotation(
            this ModelBuilder modelBuilder,
            ViewTypeBuilder view)
        {
            modelBuilder.Model.AddAnnotation(view.AnnotationName, view.BuildSql(NativeDbObjectExtensions.GetSqlProvider(modelBuilder.Model)).Sql);
            return modelBuilder;
        }
        private static EntityTypeBuilder<T> AddNativeIndexAnnotation<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            NativeIndexTypeBuilder<T> index) where T : class
        {
            var entityType = entityTypeBuilder.Metadata.Model.FindEntityType(typeof(T).FullName);
            entityType.AddAnnotation(index.AnnotationName, index.BuildSql(NativeDbObjectExtensions.GetSqlProvider(entityTypeBuilder.Metadata.Model)).Sql);
            return entityTypeBuilder;
        }

        private static EntityTypeBuilder<T> AddNativeTriggerAnnotation<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            NativeTriggerTypeBuilder<T> view) where T : class
        {
            var entityType = entityTypeBuilder.Metadata.Model.FindEntityType(typeof(T).FullName);
            entityType.AddAnnotation(view.AnnotationName, view.BuildSql(NativeDbObjectExtensions.GetSqlProvider(entityTypeBuilder.Metadata.Model)).Sql);
            return entityTypeBuilder;
        }

        /// <summary>
        /// Execute SQL before entity will be updated.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity should be updated.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> BeforeUpdate<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, OldAndNewTableRefs<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Update,
                TriggerTime.Before,
                configuration);
        }

        /// <summary>
        /// Execute SQL after entity updated.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity that was updated.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> AfterUpdate<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, OldAndNewTableRefs<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Update,
                TriggerTime.After,
                configuration);
        }

        /// <summary>
        /// Execute SQL instead of entity update.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity should be updated.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> InsteadOfUpdate<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, OldAndNewTableRefs<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Update,
                TriggerTime.InsteadOf,
                configuration);
        }

        /// <summary>
        /// Execute SQL before entity delete.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity should be deleted.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> BeforeDelete<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, OldTableRef<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Delete,
                TriggerTime.Before,
                configuration);
        }

        /// <summary>
        /// Execute SQL after entity delete.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity that was deleted.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> AfterDelete<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, OldTableRef<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Delete,
                TriggerTime.After,
                configuration);
        }

        /// <summary>
        /// Execute SQL instead of entity delete.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity should be deleted.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> InsteadOfDelete<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, OldTableRef<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Delete,
                TriggerTime.InsteadOf,
                configuration);
        }

        /// <summary>
        /// Execute SQL before entity insert.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity should be inserted.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> BeforeInsert<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, NewTableRef<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Insert,
                TriggerTime.Before,
                configuration);
        }

        /// <summary>
        /// Execute SQL after entity insert.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity that was inserted.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> AfterInsert<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, NewTableRef<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Insert,
                TriggerTime.After,
                configuration);
        }

        /// <summary>
        /// Execute SQL instead of entity insert.
        /// </summary>
        /// <param name="entityTypeBuilder">Entity builder.</param>
        /// <param name="configuration">Action to execute.</param>
        /// <typeparam name="T">Entity should be inserted.</typeparam>
        /// <returns></returns>
        public static EntityTypeBuilder<T> InsteadOfInsert<T>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            Action<Trigger<T, NewTableRef<T>>> configuration)
            where T : class
        {
            return entityTypeBuilder.AddTrigger(
                TriggerEvent.Insert,
                TriggerTime.InsteadOf,
                configuration);
        }

        private static EntityTypeBuilder<T> AddTrigger<T, TRefs>(
            this EntityTypeBuilder<T> entityTypeBuilder,
            TriggerEvent triggerEvent,
            TriggerTime triggerTime,
            Action<Trigger<T, TRefs>> configureTrigger)
            where T : class
            where TRefs : ITableRef<T>
        {
            var trigger = new Trigger<T, TRefs>(triggerEvent, triggerTime);

            configureTrigger.Invoke(trigger);

            return entityTypeBuilder.AddTriggerAnnotation(trigger);
        }


        public static EntityTypeBuilder<T> BeforeUpdate<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnUpdateTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnUpdateTrigger(configuration, TriggerTime.Before);

        public static EntityTypeBuilder<T> AfterUpdate<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnUpdateTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnUpdateTrigger(configuration, TriggerTime.After);

        public static EntityTypeBuilder<T> InsteadOfUpdate<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnUpdateTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnUpdateTrigger(configuration, TriggerTime.InsteadOf);

        public static EntityTypeBuilder<T> BeforeDelete<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnDeleteTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnDeleteTrigger(configuration, TriggerTime.Before);

        public static EntityTypeBuilder<T> AfterDelete<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnDeleteTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnDeleteTrigger(configuration, TriggerTime.After);

        public static EntityTypeBuilder<T> InsteadOfDelete<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnDeleteTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnDeleteTrigger(configuration, TriggerTime.InsteadOf);

        public static EntityTypeBuilder<T> BeforeInsert<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnInsertTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnInsertTrigger(configuration, TriggerTime.Before);

        public static EntityTypeBuilder<T> AfterInsert<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnInsertTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnInsertTrigger(configuration, TriggerTime.After);

        public static EntityTypeBuilder<T> InsteadOfInsert<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnInsertTrigger<T>> configuration) where T : class
            => entityTypeBuilder.AddOnInsertTrigger(configuration, TriggerTime.InsteadOf);

        public static EntityTypeBuilder<T> NativeTrigger<T>(this EntityTypeBuilder<T> entityTypeBuilder, string name, string rawScript, Action<NativeTriggerTypeBuilder<T>> configuration) where T : class
            => entityTypeBuilder.AddNativeTrigger<T>(configuration, name, rawScript);


        public static ModelBuilder StoredProcedure(this ModelBuilder entityTypeBuilder, string name, string rawScript, Action<StoredProcedureTypeBuilder> configuration)
            => entityTypeBuilder.AddStoredProcedure(name, rawScript, configuration);

        public static ModelBuilder UserDefinedType(this ModelBuilder entityTypeBuilder, string name, string returnType, Action<UserDefinedTypeTypeBuilder> configuration)
            => entityTypeBuilder.AddUserDefinedType(name, returnType, configuration);

        public static ModelBuilder UserDefinedFunction(this ModelBuilder entityTypeBuilder, string name, string rawScript, Action<UserDefinedFunctionTypeBuilder> configuration)
            => entityTypeBuilder.AddUserDefinedFunction(name, rawScript, configuration);

        public static ModelBuilder View(this ModelBuilder entityTypeBuilder, string name, string rawScript, Action<ViewTypeBuilder> configuration)
            => entityTypeBuilder.AddView(name, rawScript, configuration);

        public static EntityTypeBuilder<T> NativeIndex<T>(this EntityTypeBuilder<T> entityTypeBuilder, string name, string rawScript, Action<NativeIndexTypeBuilder<T>> configuration) where T : class
            => entityTypeBuilder.AddNativeIndex<T>(name, rawScript, configuration);

        private static EntityTypeBuilder<T> AddOnUpdateTrigger<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnUpdateTrigger<T>> configuration,
            TriggerTime triggerTime) where T : class
        {
            var trigger = new OnUpdateTrigger<T>(triggerTime);
            configuration.Invoke(trigger);

            return entityTypeBuilder.AddTriggerAnnotation(trigger);
        }

        private static EntityTypeBuilder<T> AddOnDeleteTrigger<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnDeleteTrigger<T>> configuration,
            TriggerTime triggerTime) where T : class
        {
            var trigger = new OnDeleteTrigger<T>(triggerTime);
            configuration.Invoke(trigger);
            return entityTypeBuilder.AddTriggerAnnotation(trigger);
        }

        private static EntityTypeBuilder<T> AddOnInsertTrigger<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<OnInsertTrigger<T>> configuration,
            TriggerTime triggerTime) where T : class
        {
            var trigger = new OnInsertTrigger<T>(triggerTime);
            configuration.Invoke(trigger);
            return entityTypeBuilder.AddTriggerAnnotation(trigger);
        }

        private static ModelBuilder AddStoredProcedure(this ModelBuilder modelBuilder, string name, string rawScript, Action<StoredProcedureTypeBuilder> configuration)
        {
            var count = modelBuilder.Model.GetAnnotations().Count();
            var storedProcedure = new StoredProcedureTypeBuilder(name, rawScript, count);
            configuration.Invoke(storedProcedure);
            return modelBuilder.AddStoredProcedureAnnotation(storedProcedure);
        }

        private static ModelBuilder AddUserDefinedType(this ModelBuilder modelBuilder, string name, string type, Action<UserDefinedTypeTypeBuilder> configuration)
        {
            var count = modelBuilder.Model.GetAnnotations().Count();
            var userDefinedType = new UserDefinedTypeTypeBuilder(name, type, count);
            configuration.Invoke(userDefinedType);
            return modelBuilder.AddUserDefinedTypeAnnotation(userDefinedType);
        }

        private static ModelBuilder AddUserDefinedFunction(this ModelBuilder modelBuilder, string name, string rawScript, Action<UserDefinedFunctionTypeBuilder> configuration)
        {
            var count = modelBuilder.Model.GetAnnotations().Count();
            var userDefinedFunction = new UserDefinedFunctionTypeBuilder(name, rawScript, count);
            configuration.Invoke(userDefinedFunction);
            return modelBuilder.AddUserDefinedFunctionAnnotation(userDefinedFunction);
        }

        private static ModelBuilder AddView(this ModelBuilder modelBuilder, string name, string rawScript, Action<ViewTypeBuilder> configuration)
        {
            var count = modelBuilder.Model.GetAnnotations().Count();
            var view = new ViewTypeBuilder(name, rawScript, count);
            configuration.Invoke(view);
            return modelBuilder.AddViewAnnotation(view);
        }

        private static EntityTypeBuilder<T> AddNativeIndex<T>(this EntityTypeBuilder<T> entityTypeBuilder, string name, string rawScript, Action<NativeIndexTypeBuilder<T>> configuration)
             where T : class
        {
            var index = new NativeIndexTypeBuilder<T>(name, rawScript, 0);
            configuration.Invoke(index);
            return entityTypeBuilder.AddNativeIndexAnnotation(index);
        }

        private static EntityTypeBuilder<T> AddNativeTrigger<T>(this EntityTypeBuilder<T> entityTypeBuilder, Action<NativeTriggerTypeBuilder<T>> configuration,
            string name, string rawScript) where T : class
        {
            var view = new NativeTriggerTypeBuilder<T>(name, rawScript, 0);
            configuration.Invoke(view);
            return entityTypeBuilder.AddNativeTriggerAnnotation(view);
        }
    }
}