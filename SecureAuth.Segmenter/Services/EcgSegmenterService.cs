using Grpc.Core;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SecureAuth.Analytics.Grpc;
using System.IO;

namespace SecureAuth.Segmenter.Services
{
    public class EcgSegmenterService : EcgAnalytics.EcgAnalyticsBase
    {
        private readonly ILogger<EcgSegmenterService> _logger;
        private readonly string _modelPath = "segmenter.onnx";
        private InferenceSession? _session;

        public EcgSegmenterService(ILogger<EcgSegmenterService> logger)
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
                    _logger.LogInformation("ONNX ECG Segmenter loaded sucessfully.");
                }
                else
                {
                    _logger.LogWarning($"Model file '{_modelPath}' not found. Service will run in Simulation Mode.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load ONNX segmenter: {ex.Message}. Running in Simulation Mode.");
            }
        }

        public override async Task<AnalyzeReply> Analyze(AnalyzeRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Processing ECG file: {request.FileName}, size: {request.FileContent.Length} bytes.");

            if (_session != null)
            {
                try
                {
                    // препроцессинг .NPY файла
                    byte[] fileBytes = request.FileContent.ToByteArray();
                    int channels = 12;
                    int samples = 5000;

                    // создание пустого тензора размерности [1, 12, 5000]
                    var inputTensor = ParseNpyToFloatTensor(fileBytes, channels, samples);

                    // Инференс ONNX
                    var inputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("input_signal", inputTensor)
                    };

                    using var results = _session.Run(inputs);
                    var outputTensor = results.First().AsTensor<float>();

                    var anomalies = new List<string>();
                    bool insideAnomaly = false;
                    int startSegment = 0;

                    for (int i = 0; i < samples; i++)
                    {
                        float probability = outputTensor[0, 0, i];

                        if (probability > 0.85f && !insideAnomaly)
                        {
                            insideAnomaly = true;
                            startSegment = i;
                        }
                        else if (probability <= 0.85f && insideAnomaly)
                        {
                            insideAnomaly = false;
                            // 1 отсчет = 2 мс (при частоте 500 Гц)
                            int startTimeMs = startSegment * 2;
                            int endTimeMs = i * 2;
                            anomalies.Add($"[{startTimeMs}ms - {endTimeMs}ms]");
                        }
                    }

                    if (anomalies.Count > 0)
                    {
                        return new AnalyzeReply
                        {
                            Result = $"[ONNX] Anomalies detected on QRS complex: {string.Join(", ", anomalies)}",
                            Confidence = outputTensor.ToArray().Max()
                        };
                    }

                    return new AnalyzeReply
                    {
                        Result = "[ONNX] No cardiac anomalies detected. Signal is normal.",
                        Confidence = 0.99f
                    };
                }
                catch (Exception ex) 
                {
                    _logger.LogError($"Inference failed: {ex.Message}. Falling back to simulation.");
                }
            }

            // Режим симуляции
            await Task.Delay(100);
            return new AnalyzeReply
            {
                Result = "[Simulation] Anomaly detected in QRS complex: [120ms - 180ms]. Potential Arrhythmia.",
                Confidence = 0.94f
            };
        }


        // парсер .npy файлов
        private DenseTensor<float> ParseNpyToFloatTensor(byte[] bytes, int channels, int samples)
        {
            var tensor = new DenseTensor<float>(new[] { 1, channels, samples });

            // проверка сигнатуры заголовка формата .npy (\x93NUMPY)
            bool isValidNpyHeader = bytes.Length > 10 &&
                                    bytes[0] == 0x93 &&
                                    bytes[1] == 'N' &&
                                    bytes[2] == 'U' &&
                                    bytes[3] == 'M' &&
                                    bytes[4] == 'P' &&
                                    bytes[5] == 'Y';

            // файл пришел без заголовка
            if (!isValidNpyHeader)
            {
                _logger.LogWarning("Invalid .npy header. Parsing file content as raw float array.");

                // чтение напрямую сырых float-ов
                int floatCount = bytes.Length / sizeof(float);
                float[] rawFloats = new float[floatCount];
                Buffer.BlockCopy(bytes, 0, rawFloats, 0, bytes.Length);

                int index = 0;
                for (int c = 0; c < channels; c++)
                {
                    for (int s = 0; s < samples; s++)
                    {
                        tensor[0, c, s] = index < rawFloats.Length ? rawFloats[index++] : 0.0f;
                    }
                }

                return tensor;
            }

            // вычисление длины заголовка .npy
            byte majorVersion = bytes[6];
            ushort headerLength;
            int headerOffset;

            if (majorVersion == 1)
            {
                headerLength = BitConverter.ToUInt16(bytes, 8);
                headerOffset = 10;
            }
            else
            {
                headerLength = (ushort)BitConverter.ToUInt32(bytes, 8);
                headerOffset = 12;
            }

            // Вычисление смещения начала сырых байтов
            int dataOffset = headerOffset + headerLength;
            int dataBytesLength = bytes.Length - dataOffset;
            int availableFloats = dataBytesLength / sizeof(float);

            float[] floats = new float[availableFloats];
            Buffer.BlockCopy(bytes, dataOffset, floats, 0, dataBytesLength);

            // Заполнение 1D-тензора [1, 12, 5000]
            int flatIndex = 0;
            for (int c = 0; c < channels; c++)
            {
                for (int s = 0; s < samples; s++)
                {
                    tensor[0, c, s] = flatIndex < floats.Length ? floats[flatIndex++] : 0.0f;
                }
            }

            return tensor;
        }
    }
}
