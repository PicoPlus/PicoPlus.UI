
using System.Text.Json.Serialization;

namespace PicoPlus.Models.CRM.Commerce
{
#nullable disable
    public class Products
    {
        public class Get
        {
            public class Response

            {
                  
                public List<Result> results { get; set; }
                  
                public Paging paging { get; set; }
                public class Result
                {
                    public string id { get; set; }
                    public Properties properties { get; set; }
                    public DateTime createdAt { get; set; }
                    public DateTime updatedAt { get; set; }
                    public bool archived { get; set; }
                }

                public class Properties
                {
                    public DateTime createdate { get; set; }
                    public object description { get; set; }
                    public DateTime hs_lastmodifieddate { get; set; }
                    public string hs_object_id { get; set; }
                    public string name { get; set; }
                    public string price { get; set; }

                    public string hs_sku { get; set; }
                }
            }

                public class Paging
                {
                    public Next next { get; set; }
                }

                public class Next
                {
                    public string after { get; set; }
                    public string link { get; set; }
                }

    


            
        }
        
    }
    
}



