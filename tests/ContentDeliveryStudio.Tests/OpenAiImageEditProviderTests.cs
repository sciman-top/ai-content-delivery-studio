using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.References;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using SkiaSharp;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiImageEditProviderTests
{
    [Fact]
    public void Capabilities_AdvertiseBoundedSubjectReferenceAndMaskEditing()
    {
        var provider = CreateProvider(new CapturingHandler());

        Assert.True(provider.Capabilities.SupportsImageEditing);
        Assert.True(provider.Capabilities.SupportsReferenceImages);
        Assert.True(provider.Capabilities.SupportsMaskEditing);
        Assert.Equal(1, provider.Capabilities.MaxReferenceImageCount);
        Assert.Equal(50L * 1024 * 1024, provider.Capabilities.MaxReferenceImageBytes);
        Assert.Equal([ReferenceImageRole.Subject], provider.Capabilities.SupportedReferenceImageRoles);
    }

    [Fact]
    public async Task EditImageAsync_UsesCapturedMultipartTransportAndWritesPathSafeProvenance()
    {
        var root = CreateRoot();
        var sourcePath = WritePng(root, "private-source.png", 2, 2);
        var maskPath = WritePng(root, "private-mask.png", 2, 2);
        var outputDirectory = Path.Combine(root, "outputs");
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);
        var sourceId = Guid.NewGuid();
        var sourceHash = OpenAiImageEditProvider.ComputeSha256(sourcePath);

        try
        {
            var result = await provider.EditImageAsync(
                CreateRequest(sourceId, sourcePath, maskPath, sourceHash, outputDirectory),
                CancellationToken.None);

            Assert.Equal(1, handler.CallCount);
            Assert.Equal(HttpMethod.Post, handler.Method);
            Assert.Equal("https://captured.invalid/v1/images/edits", handler.RequestUri?.ToString());
            Assert.StartsWith("multipart/form-data", handler.ContentType?.MediaType, StringComparison.Ordinal);
            Assert.Contains("model", handler.PartNames);
            Assert.Contains("gpt-image-test", handler.Body, StringComparison.Ordinal);
            Assert.Contains("prompt", handler.PartNames);
            Assert.Contains("Preserve the subject and change the lighting.", handler.Body, StringComparison.Ordinal);
            Assert.Contains("image[]", handler.PartNames);
            Assert.Contains("mask", handler.PartNames);
            Assert.Contains("size", handler.PartNames);
            Assert.Contains("1024x1024", handler.Body, StringComparison.Ordinal);
            Assert.Contains("quality", handler.PartNames);
            Assert.Contains("output_format", handler.PartNames);
            Assert.True(File.Exists(result.AssetPath));
            Assert.NotEqual(Path.GetFullPath(sourcePath), Path.GetFullPath(result.AssetPath));

            var metadataText = await File.ReadAllTextAsync(result.MetadataPath);
            Assert.DoesNotContain(Path.GetFullPath(sourcePath), metadataText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Path.GetFullPath(maskPath), metadataText, StringComparison.OrdinalIgnoreCase);
            using var metadata = JsonDocument.Parse(metadataText);
            Assert.Equal("openai-image-edit", metadata.RootElement.GetProperty("providerId").GetString());
            Assert.Equal("edit", metadata.RootElement.GetProperty("operation").GetString());
            Assert.Equal(sourceHash, metadata.RootElement.GetProperty("sourceSha256").GetString());
            Assert.Equal(
                OpenAiImageEditProvider.ComputeSha256ForText("Preserve the subject and change the lighting."),
                metadata.RootElement.GetProperty("instructionSha256").GetString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("style")]
    [InlineData("multiple")]
    [InlineData("hash-drift")]
    [InlineData("overwrite")]
    [InlineData("mask-dimensions")]
    public async Task EditImageAsync_RejectsUnsupportedOrDestructiveRequestsBeforeTransport(string scenario)
    {
        var root = CreateRoot();
        var sourcePath = WritePng(root, "source.png", 2, 2);
        var maskPath = scenario == "mask-dimensions"
            ? WritePng(root, "mask.png", 3, 2)
            : WritePng(root, "mask.png", 2, 2);
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler);
        var sourceId = Guid.NewGuid();
        var sourceHash = OpenAiImageEditProvider.ComputeSha256(sourcePath);
        var request = CreateRequest(sourceId, sourcePath, maskPath, sourceHash, Path.Combine(root, "outputs"));
        request = scenario switch
        {
            "style" => request with
            {
                References = [new ImageEditReferenceInput(sourceId, ReferenceImageRole.Style, sourcePath, sourceHash)],
            },
            "multiple" => request with
            {
                References =
                [
                    new ImageEditReferenceInput(sourceId, ReferenceImageRole.Subject, sourcePath, sourceHash),
                    new ImageEditReferenceInput(Guid.NewGuid(), ReferenceImageRole.Style, sourcePath, sourceHash),
                ],
            },
            "hash-drift" => request with
            {
                References = [new ImageEditReferenceInput(sourceId, ReferenceImageRole.Subject, sourcePath, new string('0', 64))],
            },
            "overwrite" => request with
            {
                OutputDirectory = root,
                OutputFileName = "source.png",
            },
            _ => request,
        };

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.EditImageAsync(request, CancellationToken.None));
            Assert.Equal(0, handler.CallCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task EditImageAsync_RejectsDisabledRealApiBeforeTransport()
    {
        var root = CreateRoot();
        var sourcePath = WritePng(root, "source.png", 1, 1);
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler, realApiEnabled: false);
        var sourceId = Guid.NewGuid();

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EditImageAsync(
                CreateRequest(
                    sourceId,
                    sourcePath,
                    null,
                    OpenAiImageEditProvider.ComputeSha256(sourcePath),
                    Path.Combine(root, "outputs")),
                CancellationToken.None));
            Assert.Equal(0, handler.CallCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static OpenAiImageEditProvider CreateProvider(CapturingHandler handler, bool realApiEnabled = true)
    {
        return new OpenAiImageEditProvider(
            new HttpClient(handler),
            new OpenAiProviderOptions
            {
                BaseUri = new Uri("https://captured.invalid/v1/"),
                ImageGenerationModel = "gpt-image-test",
                RealApiEnabled = realApiEnabled,
            },
            new StaticSecretStore("captured-test-key"));
    }

    private static ImageEditRequest CreateRequest(
        Guid sourceId,
        string sourcePath,
        string? maskPath,
        string sourceHash,
        string outputDirectory)
    {
        return new ImageEditRequest(
            Guid.NewGuid(),
            sourceId,
            sourcePath,
            maskPath,
            "Preserve the subject and change the lighting.",
            new GenerationSettings(1024, 1024, "high", "png"),
            outputDirectory,
            "edited.png",
            References: [new ImageEditReferenceInput(sourceId, ReferenceImageRole.Subject, sourcePath, sourceHash)]);
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string WritePng(string root, string fileName, int width, int height)
    {
        var path = Path.Combine(root, fileName);
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }

    private sealed class StaticSecretStore(string? value) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(value);
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public MediaTypeHeaderValue? ContentType { get; private set; }

        public string Body { get; private set; } = string.Empty;

        public IReadOnlyList<string> PartNames { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            ContentType = request.Content?.Headers.ContentType;
            PartNames = request.Content is MultipartFormDataContent multipart
                ? multipart.Select(part => part.Headers.ContentDisposition?.Name?.Trim('"') ?? string.Empty).ToArray()
                : [];
            var bodyBytes = request.Content is null
                ? []
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            Body = Encoding.Latin1.GetString(bodyBytes);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"id\":\"edit-captured-123\",\"data\":[{\"b64_json\":\"iVBORw==\"}]}",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }
}
