﻿using System;
using Foundatio.Parsers.ElasticQueries.Visitors;
using Foundatio.Parsers.LuceneQueries.Visitors;
using Nest;

namespace Foundatio.Parsers.ElasticQueries.Extensions {
    public static class QueryVisitorContextExtensions {
         public static T SetGetFieldMappingFunc<T>(this T context, Func<string, IElasticType> getFieldMappingFunc) where T: IQueryVisitorContext {
            var elasticContext = context as IElasticQueryVisitorContext;
            if (elasticContext == null)
                throw new ArgumentException("Context must be of type IElasticQueryVisitorContext", nameof(context));

            elasticContext.GetFieldMappingFunc = getFieldMappingFunc ?? (field => null);

            return context;
        }

        public static T SetDefaultOperator<T>(this T context, Operator defaultOperator) where T : IQueryVisitorContext {
            var elasticContext = context as IElasticQueryVisitorContext;
            if (elasticContext == null)
                throw new ArgumentException("Context must be of type IElasticQueryVisitorContext", nameof(context));

            elasticContext.DefaultOperator = defaultOperator;

            return context;
        }

        public static T SetDefaultField<T>(this T context, string defaultField) where T : IQueryVisitorContext {
            var elasticContext = context as IElasticQueryVisitorContext;
            if (elasticContext == null)
                throw new ArgumentException("Context must be of type IElasticQueryVisitorContext", nameof(context));

            elasticContext.DefaultField = defaultField;

            return context;
        }
    }
}