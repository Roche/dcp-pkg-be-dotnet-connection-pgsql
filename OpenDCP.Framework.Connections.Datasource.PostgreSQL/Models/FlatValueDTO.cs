using System;

namespace OpenDCP.Framework.Connections.Datasource.PostgreSQL.Models
{
    public class FlatValueDTO
    {
        public string Id { get; set; }

        public string? ChunkId { get; set; }

        public DateTime ObsId { get; set; }

        public bool Good { get; set; }

        public dynamic Value { get; set; }
    }
}
