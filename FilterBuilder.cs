using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SamedisExternalSync;

public class FilterBuilder
{
  public enum FilterType
  {
    Empty,
    NotEmpty,
    InSet,
    NotInSet,
    Contains,
    Equals,
    NotEqual,
    EndsWith,
    StartsWith,
    NotContains,
    InRange,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    BeforeToday,
    AfterToday,
    BeforeNow,
    AfterNow
  }

  public enum Type
  {
    Text,
    Number,
    ObjectId,
    Date,
    DateTime,
    Bool
  }

  public enum Operator
  {
    AND,
    OR
  }

  private class FilterCondition
  {
    public FilterType FilterType { get; set; }
    public Type Type { get; set; }
    public object? Value { get; set; }
    public object? ValueTo { get; set; }
  }

  private class FieldFilters
  {
    public List<FilterCondition> Conditions { get; } = new();
    public Operator? FieldOperator { get; set; }
  }

  private readonly Dictionary<string, FieldFilters> _filters = new();

  public void Add(string column, FilterType filterType, Type type, object? value = null, object? valueTo = null, Operator? op = null)
  {
    if (!_filters.ContainsKey(column))
    {
      _filters[column] = new FieldFilters();
    }

    var condition = new FilterCondition
    {
      FilterType = filterType,
      Type = type,
      Value = value,
      ValueTo = valueTo
    };

    _filters[column].Conditions.Add(condition);

    if (op.HasValue)
    {
      _filters[column].FieldOperator = op;
    }
  }

  public void Clear()
  {
    _filters.Clear();
  }

  private string Generate(bool prettyPrint)
  {
    var result = new Dictionary<string, object>();

    foreach (var column in _filters)
    {
      var fieldFilters = column.Value;
      var conditions = fieldFilters.Conditions;
      if (conditions.Count == 1)
      {
        var singleCondition = conditions[0];
        var valueFromKey = GetFromValueKey(singleCondition.Type);
        var valueToKey = GetToValueKey(singleCondition.Type);
        var singleFilter = new Dictionary<string, object>
                    {
                        { "filterType", ToFilterType(singleCondition.Type) },
                        { "type", ToConditionType(singleCondition.FilterType) }
                    };

        if (singleCondition.FilterType != FilterType.Empty && singleCondition.FilterType != FilterType.NotEmpty)
        {
          singleFilter.Add(valueFromKey, FormatValue(singleCondition.Value, singleCondition.Type));
        }

        if (singleCondition.ValueTo != null)
        {
          singleFilter.Add(valueToKey, FormatValue(singleCondition.ValueTo, singleCondition.Type));
        }

        result[column.Key] = singleFilter;
      }
      else
      {
        var compoundFilter = new Dictionary<string, object>();
        int conditionNumber = 1;
        Type? firstConditionType = null;

        foreach (var condition in conditions)
        {
          firstConditionType ??= condition.Type;
          var valueFromKey = GetFromValueKey(condition.Type);
          var valueToKey = GetToValueKey(condition.Type);
          var conditionKey = $"condition{conditionNumber++}";
          var conditionDict = new Dictionary<string, object>
                        {
                            { "filterType", ToFilterType(condition.Type) },
                            { "type", ToConditionType(condition.FilterType) }
                        };

          if (condition.FilterType != FilterType.Empty && condition.FilterType != FilterType.NotEmpty)
          {
            conditionDict.Add(valueFromKey, FormatValue(condition.Value, condition.Type));
          }

          if (condition.ValueTo != null)
          {
            conditionDict.Add(valueToKey, FormatValue(condition.ValueTo, condition.Type));
          }

          compoundFilter[conditionKey] = conditionDict;
        }

        if (firstConditionType.HasValue)
        {
          compoundFilter["filterType"] = ToFilterType(firstConditionType.Value);
        }

        if (fieldFilters.FieldOperator.HasValue)
        {
          compoundFilter["operator"] = fieldFilters.FieldOperator.Value.ToString();
        }
        result[column.Key] = compoundFilter;
      }
    }

    return JsonConvert.SerializeObject(result, prettyPrint ? Formatting.Indented : Formatting.None);
  }

  private object FormatValue(object? value, Type type)
  {
    if (value == null) return "";
    if (type == Type.ObjectId && value is IEnumerable<string>)
      return value;
    if (type == Type.Date)
      return NormalizeDateValue(value);
    if (type == Type.DateTime)
      return NormalizeDateTimeValue(value);

    return UrlEncodeValue(value);
  }

  private static string NormalizeDateValue(object value)
  {
    if (value is DateTimeOffset dto)
      return dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    if (value is DateTime dt)
      return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    var raw = value.ToString() ?? string.Empty;
    if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDto))
      return parsedDto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDt))
      return parsedDt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    return raw;
  }

  private static string NormalizeDateTimeValue(object value)
  {
    if (value is DateTimeOffset dto)
      return dto.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    if (value is DateTime dt)
      return dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    var raw = value.ToString() ?? string.Empty;
    if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDto))
      return parsedDto.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDt))
      return parsedDt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    return raw;
  }

  private static string UrlEncodeValue(object value)
  {
    var strValue = value?.ToString() ?? string.Empty;
    return strValue
        .Replace("&", "%26")
        .Replace("/", "%2F")
        .Replace("+", "%2B");
  }

  private static string GetFromValueKey(Type type)
  {
    return type switch
    {
      Type.Date => "dateFrom",
      Type.DateTime => "dateTimeFrom",
      _ => "filter"
    };
  }

  private static string GetToValueKey(Type type)
  {
    return type switch
    {
      Type.Date => "dateTo",
      Type.DateTime => "dateTimeTo",
      _ => "filterTo"
    };
  }

  private static string ToFilterType(Type type)
  {
    return type switch
    {
      Type.Text => "text",
      Type.Number => "number",
      Type.ObjectId => "object_id",
      Type.Date => "date",
      Type.DateTime => "dateTime",
      Type.Bool => "bool",
      _ => "text"
    };
  }

  private static string ToConditionType(FilterType filterType)
  {
    return filterType switch
    {
      FilterType.Empty => "empty",
      FilterType.NotEmpty => "notEmpty",
      FilterType.InSet => "inSet",
      FilterType.NotInSet => "notInSet",
      FilterType.Contains => "contains",
      FilterType.Equals => "equals",
      FilterType.NotEqual => "notEqual",
      FilterType.EndsWith => "endsWith",
      FilterType.StartsWith => "startsWith",
      FilterType.NotContains => "notContains",
      FilterType.InRange => "inRange",
      FilterType.LessThan => "lessThan",
      FilterType.LessThanOrEqual => "lessThanOrEqual",
      FilterType.GreaterThan => "greaterThan",
      FilterType.GreaterThanOrEqual => "greaterThanOrEqual",
      FilterType.BeforeToday => "beforeToday",
      FilterType.AfterToday => "afterToday",
      FilterType.BeforeNow => "beforeNow",
      FilterType.AfterNow => "afterNow",
      _ => "equals"
    };
  }

  public string? Get(bool prettyPrint = false)
  {
    return Generate(prettyPrint);
  }

  public override string ToString()
  {
    return Generate(true);
  }
}
