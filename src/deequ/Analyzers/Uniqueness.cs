using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Spark.Sql;
using xdeequ.Extensions;
using xdeequ.Metrics;
using xdeequ.Util;
using static Microsoft.Spark.Sql.Functions;

namespace xdeequ.Analyzers
{
    public sealed class Uniqueness : ScanShareableFrequencyBasedAnalyzer, IFilterableAnalyzer,
        IGroupAnalyzer<FrequenciesAndNumRows, DoubleMetric>
    {
        public IEnumerable<string> Columns;
        public readonly Option<string> Where;

        public Uniqueness(IEnumerable<string> columns, Option<string> where) : base("Uniqueness", columns)
        {
            Columns = columns;
            Where = where;
        }

        public Uniqueness(IEnumerable<string> columns) : base("Uniqueness", columns)
        {
            Columns = columns;
            Where = Option<string>.None;
        }

        public Option<string> FilterCondition() => Where;

        public override DoubleMetric ToFailureMetric(Exception e) => base.ToFailureMetric(e);

        public override IEnumerable<Column> AggregationFunctions(long numRows) =>
            new[] { Sum(Col(AnalyzersExt.COUNT_COL).EqualTo(Lit(1)).Cast("double")) / numRows };


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb
                .Append(GetType().Name)
                .Append("(")
                .Append("List(")
                .Append(string.Join(",", Columns))
                .Append(")")
                .Append(",")
                .Append(Where.GetOrElse("None"))
                .Append(")");

            return sb.ToString();
        }
    }
}
