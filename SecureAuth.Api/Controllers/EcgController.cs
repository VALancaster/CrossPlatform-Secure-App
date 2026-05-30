using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAuth.Analytics.Grpc;

namespace SecureAuth.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EcgController : ControllerBase
    {
        private readonly EcgAnalytics.EcgAnalyticsClient _classifierClient;
        private readonly EcgAnalytics.EcgAnalyticsClient _segmenterClient;
        private readonly ILogger<EcgController> _logger;

        // Внедрение gRPC-клиентов через фабрику
        public EcgController(GrpcClientFactory grpcClientFactory, ILogger<EcgController> logger)
        {
            _logger = logger;
            _classifierClient = grpcClientFactory.CreateClient<EcgAnalytics.EcgAnalyticsClient>("ClassifierClient");
            _segmenterClient = grpcClientFactory.CreateClient<EcgAnalytics.EcgAnalyticsClient>("SegmenterClient");
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> UploadAndAnalyze(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            _logger.LogInformation($"Uploading file '{file.FileName}' for full analysis.");

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                var grpcRequest = new AnalyzeRequest
                {
                    FileContent = Google.Protobuf.ByteString.CopyFrom(fileBytes),
                    FileName = file.FileName
                };

                // параллельная отправка запросов в оба микросервиса
                var classificationTask = _classifierClient.AnalyzeAsync(grpcRequest).ResponseAsync;
                var segmentationTask = _segmenterClient.AnalyzeAsync(grpcRequest).ResponseAsync;

                // ожидание завершения обоих вычислений
                await Task.WhenAll(classificationTask, segmentationTask);

                var classResult = await classificationTask;
                var segResult = await segmentationTask;

                return Ok(new
                {
                    File = file.FileName,
                    Size = file.Length,
                    ClassifierResult = classResult.Result,
                    ClassifierConfidence = classResult.Confidence,
                    SegmenterResult = segResult.Result,
                    SegmenterConfidence = segResult.Confidence
                });
            }
            catch (Exception ex)
            { 
                _logger.LogError($"Analysis failed: {ex.Message}");
                return StatusCode(500, $"Internal server error during analysis: {ex.Message}");
            }
        }
    }
}
