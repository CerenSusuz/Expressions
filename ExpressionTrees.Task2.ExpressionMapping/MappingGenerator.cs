using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionTrees.Task2.ExpressionMapping
{
    public class MappingGenerator
    {
        private readonly Dictionary<string, string> _customMappings = new Dictionary<string, string>();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), object> _cache
            = new ConcurrentDictionary<(Type, Type), object>();

        public void AddMapping(string sourcePropName, string targetPropName)
        {
            _customMappings[sourcePropName] = targetPropName;
        }

        public Mapper<TSource, TDestination> Generate<TSource, TDestination>()
        {
            var key = (typeof(TSource), typeof(TDestination));

            if (_cache.TryGetValue(key, out var cached))
            {
                return (Mapper<TSource, TDestination>)cached;
            }

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

                if (sourceProp.PropertyType == targetProp.PropertyType)
                {
                    bindings.Add(Expression.Bind(targetProp, sourceValue));
                }
                else if (IsSimpleType(sourceProp.PropertyType) && IsSimpleType(targetProp.PropertyType))
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

                    bindings.Add(Expression.Bind(targetProp, sourceValue));
                }
                else if (IsCollectionType(sourceProp.PropertyType) && IsCollectionType(targetProp.PropertyType))
                {
                    var sourceElementType = GetCollectionElementType(sourceProp.PropertyType);
                    var targetElementType = GetCollectionElementType(targetProp.PropertyType);

                    var nestedMapper = GetOrGenerateMapper(sourceElementType, targetElementType);

                    var selectMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(sourceElementType, targetElementType);

                    var toListMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "ToList" && m.GetParameters().Length == 1)
                        .MakeGenericMethod(targetElementType);

                    var param = Expression.Parameter(sourceElementType, "x");
                    var invokeMap = Expression.Invoke(Expression.Constant(nestedMapper), param);
                    var selector = Expression.Lambda(invokeMap, param);

                    var selectCall = Expression.Call(selectMethod, sourceValue, selector);
                    var toListCall = Expression.Call(toListMethod, selectCall);

                    bindings.Add(Expression.Bind(targetProp, toListCall));
                }
                else if (IsComplexType(sourceProp.PropertyType) && IsComplexType(targetProp.PropertyType))
                {
                    var nestedMapper = GetOrGenerateMapper(sourceProp.PropertyType, targetProp.PropertyType);

                    var invokeMap = Expression.Invoke(Expression.Constant(nestedMapper), sourceValue);
                    bindings.Add(Expression.Bind(targetProp, invokeMap));
                }
                else
                {
                    continue;
                }
            }

            var body = Expression.MemberInit(Expression.New(typeof(TDestination)), bindings);

            Expression condition = Expression.Condition(
                Expression.Equal(sourceParam, Expression.Constant(null, typeof(TSource))),
                Expression.Throw(
                    Expression.New(typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }), Expression.Constant("source")),
                    typeof(TDestination)
                ),
                body
            );

            var mapFunction = Expression.Lambda<Func<TSource, TDestination>>(condition, sourceParam);

            var mapper = new Mapper<TSource, TDestination>(mapFunction.Compile());
            _cache[key] = mapper;

            return mapper;
        }

        private Delegate GetOrGenerateMapper(Type sourceType, Type targetType)
        {
            var method = typeof(MappingGenerator).GetMethod(nameof(Generate), Type.EmptyTypes);
            var genericMethod = method.MakeGenericMethod(sourceType, targetType);

            var mapper = genericMethod.Invoke(this, null);

            var mapFuncProp = mapper.GetType().GetField("_mapFunction", BindingFlags.NonPublic | BindingFlags.Instance);

            return (Delegate)mapFuncProp.GetValue(mapper);
        }

        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type == typeof(DateTime)
                   || type == typeof(Guid);
        }

        private bool IsComplexType(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        private bool IsCollectionType(Type type)
        {
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }

        private Type GetCollectionElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType)
            {
                return type.GetGenericArguments()[0];
            }

            throw new NotSupportedException($"Cannot determine collection element type for {type}");
        }
    }
}
