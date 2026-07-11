
namespace PicoPlus.Models.CRM;

public partial class Associate
{


    public class Create
    {

        public class Rootobject
        {
            public List<Input> inputs { get; set; }
        }

        public class Input
        {
            public From from { get; set; }
            public To to { get; set; }
            public string type { get; set; }
            public int definitionId { get; set; }
        }

        public class From
        {
            public int id { get; set; }
        }

        public class To
        {
            public int id { get; set; }
        }

    }

    public class ListAssoc
    {

        public class Response
        {
            public class AssociationType
            {
                public string category { get; set; }
                public int typeId { get; set; }
                public object label { get; set; }
            }

            public class Result
            {
                public long toObjectId { get; set; }
                public List<AssociationType> associationTypes { get; set; }
            }
            public List<Result> results { get; set; }
        }


    }

}
