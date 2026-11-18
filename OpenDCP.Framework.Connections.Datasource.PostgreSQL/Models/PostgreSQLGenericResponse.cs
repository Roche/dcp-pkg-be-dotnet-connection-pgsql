using OpenDCP.Framework.Configuration.Model.Responses.Datasource;
using System.Collections.Generic;

namespace OpenDCP.Framework.Connections.Datasource.PostgreSQL.Models
{
    public class PostgreSQLGenericResponse<T> : DatasourceResponse
    {
        public List<T> Data { get; set; } = new List<T>();
    }
}
