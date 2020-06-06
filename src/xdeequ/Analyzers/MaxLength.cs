using System;
using System.Collections.Generic;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using xdeequ.Analyzers.States;
using xdeequ.Extensions;
using xdeequ.Metrics;
using xdeequ.Util;
using static Microsoft.Spark.Sql.Functions;

namespace xdeequ.Analyzers
{
    public class MaxLength : StandardScanShareableAnalyzer<MaxState>, IFilterableAnalyzer
    {
        public string Column;
        public Option<string> Where;


        public MaxLength(string column, Option<string> where) : base("MaxLength", column, Entity.Column)
        {
            Column = column;
            Where = where;
        }

        public static MaxLength Create(string column)
        {
            return new MaxLength(column, new Option<string>());
        }

        public static MaxLength Create(string column, string where)
        {
            return new MaxLength(column, where);
        }

        public override IEnumerable<Column> AggregationFunctions()
        {
            return new[] {Max(Length(AnalyzersExt.ConditionalSelection(Column, Where))).Cast("double")};
        }

        public override Option<MaxState> FromAggregationResult(Row result, int offset)
        {
            return AnalyzersExt.IfNoNullsIn(result, offset, () => new MaxState(result.GetAs<double>(offset)));
        }

        public override IEnumerable<Action<StructType>> AdditionalPreconditions()
        {
            return new[] {AnalyzersExt.HasColumn(Column), AnalyzersExt.IsString(Column)};
        }

        public Option<string> FilterCondition() => Where;
    }
}