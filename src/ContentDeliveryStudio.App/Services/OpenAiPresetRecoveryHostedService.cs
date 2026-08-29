using ContentDeliveryStudio.Infrastructure.OpenAI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentDeliveryStudio.App.Services;

/// <summary>
/// Owns the low-frequency recovery schedule. The controller owns all routing,
/// cooldown, and promotion decisions; this host adapter only supplies time.
/// </summary>
public sealed class OpenAiPresetRecoveryHostedService : BackgroundService
{
    private readonly IOpenAiPresetRecoveryController _controller;
    private readonly ILogger<OpenAiPresetRecoveryHostedService> _logger;

    public OpenAiPresetRecoveryHostedService(
        IOpenAiPresetRecoveryController controller,
        ILogger<OpenAiPresetRecoveryHostedService> logger)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_controller.PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var decision = await _controller.TryRecoverAsync(stoppingToken);
                if (decision.Kind is OpenAiPresetRecoveryDecisionKind.Promoted)
                {
                    _logger.LogInformation(
                        "Promoted the active OpenAI preset set to {PresetSet} after {SuccessCount} successful recovery canaries.",
                        decision.ActivePresetSet,
                        decision.SuccessfulCanaries);
                }
                else if (decision.Kind is OpenAiPresetRecoveryDecisionKind.CandidateUnhealthy
                    or OpenAiPresetRecoveryDecisionKind.CandidateUnavailable)
                {
                    _logger.LogWarning(
                        "OpenAI preferred-preset recovery check ended as {Decision}; next eligible probe is {NextEligibleProbeAt}.",
                        decision.Kind,
                        decision.NextEligibleProbeAt);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "OpenAI preferred-preset recovery check failed unexpectedly.");
            }
        }
    }
}
