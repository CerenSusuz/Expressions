using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionTrees.Task2.ExpressionMapping
{
    public class MappingGenerator
    {
        private readonly Dictionary<string, string> _customMappings = new Dictionary<string, string>();

        public void AddMapping(string sourcePropName, string targetPropName)
        {
            _customMappings[sourcePropName] = targetPropName;
        }

        public Mapper<TSource, TDestination> Generate<TSource, TDestination>()
        {
            var sourceParam = Expression.Parameter(typeof(TSource), "src");
            var bindings = new List<MemberBinding>();

            foreach (var targetProp in typeof(TDestination).GetProperties().Where(p => p.CanWrite))
            {
                var sourcePropName = _customMappings.FirstOrDefault(x => x.Value == targetProp.Name).Key
                                     ?? targetProp.Name;

                var sourceProp = typeof(TSource).GetProperty(sourcePropName);
                if (sourceProp == null || !sourceProp.CanRead)
                    continue;

                Expression sourceValue = Expression.Property(sourceParam, sourceProp);

                if (sourceProp.PropertyType != targetProp.PropertyType)
                {
                    if (targetProp.PropertyType == typeof(string) &&
                        sourceProp.PropertyType.GetMethod("ToString", Type.EmptyTypes) != null)
                    {
                        sourceValue = Expression.Call(sourceValue, sourceProp.PropertyType.GetMethod("ToString", Type.EmptyTypes));
                    }
                    else
                    {
                        sourceValue = Expression.Convert(sourceValue, targetProp.PropertyType);
                    }
                }

                bindings.Add(Expression.Bind(targetProp, sourceValue));
            }

            var body = Expression.MemberInit(Expression.New(typeof(TDestination)), bindings);
            var mapFunction = Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam);

            return new Mapper<TSource, TDestination>(mapFunction.Compile());
        }
    }
}
