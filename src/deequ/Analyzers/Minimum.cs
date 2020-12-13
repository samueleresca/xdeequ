using System;
using System.Collections.Generic;
using System.Text;
using deequ.Analyzers.States;
using deequ.Extensions;
using deequ.Metrics;
using deequ.Util;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using static Microsoft.Spark.Sql.Functions;

namespace deequ.Analyzers
{
    public sealed class MinState : DoubleValuedState<MinState>, IState
    {
        private readonly double _minValue;

        public MinState(double minValue) => _minValue = minValue;

        public IState Sum(IState other)
        {
            MinState otherMin = (MinState)other;
            return new MinState(Math.Min(_minValue, otherMin._minValue));
        }

        public override MinState Sum(MinState other) => new MinState(Math.Min(_minValue, other._minValue));

        public override double GetMetricValue() => _minValue;
    }

    public class Minimum : StandardScanShareableAnalyzer<MinState>, IFilterableAnalyzer
    {
        public readonly string Column;
        public readonly Option<string> Where;


        public Minimum(string column, Option<string> where) : base("Minimum", column, MetricEntity.Column)
        {
            Column = column;
            Where = where;
        }

        public Option<string> FilterCondition() => Where;


        public override IEnumerable<Column> AggregationFunctions() =>
            new[] { Min(AnalyzersExt.ConditionalSelection(Column, Where)).Cast("double") };

        public override Option<MinState> FromAggregationResult(Row result, int offset) =>
            AnalyzersExt.IfNoNullsIn(result, offset, () => new MinState(result.GetAs<double>(offset)));

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
