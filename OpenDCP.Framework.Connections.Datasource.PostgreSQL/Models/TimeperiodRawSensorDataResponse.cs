using OpenDCP.Framework.Configuration.Model.Responses.Datasource;
using System.Collections.Generic;

namespace OpenDCP.Framework.Connections.Datasource.PostgreSQL.Models
{
    public class TimeperiodRawSensorDataResponse : DatasourceResponse
    {
        public List<FlatValueDTO> DataPoints { get; set; } = new List<FlatValueDTO>();
    }
}
