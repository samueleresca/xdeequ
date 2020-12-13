using System;
using System.Collections.Generic;
using System.Text;
using deequ.Extensions;
using deequ.Metrics;
using deequ.Util;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using static Microsoft.Spark.Sql.Functions;

namespace deequ.Analyzers
{
    public sealed class MaxLength : StandardScanShareableAnalyzer<MaxState>, IFilterableAnalyzer
    {
        public readonly string Column;
        public readonly Option<string> Where;

        public MaxLength(string column, Option<string> where)
            : base("MaxLength", column, MetricEntity.Column)
        {
            Column = column;
            Where = where;
        }

        public Option<string> FilterCondition() => Where;

        public override IEnumerable<Column> AggregationFunctions() => new[]
        {
            Max(Length(AnalyzersExt.ConditionalSelection(Column, Where))).Cast("double")
        };

        protected override Option<MaxState> FromAggregationResult(Row result, int offset) =>
            AnalyzersExt.IfNoNullsIn(result, offset, () => new MaxState(result.GetAs<double>(offset)));

        public override IEnumerable<Action<StructType>> AdditionalPreconditions() =>
            new[] { AnalyzersExt.HasColumn(Column), AnalyzersExt.IsString(Column) };

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
