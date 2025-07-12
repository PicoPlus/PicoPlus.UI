
namespace PicoPlus.Models.Services.SMS;
public interface IMessageModel<TInputdata>
{

    string op { get; set; } 
    string pass { get; set; }
    string fromNum { get; set; }
    string toNum { get; set; }
    string patternCode { get; set; }
    List<TInputdata> inputData { get; set; }
}

public partial class SMS
{
    public class Inputdata
    {
        // Common properties for Inputdata
    }

    public class WelcomeNewInputdata : Inputdata
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string cid { get; set; }
    }

    public class DealClosedWonInputdata : Inputdata
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
       public string id { get; set; }
    }
    public class DealQualifiedToBuyInputdata : Inputdata
    {
        public string id { get; set; }
    }
    public class Deal : Inputdata
    {

    }

    public class SenOTPInputdata : Inputdata
    {
        public string otp { get; set; }
    }

    public class Send<TInputdata> : IMessageModel<TInputdata>
        where TInputdata : Inputdata
    {
        public string op { get; set; }
        public string user { get; set; } 
        public string pass { get; set; }
        public string fromNum { get; set; } 
        public string toNum { get; set; } 
        public string patternCode { get; set; }
        public List<TInputdata> inputData { get; set; }
    }

    public class WelcomeNew : Send<WelcomeNewInputdata>
    {
        // Additional properties/methods specific to WelcomeNew
    }

    public class DealClosedWon : Send<DealClosedWonInputdata>
    {
        // Additional properties/methods specific to DealClosedWon
    }
    public class DealQualifiedToBuy : Send<DealQualifiedToBuyInputdata>
    {
        // Additional properties/methods specific to DealClosedWon
    }
    public class SenOTP : Send<SenOTPInputdata>
    {
    }
}
