using System.Net.Http;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM
{
    public class Pipelines
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _hubspotToken;

        public Pipelines(IHttpClientFactory httpClientFactory, HubSpotTokenProvider tokenProvider)
        {
            _httpClientFactory = httpClientFactory;
            _hubspotToken = tokenProvider.Token;
        }

        public async Task<Models.CRM.Pipelines.List> GetPipelines(string objectType)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"/crm/v3/pipelines/{objectType}";
            return await HubSpotRequestHelper.GetAsync<Models.CRM.Pipelines.List>(httpClient, url, _hubspotToken);
        }

        public async Task<Models.CRM.Pipelines.GetPipelineByStageID> GetStagesByPipID(string objectName, string stageID)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"/crm/v3/pipelines/{objectName}/default/stages/{stageID}";
            return await HubSpotRequestHelper.GetAsync<Models.CRM.Pipelines.GetPipelineByStageID>(httpClient, url, _hubspotToken);
        }

        public async Task<Models.CRM.Pipelines.GetStages> GetDealStages()
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = "/crm/v3/pipelines/deals/default/stages";
            return await HubSpotRequestHelper.GetAsync<Models.CRM.Pipelines.GetStages>(httpClient, url, _hubspotToken);
        }
    }
}
