using System.Linq;
using deequ.Analyzers;
using deequ.Analyzers.States;
using deequ.Metrics;
using Microsoft.Spark.Sql;
using Shouldly;
using Xunit;
using static deequ.Analyzers.Initializers;

namespace xdeequ.tests.Analyzers
{
    [Collection("Spark instance")]
    public class CorrelationAnalyzer
    {
        private readonly SparkSession _session;

        public CorrelationAnalyzer(SparkFixture fixture) => _session = fixture.Spark;


        [Fact]
        public void compute_strong_positive_correlation_coefficient()
        {
            DataFrame numericValues = FixtureSupport.GetDfWithStrongPositiveCorrelation(_session);

            Correlation("att1", "att2").Preconditions().Count().ShouldBe(4);

            DoubleMetric attCorrelation = Correlation("att1", "att2").Calculate(numericValues);
            DoubleMetric expected1 = DoubleMetric.Create(MetricEntity.Multicolumn, "Correlation", "att1,att2", 1);

            attCorrelation.MetricEntity.ShouldBe(expected1.MetricEntity);
            attCorrelation.Instance.ShouldBe(expected1.Instance);
            attCorrelation.Name.ShouldBe(expected1.Name);
            attCorrelation.Value.Get().ShouldBe(expected1.Value.Get(), 0.00001);
        }

        [Fact]
        public void compute_strong_negative_correlation_coefficient()
        {
            DataFrame numericValues = FixtureSupport.GetDfWithStrongNegativeCorrelation(_session);

            Correlation("att1", "att2").Preconditions().Count().ShouldBe(4);

            DoubleMetric attCorrelation = Correlation("att1", "att2").Calculate(numericValues);
            DoubleMetric expected1 = DoubleMetric.Create(MetricEntity.Multicolumn, "Correlation", "att1,att2", -1);

            attCorrelation.MetricEntity.ShouldBe(expected1.MetricEntity);
            attCorrelation.Instance.ShouldBe(expected1.Instance);
            attCorrelation.Name.ShouldBe(expected1.Name);
            attCorrelation.Value.Get().ShouldBe(expected1.Value.Get(), 0.1);

        }


        [Fact]
        public void compute_low_positive_correlation_coefficient()
        {
            DataFrame numericValues = FixtureSupport.GetDfWithLowCorrelation(_session);

            Correlation("att1", "att2").Preconditions().Count().ShouldBe(4);

            DoubleMetric attCorrelation = Correlation("att1", "att2").Calculate(numericValues);
            DoubleMetric expected1 = DoubleMetric.Create(MetricEntity.Multicolumn, "Correlation", "att1,att2", 0.0);

            attCorrelation.MetricEntity.ShouldBe(expected1.MetricEntity);
            attCorrelation.Instance.ShouldBe(expected1.Instance);
            attCorrelation.Name.ShouldBe(expected1.Name);
            attCorrelation.Value.Get().ShouldBe(expected1.Value.Get(), 0.1);
        }

        [Fact]
        public void correlation_coefficient_is_commutative()
        {
            DataFrame numericValues = FixtureSupport.GetDfWithLowCorrelation(_session);

            DoubleMetric attCorrelation = Correlation("att1", "att2").Calculate(numericValues);
            DoubleMetric attCorrelationComm = Correlation("att2", "att1").Calculate(numericValues);

            attCorrelation.Value.Get().ShouldBe(attCorrelationComm.Value.Get());
        }

        [Fact]
        public void compute_correlation_coefficient_with_filtering()
        {
            DataFrame numericValues = FixtureSupport.GetDfWithStrongPositiveCorrelationFilter(_session);
            DoubleMetric attCorrelation = Correlation("att1", "att2", "att1 <= 20").Calculate(numericValues);
            DoubleMetric expected1 = DoubleMetric.Create(MetricEntity.Multicolumn, "Correlation", "att1,att2", 1.0);

            attCorrelation.MetricEntity.ShouldBe(expected1.MetricEntity);
            attCorrelation.Instance.ShouldBe(expected1.Instance);
            attCorrelation.Name.ShouldBe(expected1.Name);
            attCorrelation.Value.Get().ShouldBe(expected1.Value.Get(), 0.00001);
        }

        [Fact]
        public void fails_with_non_numeric_fields()
        {
            DataFrame numericValues = FixtureSupport.GetDFFull(_session);
            DoubleMetric attCorrelation = Correlation("att1", "att2").Calculate(numericValues);
            DoubleMetric expected1 = DoubleMetric.Create(MetricEntity.Multicolumn, "Correlation", "att1,att2", 0.0);

            attCorrelation.MetricEntity.ShouldBe(expected1.MetricEntity);
            attCorrelation.Instance.ShouldBe(expected1.Instance);
            attCorrelation.Name.ShouldBe(expected1.Name);
            attCorrelation.Value.Failure.HasValue.ShouldBeTrue();

        }

        [Fact]
        public void compute_combined_correlation_coefficient_with_a_partitioned_dataset()
        {
            (DataFrame dfA, DataFrame dfB) = FixtureSupport.GetDfWithStrongPositiveCorrelationPartitioned(_session);

            var corrA = Correlation("att1", "att2").ComputeStateFrom(dfA);
            var corrB = Correlation("att1", "att2").ComputeStateFrom(dfB);

            var corrAB = corrA.Value.Sum(corrB.Value);

            var totalCorr = Correlation("att1", "att2").ComputeStateFrom(dfA.Union(dfB));

            corrAB.Equals(totalCorr.Value).ShouldBeTrue();
        }
    }
}
