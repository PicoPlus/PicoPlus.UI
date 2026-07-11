#nullable enable

using PicoPlus.Application.Common.Interfaces;

namespace PicoPlus.Services.Identity;

/// <summary>
/// Implements IIdentityVerificationService by delegating to ZohalService.
/// </summary>
public class ZohalIdentityAdapter : IIdentityVerificationService
{
    private readonly ZohalService _zohal;

    public ZohalIdentityAdapter(ZohalService zohal) => _zohal = zohal;

    public async Task<IdentityVerificationResult> VerifyNationalIdentityAsync(
        string nationalCode, string birthDate)
    {
        try
        {
            var resp = await _zohal.NationalIdentityInquiryAsync(nationalCode, birthDate);

            if (resp?.Result != 1 || resp.ResponseBody?.Data == null)
                return new(false, null, null, null, null,
                    resp?.ResponseBody?.ErrorCode ?? resp?.ResponseBody?.Message ?? "No data returned");

            var data = resp.ResponseBody.Data;
            return new(
                IsValid    : data.Matched,
                FirstName  : data.FirstName,
                LastName   : data.LastName,
                FatherName : data.FatherName,
                BirthDate  : null);
        }
        catch (Exception ex)
        {
            return new(false, null, null, null, null, ex.Message);
        }
    }

    public async Task<ShahkarVerificationResult> VerifyPhoneOwnershipAsync(
        string nationalCode, string phoneNumber)
    {
        try
        {
            var resp = await _zohal.ShahkarInquiryAsync(nationalCode, phoneNumber);
            return new(resp?.ResponseBody?.Data?.Matched == true);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message);
        }
    }
}
