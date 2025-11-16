namespace PicoPlus.Models.CRM.Objects;

public partial class Contact
{
#nullable disable
    public class Base
    {
        public class Response

        {
            public string id { get; set; }
            public Properties properties { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public bool archived { get; set; }
            public class Properties

            {

                public string email { get; set; }
                public string firstname { get; set; }

                public string lastname { get; set; }
                public string phone { get; set; }
                public string natcode { get; set; }
                public string dateofbirth { get; set; }
                public string father_name { get; set; }
                public string total_revenue { get; set; }

                public string shahkar_status { get; set; }
                public string wallet { get; set; }
                public string num_associated_deals { get; set; }
                public string contact_plan { get; set; }
                public string gender { get; set; }
                /// <summary>Avatar/National Card Image URL</summary>
                public string last_products_bought_product_1_image_url { get; set; }
            }
        }
    }

    public class Search

    {
        public class Request
        {
            public string query { get; set; }
            public int limit { get; set; }
            public object[] sorts { get; set; }
            public string[] properties { get; set; }
            public FilterGroup[] filterGroups { get; set; }

            public class Filter
            {
                public string highValue { get; set; }
                public string propertyName { get; set; }
                public string value { get; set; }


                public string @operator { get; set; }
            }

            public class FilterGroup
            {
                public Filter[] filters { get; set; }
            }
        }
        public class Response

        {
            public int total { get; set; }

            public Paging paging { get; set; }

            public List<Result> results { get; set; }


            public class Paging
            {
                public Next next { get; set; }
            }

            public class Next
            {
                public string after { get; set; }
                public string link { get; set; }
            }

            public class Result
            {
                public string id { get; set; }
                public Properties properties { get; set; }
                public string createdAt { get; set; }
                public string updatedAt { get; set; }
                public bool archived { get; set; }
                public class Properties : Read.Response.Properties { }


            }


        }

    }

    public class Create
    {
        public class Request
        {
            public Properties properties { get; set; }

            public class Properties : Read.Response.Properties { }
        }

        public class Response : Base.Response { }


    }

    public class Read : Base

    {
        public class Response : Base.Response { public class Properties : Base.Response.Properties { } }
    }


}

















