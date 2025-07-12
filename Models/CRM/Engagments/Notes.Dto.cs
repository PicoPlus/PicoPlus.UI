namespace PicoPlus.Models.CRM.Engagements;


public partial class Notes
{
    public class ReadSingle
        {  
            public string id { get; set; }
            public Properties properties { get; set; }
              
            public DateTime createdAt { get; set; }
              
            public DateTime updatedAt { get; set; }
            public bool archived { get; set; }
            

            public class Properties
            {
                public string hs_attachment_ids { get; set; }
                public DateTime hs_createdate { get; set; }
                public DateTime hs_lastmodifieddate { get; set; }
                public object hs_note_body { get; set; }
                public string hs_object_id { get; set; }
            }

        }
    
}
