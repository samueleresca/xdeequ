using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using xdeequ.Analyzers.States;
using xdeequ.Extensions;
using xdeequ.Metrics;
using xdeequ.Util;
using static Microsoft.Spark.Sql.Functions;

namespace xdeequ.Analyzers
{
    public class SumState : DoubleValuedState<SumState>, IState
    {
        private readonly double _sum;

        public SumState(double sum) => _sum = sum;

        public IState Sum(IState other)
        {
            SumState sumStateOther = (SumState)other;
            return new SumState(_sum + sumStateOther._sum);
        }

        public override SumState Sum(SumState other) => new SumState(_sum + other._sum);

        public override double MetricValue() => _sum;
    }

    internal sealed class Sum : StandardScanShareableAnalyzer<SumState>, IFilterableAnalyzer, IAnalyzer<DoubleMetric>
    {
        public readonly string Column;
        public readonly Option<string> Where;

        public Sum(string column, Option<string> where) : base("Sum", column, Entity.Column)
        {
            Column = column;
            Where = where;
        }

        public new DoubleMetric Calculate(DataFrame data) => base.Calculate(data);

        public Option<string> FilterCondition() => Where;

        public override IEnumerable<Column> AggregationFunctions() =>
            new[] { Sum(AnalyzersExt.ConditionalSelection(Column, Where)).Cast("double") };

        public override Option<SumState> FromAggregationResult(Row result, int offset) =>
            AnalyzersExt.IfNoNullsIn(result, offset, () => new SumState(result.GetAs<double>(offset)));

        public override IEnumerable<Action<StructType>> AdditionalPreconditions() =>
            new[] { AnalyzersExt.HasColumn(Column), AnalyzersExt.IsNumeric(Column) };

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb
                .Append(GetType().Name)
                .Append("(")
                .Append(Column)
                .Append(",")
                .Append(Where.GetOrElse("None"))
                .Append(")");

            return sb.ToString();
        }
    }
}
