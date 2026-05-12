using System.Data;
using Dapper;

namespace DocumentTracker.Repositories;

public static class DapperDateOnlyTypeHandler
{
    private static readonly SqlMapper.ITypeHandler Handler = new DateOnlyHandler();
    private static int _isRegistered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _isRegistered, 1) == 1)
        {
            return;
        }

        SqlMapper.AddTypeHandler(typeof(DateOnly), Handler);
        SqlMapper.AddTypeHandler(typeof(DateOnly?), Handler);
    }

    private sealed class DateOnlyHandler : SqlMapper.ITypeHandler
    {
        public object Parse(Type destinationType, object value)
        {
            if (value is DateOnly dateOnly)
            {
                return dateOnly;
            }

            if (value is DateTime dateTime)
            {
                return DateOnly.FromDateTime(dateTime);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return DateOnly.FromDateTime(dateTimeOffset.DateTime);
            }

            if (value is string text && DateOnly.TryParse(text, out var parsedDate))
            {
                return parsedDate;
            }

            throw new DataException($"Cannot convert {value.GetType()} to {destinationType}.");
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            parameter.DbType = DbType.Date;

            if (value is null || value is DBNull)
            {
                parameter.Value = DBNull.Value;
                return;
            }

            if (value is DateOnly dateOnly)
            {
                parameter.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                return;
            }

            throw new DataException($"Cannot set parameter value of type {value.GetType()} as DateOnly.");
        }
    }
}