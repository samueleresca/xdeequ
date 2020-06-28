using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using xdeequ.Extensions;
using xdeequ.Metrics;
using xdeequ.Util;

namespace xdeequ.Analyzers.Runners
{
    public class AnalyzerContext
    {
        public Dictionary<IAnalyzer<IMetric>, IMetric> MetricMap;

        public AnalyzerContext(Dictionary<IAnalyzer<IMetric>, IMetric> metricMap) => MetricMap = metricMap;

        public static AnalyzerContext Empty() => new AnalyzerContext(new Dictionary<IAnalyzer<IMetric>, IMetric>());

        public IEnumerable<IMetric> AllMetrics() => MetricMap.Values.AsEnumerable();

        public static AnalyzerContext operator +(AnalyzerContext current, AnalyzerContext other)
        {
            current.MetricMap.Merge(other.MetricMap);
            return new AnalyzerContext(current.MetricMap);
        }

        public Option<IMetric> Metric(IAnalyzer<IMetric> analyzer)
        {
            try
            {
                return new Option<IMetric>(MetricMap[analyzer]);
            }
            catch (KeyNotFoundException e)
            {
                return Option<IMetric>.None;
            }
        }

        public DataFrame SuccessMetricsAsDataFrame(
            SparkSession sparkSession,
            IEnumerable<IAnalyzer<IMetric>> forAnalyzers)
        {
            IEnumerable<GenericRow> metricList =
                GetSimplifiedMetricOutputForSelectedAnalyzers(forAnalyzers)
                    .Select(x => new GenericRow(new object[] { x.Entity.ToString(), x.Instance, x.Name, x.Value }));

            DataFrame df = sparkSession.CreateDataFrame(metricList,
                new StructType(new[]
                {
                    new StructField("entity", new StringType()), new StructField("instance", new StringType()),
                    new StructField("name", new StringType()), new StructField("value", new DoubleType())
                }));
            return df;
        }

        public string SuccessMetricsAsJson(IEnumerable<IAnalyzer<IMetric>> forAnalyzers)
        {
            SimpleMetricOutput[] metricsList = GetSimplifiedMetricOutputForSelectedAnalyzers(forAnalyzers).ToArray();
            return JsonSerializer.Serialize(metricsList);
        }

        public IEnumerable<SimpleMetricOutput> GetSimplifiedMetricOutputForSelectedAnalyzers(
            IEnumerable<IAnalyzer<IMetric>> forAnalyzers) =>
            MetricMap
                .Where((pair, i) => !forAnalyzers.Any() || forAnalyzers.Contains(pair.Key))
                .Where((pair, i) =>
                {
                    DoubleMetric dm = pair.Value as DoubleMetric;
                    return dm.Value.IsSuccess;
                })
                .SelectMany(pair =>
                {
                    DoubleMetric dm = pair.Value as DoubleMetric;
                    return dm.Flatten().Select(x => RenameMetric(x, DescribeAnalyzer(pair.Key)));
                })
                .Select(x => new SimpleMetricOutput(x));

        private static string DescribeAnalyzer(IAnalyzer<IMetric> analyzer)
        {
            string name = analyzer.GetType().Name;

            Option<string> filter = Option<string>.None;

            if (analyzer is IFilterableAnalyzer filterable)
            {
                filter = filterable.FilterCondition();
            }

            return filter.Select(x => $"{name} (where: {x} )").GetOrElse(name);
        }

        private static DoubleMetric RenameMetric(DoubleMetric doubleMetric, string newName) =>
            new DoubleMetric(doubleMetric.Entity, newName, doubleMetric.Instance, doubleMetric.Value);
    }

    public class SimpleMetricOutput
    {
        public SimpleMetricOutput(DoubleMetric doubleMetric)
        {
            Entity = Enum.GetName(typeof(Entity), doubleMetric.Entity);
            Instance = doubleMetric.Instance;
            Name = doubleMetric.Name;
            Value = doubleMetric.Value.Get();
        }

        public SimpleMetricOutput()
        {
        }

        public string Entity { get; set; }
        public string Instance { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
    }
}