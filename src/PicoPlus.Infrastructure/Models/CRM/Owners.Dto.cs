namespace PicoPlus.Models.CRM;

public partial class Owners
{
    public class GetAll
    {
        public List<Result> results { get; set; }

        public class Result
        {
            public string id { get; set; }
            public string email { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public int userId { get; set; }
            public int userIdIncludingInactive { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public bool archived { get; set; }
        }

    }



}
