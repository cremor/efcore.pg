using Npgsql.EntityFrameworkCore.PostgreSQL.Query;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime.Query.Internal;

public class NpgsqlNodaTimeAggregateMethodCallTranslatorPlugin : IAggregateMethodCallTranslatorPlugin
{
    public NpgsqlNodaTimeAggregateMethodCallTranslatorPlugin(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        if (sqlExpressionFactory is not NpgsqlSqlExpressionFactory npgsqlSqlExpressionFactory)
        {
            throw new ArgumentException($"Must be an {nameof(NpgsqlSqlExpressionFactory)}", nameof(sqlExpressionFactory));
        }

        Translators = new IAggregateMethodCallTranslator[]
        {
            new NpgsqlNodaTimeAggregateMethodTranslator(npgsqlSqlExpressionFactory, typeMappingSource)
        };
    }

    public virtual IEnumerable<IAggregateMethodCallTranslator> Translators { get; }
}

public class NpgsqlNodaTimeAggregateMethodTranslator : IAggregateMethodCallTranslator
{
    private static readonly bool[][] FalseArrays = { Array.Empty<bool>(), new[] { false } };

    private readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    public NpgsqlNodaTimeAggregateMethodTranslator(
        NpgsqlSqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _typeMappingSource = typeMappingSource;
    }

    public virtual SqlExpression? Translate(
        MethodInfo method,
        EnumerableExpression source,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (source.Selector is not SqlExpression sqlExpression || method.DeclaringType != typeof(NpgsqlNodaTimeDbFunctionsExtensions))
        {
            return null;
        }

        return method.Name switch
        {
            nameof(NpgsqlNodaTimeDbFunctionsExtensions.Sum) => _sqlExpressionFactory.AggregateFunction(
                "sum", new[] { sqlExpression }, source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: sqlExpression.Type, sqlExpression.TypeMapping),

            nameof(NpgsqlNodaTimeDbFunctionsExtensions.Average) => _sqlExpressionFactory.AggregateFunction(
                "avg", new[] { sqlExpression }, source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: sqlExpression.Type, sqlExpression.TypeMapping),

            nameof(NpgsqlNodaTimeDbFunctionsExtensions.RangeAgg) => _sqlExpressionFactory.AggregateFunction(
                "range_agg", new[] { sqlExpression }, source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: method.ReturnType, _typeMappingSource.FindMapping(method.ReturnType)),

            nameof(NpgsqlNodaTimeDbFunctionsExtensions.RangeIntersectAgg) => _sqlExpressionFactory.AggregateFunction(
                "range_intersect_agg", new[] { sqlExpression }, source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: sqlExpression.Type, sqlExpression.TypeMapping),

            _ => null
        };
    }
}
