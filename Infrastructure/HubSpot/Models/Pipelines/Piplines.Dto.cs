namespace PicoPlus.Models.CRM
{
    public partial class Pipelines
    {
        public class List
        {
            public List<Result> results { get; set; }


            public class Result
            {
                public string label { get; set; }
                public int displayOrder { get; set; }
                public string id { get; set; }
                public Stage[] stages { get; set; }
                public DateTime createdAt { get; set; }
                public DateTime updatedAt { get; set; }
                public bool archived { get; set; }
            }

            public class Stage
            {
                public string label { get; set; }
                public int displayOrder { get; set; }
                public Metadata metadata { get; set; }
                public string id { get; set; }
                public DateTime createdAt { get; set; }
                public DateTime updatedAt { get; set; }
                public bool archived { get; set; }
                public string writePermissions { get; set; }
            }

            public class Metadata
            {
                public string isClosed { get; set; }
                public string probability { get; set; }
            }

        }

        public class GetPipelineByStageID
        {
            public string label { get; set; }
            public int displayOrder { get; set; }
            public Metadata metadata { get; set; }
            public string id { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public bool archived { get; set; }
            public string writePermissions { get; set; }

            public class Metadata
            {
                public string isClosed { get; set; }
                public string probability { get; set; }
            }


        }

        public class GetStages()
        {
            public List<Result> results { get; set; }


            public class Result
            {
                public string label { get; set; }
                public int displayOrder { get; set; }
                public Metadata metadata { get; set; }
                public string id { get; set; }
                public DateTime createdAt { get; set; }
                public DateTime updatedAt { get; set; }
                public bool archived { get; set; }
                public string writePermissions { get; set; }
            }

            public class Metadata
            {
                public string isClosed { get; set; }
                public string probability { get; set; }
            }


        }

    }
}
