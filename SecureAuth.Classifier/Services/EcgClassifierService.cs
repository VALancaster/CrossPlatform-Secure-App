using Grpc.Core;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SecureAuth.Analytics.Grpc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Xml;

namespace SecureAuth.Classifier.Services
{
    public class EcgClassifierService : EcgAnalytics.EcgAnalyticsBase
    {
        private readonly ILogger<EcgClassifierService> _logger;
        private readonly string _modelPath = "mnist.onnx";
        private InferenceSession? _session;

        public EcgClassifierService(ILogger<EcgClassifierService> logger)
        {
            _logger = logger;
            InitializeModel();
        }

        private void InitializeModel()
        {
            try
            {
                if (File.Exists(_modelPath))
                {
                    _session = new InferenceSession(_modelPath);
                    _logger.LogInformation("ONNX MNIST Model loaded sucessfully.");
                }
                else
                {
                    _logger.LogWarning($"Model file '{_modelPath}' not found. Service will run in Simulation Mode.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load ONNX model: {ex.Message}. Running in Simulation Mode.");
            }
        }

        public override async Task<AnalyzeReply> Analyze(AnalyzeRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Received file: {request.FileName}, size: {request.FileContent.Length} bytes.");

            if (_session != null)
            {
                try
                {
                    // препроцессинг изображения
                    byte[] imageBytes = request.FileContent.ToByteArray();

                    // загрузка изображения как 8-битное серое 
                    using var image = Image.Load<L8>(imageBytes);

                    // принудительное изменение размера нп 28x28 (стандарт MNIST)
                    image.Mutate(x => x.Resize(28, 28));

                    // Реальный вызов нейросети через ONNX Runtime
                    var inputTensor = new DenseTensor<float>(new[] { 1, 1, 28, 28 });

                    // Заполнение тензора нормализованными пикселями
                    for (int y = 0; y < 28; y++)
                    {
                        for (int x = 0; x < 28; x++)
                        {
                            L8 pixel = image[x, y];
                            float normalizedValue = pixel.PackedValue / 255.0f;
                            inputTensor[0,0,y,x] = normalizedValue;
                        }
                    }

                    // Инференс ONNX
                    var inputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("Input3", inputTensor)
                    };

                    using var results = _session.Run(inputs);
                    var output = results.First().AsEnumerable<float>().ToArray();

                    int predictedDigit = Array.IndexOf(output, output.Max());

                    float confidence = output.Max();

                    _logger.LogInformation($"Successfully predicted digit: {predictedDigit} with confidence {confidence:P2}");

                    return new AnalyzeReply
                    {
                        Result = $"[ONNX] Recognized Digit: {predictedDigit}",
                        Confidence = confidence
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Inference failed: {ex.Message}. Falling back to simulation.");
                }
            }

            // Режим симуляции
            await Task.Delay(100);
            var randomDigit = new Random().Next(0, 10);
            return new AnalyzeReply
            {
                Result = $"[Simulation] Recognized Digit: {randomDigit}",
                Confidence = 0.89f
            };
        }
    }
}
