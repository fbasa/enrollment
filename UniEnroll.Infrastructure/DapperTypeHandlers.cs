using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

namespace UniEnroll.Infrastructure;

public static class DapperTypeHandlers
{
    private static bool _registered;

    public static void RegisterAll()
    {
        if (_registered) return;

        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyHandler());
        SqlMapper.AddTypeHandler(new NullableTimeOnlyHandler());

        _registered = true;
    }

    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            if (parameter is SqlParameter sp)
            {
                sp.SqlDbType = System.Data.SqlDbType.Date;
                sp.Value = value.ToDateTime(TimeOnly.MinValue); // SQL 'date' expects DateTime
            }
            else
            {
                parameter.DbType = DbType.Date;
                parameter.Value = value.ToDateTime(TimeOnly.MinValue);
            }
        }

        public override DateOnly Parse(object value) => value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateTimeOffset dto => DateOnly.FromDateTime(dto.DateTime),
            string s => DateOnly.Parse(s),
            _ => throw new DataException($"Cannot convert {value.GetType()} to DateOnly")
        };
    }

    private sealed class NullableDateOnlyHandler : SqlMapper.TypeHandler<DateOnly?>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        {
            if (value is null) { parameter.Value = DBNull.Value; return; }
            if (parameter is SqlParameter sp)
            {
                sp.SqlDbType = System.Data.SqlDbType.Date;
                sp.Value = value.Value.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                parameter.DbType = DbType.Date;
                parameter.Value = value.Value.ToDateTime(TimeOnly.MinValue);
            }
        }

        public override DateOnly? Parse(object value) =>
            value is null or DBNull ? null : new DateOnlyHandler().Parse(value);
    }

    private sealed class TimeOnlyHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
        {
            var ts = value.ToTimeSpan(); // SQL 'time' expects TimeSpan
            if (parameter is SqlParameter sp)
            {
                sp.SqlDbType = System.Data.SqlDbType.Time;
                sp.Value = ts;
            }
            else
            {
                parameter.DbType = DbType.Time;
                parameter.Value = ts;
            }
        }

        public override TimeOnly Parse(object value) => value switch
        {
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            string s => TimeOnly.Parse(s),
            _ => throw new DataException($"Cannot convert {value.GetType()} to TimeOnly")
        };
    }

    private sealed class NullableTimeOnlyHandler : SqlMapper.TypeHandler<TimeOnly?>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly? value)
        {
            if (value is null) { parameter.Value = DBNull.Value; return; }
            var ts = value.Value.ToTimeSpan();
            if (parameter is SqlParameter sp)
            {
                sp.SqlDbType = System.Data.SqlDbType.Time;
                sp.Value = ts;
            }
            else
            {
                parameter.DbType = DbType.Time;
                parameter.Value = ts;
            }
        }

        public override TimeOnly? Parse(object value) =>
            value is null or DBNull ? null : new TimeOnlyHandler().Parse(value);
    }
}
