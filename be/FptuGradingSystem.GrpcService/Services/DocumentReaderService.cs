using Grpc.Core;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FptuGradingSystem.GrpcService.Services
{
    public class DocumentReaderService : DocumentReader.DocumentReaderBase
    {
        private readonly ILogger<DocumentReaderService> _logger;

        public DocumentReaderService(ILogger<DocumentReaderService> logger)
        {
            _logger = logger;
        }

        public override Task<ReadDocumentResponse> ReadDocument(ReadDocumentRequest request, ServerCallContext context)
        {
            _logger.LogInformation("gRPC Request to read file: {FilePath} ({FileType})", request.FilePath, request.FileType);

            if (!File.Exists(request.FilePath))
            {
                return Task.FromResult(new ReadDocumentResponse
                {
                    Success = false,
                    ErrorMessage = $"File not found on disk at: {request.FilePath}"
                });
            }

            try
            {
                string content = string.Empty;
                string fileType = request.FileType.ToLower().TrimStart('.');

                if (fileType == "txt")
                {
                    content = File.ReadAllText(request.FilePath, Encoding.UTF8);
                }
                else if (fileType == "docx")
                {
                    content = ReadWordDocument(request.FilePath);
                }
                else
                {
                    content = $"[Unsupported format '{fileType}'. Cannot preview this file type directly.]";
                }

                return Task.FromResult(new ReadDocumentResponse
                {
                    Success = true,
                    Content = content
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read document file {FilePath}", request.FilePath);
                return Task.FromResult(new ReadDocumentResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private string ReadWordDocument(string filePath)
        {
            var sb = new StringBuilder();
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        sb.AppendLine(paragraph.InnerText);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
