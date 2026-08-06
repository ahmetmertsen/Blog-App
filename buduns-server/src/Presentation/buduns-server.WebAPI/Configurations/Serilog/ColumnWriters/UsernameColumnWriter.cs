using NpgsqlTypes;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace buduns_server.WebAPI.Configurations.Serilog.ColumnWriters
{
    public class UsernameColumnWriter : ColumnWriterBase
    {
        public UsernameColumnWriter() : base(NpgsqlDbType.Varchar)
        {
        }

        public override object GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null)
        {
            if (logEvent.Properties.TryGetValue("user_name", out var value) && value is ScalarValue scalarValue)
            {
                return scalarValue.Value?.ToString() ?? (object)DBNull.Value;
            }

            return DBNull.Value;
        }
    }
}
