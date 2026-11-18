using System;

namespace OpenDCP.Framework.Connections.Datasource.PostgreSQL.Models
{
    public class ValueWithNeighboursDTO
    {
        public string Id { get; set; }
        public DateTime ReferenceObsId { get; set; }
        public DateTime BeforeObsId { get; set; }
        public dynamic BeforeValue { get; set; }
        public DateTime AfterObsId { get; set; }
        public dynamic AfterValue { get; set; }
    }
}
