using OpenDCP.Framework.Configuration.Model.Responses.Datasource;
using System.Collections.Generic;

namespace OpenDCP.Framework.Connections.Datasource.PostgreSQL.Models
{
    public class TimepointRawSensorDataResponse : DatasourceResponse
    {
        public List<ValueWithNeighboursDTO> DataPoints { get; set; } = new List<ValueWithNeighboursDTO>();
    }
}
