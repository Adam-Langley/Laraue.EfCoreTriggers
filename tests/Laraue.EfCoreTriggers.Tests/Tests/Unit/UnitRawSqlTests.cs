﻿using System;
using System.Linq.Expressions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.OnDelete;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.OnInsert;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.OnUpdate;
using Laraue.EfCoreTriggers.Tests.Infrastructure;
using Xunit;
using Xunit.Categories;

namespace Laraue.EfCoreTriggers.Tests.Tests.Unit
{
    [UnitTest]
    public abstract class UnitRawSqlTests
    {
        protected readonly ITriggerProvider Provider;

        protected UnitRawSqlTests(ITriggerProvider provider)
        {
            Provider = provider;
        }
        
        protected abstract string ExceptedInsertTriggerSqlForMemberArgs { get; }
        
        [Fact]
        protected void GenerateSqlForMemberArgs()
        {
            Expression<Func<SourceEntity, object>> arg1Expression = sourceEntity => sourceEntity.BooleanValue;
            Expression<Func<SourceEntity, object>> arg2Expression = sourceEntity => sourceEntity.DoubleValue;
            
            var trigger = new OnInsertTriggerRawSqlAction<SourceEntity>("PERFORM func({0}, {1})", arg1Expression, arg2Expression);

            var generatedSql = trigger.BuildSql(Provider);

            Assert.Equal(ExceptedInsertTriggerSqlForMemberArgs, generatedSql);
        }
        
        
        protected abstract string ExceptedInsertTriggerSqlForComputedArgs { get; }
        
        [Fact]
        protected void GenerateSqlForComputedArgs()
        {
            Expression<Func<SourceEntity, object>> argExpression = sourceEntity => sourceEntity.DoubleValue + 10;
            
            var trigger = new OnInsertTriggerRawSqlAction<SourceEntity>("PERFORM func({0})", argExpression);

            var generatedSql = trigger.BuildSql(Provider);

            Assert.Equal(ExceptedInsertTriggerSqlForComputedArgs, generatedSql);
        }
        
        protected abstract string ExceptedInsertTriggerSqlWhenNoArgs { get; }
        
        [Fact]
        protected void GenerateSqlWhenNoArgs()
        {
            var trigger = new OnInsertTriggerRawSqlAction<SourceEntity>("PERFORM func()");

            var generatedSql = trigger.BuildSql(Provider);

            Assert.Equal(ExceptedInsertTriggerSqlWhenNoArgs, generatedSql);
        }
        
        protected abstract string ExceptedUpdateTriggerSqlForMemberArgs { get; }
        
        [Fact]
        protected void GenerateSqlForUpdateTrigger()
        {
            var trigger = new OnUpdateTriggerRawSqlAction<SourceEntity>("PERFORM func({0}, {1})", 
                (@old, @new) => @old.DecimalValue, 
                (@old, @new) => @new.DecimalValue);

            var generatedSql = trigger.BuildSql(Provider);

            Assert.Equal(ExceptedUpdateTriggerSqlForMemberArgs, generatedSql);
        }
        
        protected abstract string ExceptedDeleteTriggerSqlForMemberArgs { get; }
        
        [Fact]
        protected void GenerateSqlForDeleteTrigger()
        {
            var trigger = new OnDeleteTriggerRawSqlAction<SourceEntity>("PERFORM func({0}, {1})", 
                @old => @old.DecimalValue, 
                @old => @old.DoubleValue);

            var generatedSql = trigger.BuildSql(Provider);

            Assert.Equal(ExceptedDeleteTriggerSqlForMemberArgs, generatedSql);
        }
    }
}