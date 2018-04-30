﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Api.ClientSchema
{
    public static class SchemaBuilder
    {
        private static Dictionary<Type, FieldMapping[]> _mappings = new Dictionary<Type, FieldMapping[]>();

        public static List<Field> ToSchema(object model)
        {
            Ensure.That(model, () => model).IsNotNull();

            var mappings = GetFieldMappings(model.GetType());

            var result = new List<Field>(mappings.Length);

            foreach (var mapping in mappings)
            {
                var field = mapping.Field.Clone();
                field.Value = mapping.GetterFunc(model);

                result.Add(field);
            }

            return result.OrderBy(r => r.Order).ToList();
        }

        public static object ReadFromSchema(List<Field> fields, Type targetType)
        {
            Ensure.That(targetType, () => targetType).IsNotNull();

            var mappings = GetFieldMappings(targetType);

            var target = Activator.CreateInstance(targetType);

            foreach (var mapping in mappings)
            {
                var propertyType = mapping.PropertyType;
                var field = fields.Find(f => f.Name == mapping.Field.Name);

                mapping.SetterFunc(target, field.Value);
            }

            return target;

        }

        public static T ReadFromSchema<T>(List<Field> fields)
        {
            return (T)ReadFromSchema(fields, typeof(T));
        }


        // Ideally this function should begin a System.Linq.Expression expression tree since it's faster.
        // But it's probably not needed till performance issues pop up.
        public static FieldMapping[] GetFieldMappings(Type type)
        {
            lock (_mappings)
            {
                FieldMapping[] result;
                if (!_mappings.TryGetValue(type, out result))
                {
                    result = GetFieldMapping(type, "", v => v);

                    // Renumber al the field Orders since nested settings will have dupe Orders.
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i].Field.Order = i;
                    }

                    _mappings[type] = result;
                }
                return result;
            }
        }

        private static FieldMapping[] GetFieldMapping(Type type, string prefix, Func<object, object> targetSelector)
        {
            var result = new List<FieldMapping>();
            foreach (var property in GetProperties(type))
            {
                var propertyInfo = property.Item1;
                if (propertyInfo.PropertyType.IsSimpleType())
                {
                    var fieldAttribute = property.Item2;
                    var field = new Field
                    {
                        Name = prefix + propertyInfo.Name,
                        Label = fieldAttribute.Label,
                        HelpText = fieldAttribute.HelpText,
                        HelpLink = fieldAttribute.HelpLink,
                        Order = fieldAttribute.Order,
                        Advanced = fieldAttribute.Advanced,
                        Type = fieldAttribute.Type.ToString().ToLowerInvariant()
                    };

                    if (fieldAttribute.Type == FieldType.Select)
                    {
                        field.SelectOptions = GetSelectOptions(fieldAttribute.SelectOptions);
                    }

                    var valueConverter = GetValueConverter(propertyInfo.PropertyType);

                    result.Add(new FieldMapping
                    {
                        Field = field,
                        PropertyType = propertyInfo.PropertyType,
                        GetterFunc = t => propertyInfo.GetValue(targetSelector(t), null),
                        SetterFunc = (t, v) => propertyInfo.SetValue(targetSelector(t), valueConverter(v), null)
                    });
                }
                else
                {
                    result.AddRange(GetFieldMapping(propertyInfo.PropertyType, propertyInfo.Name + ".", t => propertyInfo.GetValue(targetSelector(t), null)));
                }
            }

            return result.ToArray();
        }

        private static Tuple<PropertyInfo, FieldDefinitionAttribute>[] GetProperties(Type type)
        {
            return type.GetProperties()
                .Select(v => Tuple.Create(v, v.GetAttribute<FieldDefinitionAttribute>(false)))
                .Where(v => v.Item2 != null)
                .OrderBy(v => v.Item2.Order)
                .ToArray();
        }

        private static List<SelectOption> GetSelectOptions(Type selectOptions)
        {
            var options = from Enum e in Enum.GetValues(selectOptions)
                          select new SelectOption { Value = Convert.ToInt32(e), Name = e.ToString() };

            return options.OrderBy(o => o.Value).ToList();
        }

        private static Func<object, object> GetValueConverter(Type propertyType)
        {
            if (propertyType == typeof(int))
            {
                return fieldValue => fieldValue?.ToString().ParseInt32() ?? 0;
            }

            else if (propertyType == typeof(long))
            {
                return fieldValue => fieldValue?.ToString().ParseInt64() ?? 0;
            }

            else if (propertyType == typeof(double))
            {
                return fieldValue => fieldValue?.ToString().ParseDouble() ?? 0.0;
            }

            else if (propertyType == typeof(int?))
            {
                return fieldValue => fieldValue?.ToString().ParseInt32();
            }

            else if (propertyType == typeof(Int64?))
            {
                return fieldValue => fieldValue?.ToString().ParseInt64();
            }

            else if (propertyType == typeof(double?))
            {
                return fieldValue => fieldValue?.ToString().ParseDouble();
            }

            else if (propertyType == typeof(IEnumerable<int>))
            {
                return fieldValue =>
                {
                    if (fieldValue.GetType() == typeof(JArray))
                    {
                        return ((JArray)fieldValue).Select(s => s.Value<int>());
                    }
                    else
                    {
                        return fieldValue.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => Convert.ToInt32(s));
                    }
                };
            }

            else if (propertyType == typeof(IEnumerable<string>))
            {
                return fieldValue =>
                {
                    if (fieldValue.GetType() == typeof(JArray))
                    {
                        return ((JArray)fieldValue).Select(s => s.Value<string>());
                    }
                    else
                    {
                        return fieldValue.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                };
            }

            else
            {
                return fieldValue => fieldValue;
            }
        }
    }
}
