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
        var singleFilter = new Dictionary<string, object>
                    {
                        { "filterType", ConvertToCamelCaseWithUnderscores(singleCondition.Type.ToString()) },
                        { "type", ConvertToCamelCaseWithUnderscores(singleCondition.FilterType.ToString()) }
                    };

        if (singleCondition.FilterType != FilterType.Empty && singleCondition.FilterType != FilterType.NotEmpty)
        {
          singleFilter.Add(singleCondition.Type == Type.Date || singleCondition.Type == Type.DateTime ? "dateFrom" : "filter", FormatValue(singleCondition.Value, singleCondition.Type));
        }

        if (singleCondition.ValueTo != null)
        {
          singleFilter.Add(singleCondition.Type == Type.Date || singleCondition.Type == Type.DateTime ? "dateTo" : "filterTo", FormatValue(singleCondition.ValueTo, singleCondition.Type));
        }

        result[column.Key] = singleFilter;
      }
      else
      {
        var compoundFilter = new Dictionary<string, object>();
        int conditionNumber = 1;

        foreach (var condition in conditions)
        {
          var conditionKey = $"condition{conditionNumber++}";
          var conditionDict = new Dictionary<string, object>
                        {
                            { "filterType", ConvertToCamelCaseWithUnderscores(condition.Type.ToString()) },
                            { "type", ConvertToCamelCaseWithUnderscores(condition.FilterType.ToString()) }
                        };

          if (condition.FilterType != FilterType.Empty && condition.FilterType != FilterType.NotEmpty)
          {
            conditionDict.Add(condition.Type == Type.Date || condition.Type == Type.DateTime ? "dateFrom" : "filter", FormatValue(condition.Value, condition.Type));
          }

          if (condition.ValueTo != null)
          {
            conditionDict.Add(condition.Type == Type.Date || condition.Type == Type.DateTime ? "dateTo" : "filterTo", FormatValue(condition.ValueTo, condition.Type));
          }

          compoundFilter[conditionKey] = conditionDict;
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

    return UrlEncodeValue(value);
  }

  private static string UrlEncodeValue(object value)
  {
    var strValue = value?.ToString() ?? string.Empty;
    return strValue
        .Replace("&", "%26")
        .Replace("/", "%2F")
        .Replace("+", "%2B");
  }

  private string ConvertToCamelCaseWithUnderscores(string value)
  {
    return string.Concat(value.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
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
