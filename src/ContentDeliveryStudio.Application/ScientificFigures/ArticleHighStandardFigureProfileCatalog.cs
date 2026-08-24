namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Single source of truth for the deterministic high-standard profiles that
/// originate in the article corpus. A contract names the scientific labels,
/// visual roles, concrete apparatus, and causal connections a renderer must
/// make visible; neither a role count nor prose alone is sufficient.
/// </summary>
public sealed record ArticleHighStandardFigureProfile(
    string PackageId,
    IReadOnlyList<string> TitleMarkers,
    IReadOnlyDictionary<ArticleScientificFigureCandidateKind, ArticleHighStandardFigureCandidateContract> CandidateContracts);

public sealed record ArticleHighStandardFigureCandidateContract(
    ArticleScientificFigureCandidateKind CandidateKind,
    IReadOnlyList<string> RequiredLabels,
    IReadOnlyList<string> RequiredRoles,
    IReadOnlyDictionary<string, int> MinimumRoleCounts,
    int MinimumGraphicCount,
    IReadOnlyList<string> RequiredConcreteObjectRoles,
    IReadOnlyList<string> RequiredConnectionIds,
    IReadOnlyList<string> CounterMisreadings);

public sealed record ArticleHighStandardEffectiveContract(
    string PackageId,
    ArticleScientificFigureCandidateKind CandidateKind,
    IReadOnlyList<string> RequiredLabels,
    IReadOnlyList<string> RequiredRoles,
    IReadOnlyDictionary<string, int> MinimumRoleCounts,
    int MinimumGraphicCount,
    IReadOnlyList<string> RequiredConcreteObjectRoles,
    IReadOnlyList<string> RequiredConnectionIds,
    IReadOnlyList<string> CounterMisreadings);

public static class ArticleHighStandardFigureProfileCatalog
{
    private static readonly IReadOnlyList<ArticleHighStandardFigureProfile> Profiles =
    [
        Profile(
            "article-bernoulli-v1", ["伯努利", "电吹风"],
            Contract(ArticleScientificFigureCandidateKind.BernoulliFanEnergy, ["风机做功", "电功", "总能"], ["intake-flow", "fan-blade", "electrical-work", "outlet-flow", "fan-body", "outlet-channel"], 20, Counts(("intake-flow", 3), ("fan-blade", 4), ("electrical-work", 1), ("outlet-flow", 3), ("fan-body", 1), ("outlet-channel", 1))),
            Contract(ArticleScientificFigureCandidateKind.BernoulliFanZones, ["吸风区", "压缩区", "风机"], ["duct-wall", "suction-flow", "compression-flow", "fan-body", "fan-blade"], 20, Counts(("duct-wall", 2), ("suction-flow", 3), ("compression-flow", 3), ("fan-body", 1), ("fan-blade", 3))),
            Contract(ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary, ["同一流线", "大气压"], ["same-streamline", "free-jet", "comparison-boundary", "throat-section", "duct-wall"], 16, Counts(("duct-wall", 4), ("same-streamline", 6), ("free-jet", 2), ("comparison-boundary", 1), ("throat-section", 1)))),
        Profile(
            "article-pinhole-v1", ["小孔成像", "小孔"],
            Contract(ArticleScientificFigureCandidateKind.PinholeGeometry, ["小孔", "倒立实像", "可视范围"], ["object", "barrier", "principal-ray", "image-plane", "inverted-image"], 15, Counts(("object", 3), ("barrier", 3), ("principal-ray", 4), ("image-plane", 1), ("inverted-image", 3))),
            Contract(ArticleScientificFigureCandidateKind.PinholeFocusPlane, ["小孔处", "光源处", "像位置"], ["focus-plane", "ray", "camera-body", "camera-input-ray", "camera-focused-ray", "sensor"], 20, Counts(("focus-plane", 3), ("ray", 4), ("camera-body", 1), ("camera-input-ray", 2), ("camera-focused-ray", 2), ("sensor", 1))),
            Contract(ArticleScientificFigureCandidateKind.PinholeObservation, ["近距", "远距", "全景"], ["barrier", "near-aperture", "far-aperture", "near-field", "far-field", "near-object", "far-object", "near-camera", "far-camera"], 28, Counts(("barrier", 2), ("near-aperture", 1), ("far-aperture", 1), ("near-field", 3), ("far-field", 3), ("near-object", 1), ("far-object", 1), ("near-camera", 1), ("far-camera", 1)))),
        Profile(
            "article-superconducting-v1", ["超导磁体", "超导"],
            Contract(ArticleScientificFigureCandidateKind.SuperconductingEnergy, ["电能", "磁能", "电流变化"], ["circuit", "switch", "magnetic-field", "power-source", "coil", "electrical-work"], 20, Counts(("circuit", 7), ("switch", 2), ("magnetic-field", 4), ("power-source", 1), ("coil", 6), ("electrical-work", 1))),
            Contract(ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent, ["闭合通路", "撤去励磁电源", "恒定电流"], ["charging-loop", "charging-coil", "persistent-current", "power-source"], 20, Counts(("charging-loop", 6), ("charging-coil", 3), ("persistent-current", 4), ("power-source", 1))),
            Contract(ArticleScientificFigureCandidateKind.SuperconductingExcitation, ["heater", "超导开关", "励磁电源", "液氦"], ["excitation-circuit", "persistent-switch-branch", "superconducting-switch", "heater-circuit", "heater-element", "thermal-coupling", "cryostat", "main-coil"], 20, Counts(("excitation-circuit", 8), ("persistent-switch-branch", 2), ("superconducting-switch", 1), ("heater-circuit", 2), ("heater-element", 1), ("thermal-coupling", 1), ("cryostat", 1), ("main-coil", 6)))),
        Profile(
            "article-meter-trial-v1", ["试触", "电表"],
            Contract(ArticleScientificFigureCandidateKind.MeterTransientResponse, ["指针示值", "量程上限", "立即断开", "稳定示值"], ["time-axis", "reading-axis", "range-limit", "damped-response", "danger-response", "steady-window"], 15, Counts(("damped-response", 7), ("range-limit", 1), ("danger-response", 1)), ["meter-body", "meter-terminal", "switch-control", "low-voltage-board", "operator-hand"], ["board-meter-loop", "hand-switch-control"], ["把暂态峰值当作稳态读数", "把试触误读为无条件的一触即断"]),
            Contract(ArticleScientificFigureCandidateKind.MeterTrialDecision, ["先预估", "立即断开", "基本稳定", "低压课堂实验"], ["decision-step", "workflow-link", "abort-branch", "stable-branch", "abort-decision", "stable-decision"], 15, Counts(("decision-step", 4), ("workflow-link", 3), ("abort-branch", 1), ("stable-branch", 1)), ["meter-body", "meter-terminal", "switch-control", "low-voltage-board", "operator-hand"], ["board-meter-loop", "hand-switch-control"], ["把文字流程卡当作真实接线", "忽略手与开关的可操作关系"]),
            Contract(ArticleScientificFigureCandidateKind.MeterProtectionLayers, ["预防层", "测量层", "保护层", "先断电"], ["prevention-layer", "measurement-layer", "protection-layer", "layer-link"], 11, Counts(("prevention-layer", 3), ("measurement-layer", 3), ("protection-layer", 3), ("layer-link", 2)), ["meter-body", "meter-terminal", "switch-control", "low-voltage-board", "operator-hand"], ["board-meter-loop", "hand-switch-control"], ["用保护层文字替代真实保护关系", "把操作顺序误解为可忽略的装置条件"])),
        Profile(
            "article-boiling-bubbles-v1", ["沸腾", "气泡"],
            Contract(ArticleScientificFigureCandidateKind.BoilingPreBubbleCollapse, ["上层较冷", "凝结", "溶解气体", "半径缩小"], ["vessel", "cool-water", "hot-water", "shrinking-bubble", "rise-path", "condensation-flux", "origin-caveat"], 18, Counts(("shrinking-bubble", 4), ("rise-path", 3), ("condensation-flux", 2)), ["beaker-glass", "heater", "thermometer", "water-surface"], ["heat-to-water", "thermometer-in-water"], ["把所有小气泡都当作水蒸气泡", "把上升变小只归因于压强"]),
            Contract(ArticleScientificFigureCandidateKind.BoilingBubbleGrowth, ["净汽化", "泡内蒸气压", "表面张力", "沸腾判据"], ["vessel", "growing-bubble", "rise-path", "vaporization-flux", "pressure-balance"], 17, Counts(("growing-bubble", 4), ("rise-path", 3), ("vaporization-flux", 4)), ["beaker-glass", "heater", "thermometer", "water-surface"], ["heat-to-water", "thermometer-in-water"], ["把气泡变大只归因于外压下降", "忽略相变质量交换"]),
            Contract(ArticleScientificFigureCandidateKind.BoilingPressureScale, ["0.98 kPa", "101 kPa", "蒸气质量", "表面张力"], ["pressure-column", "water-depth", "reference-bubble", "bubble-rise", "depth-bracket", "causal-balance", "causal-factor", "scale-link"], 15, Counts(("reference-bubble", 2), ("bubble-rise", 1), ("causal-factor", 4), ("pressure-column", 1), ("scale-link", 1)), ["beaker-glass", "heater", "thermometer", "water-surface"], ["heat-to-water", "thermometer-in-water"], ["把定量浅水压强差夸大为全部原因", "把数量卡片误认作真实沸腾装置"])),
        Profile(
            "article-galilean-eyepiece-v1", ["伽利略", "望远镜"],
            Contract(ArticleScientificFigureCandidateKind.GalileanAfocalPath, ["物镜", "目镜", "焦平面", "视网膜"], ["incoming-ray", "objective-convergence", "would-be-focus", "would-be-focus-point", "afocal-output", "common-focal-plane", "eye"], 22, Counts(("incoming-ray", 3), ("objective-convergence", 3), ("would-be-focus", 3), ("would-be-focus-point", 1), ("afocal-output", 3)), ["telescope-tube", "objective-mount", "eyepiece-mount", "distant-object", "observer-eye"], ["objective-eyepiece-tube", "eyepiece-eye-path"], ["让实线光束穿过凹目镜后仍指向原焦点", "把虚焦点误读为可放置光屏的实像"]),
            Contract(ArticleScientificFigureCandidateKind.GalileanVirtualObjectRegimes, ["s < F", "s = F", "s > F", "平行出射"], ["regime-panel", "converging-input", "regime-output", "image-point"], 30, Counts(("regime-panel", 3), ("converging-input", 6), ("regime-output", 6), ("image-point", 2)), ["telescope-tube", "objective-mount", "eyepiece-mount", "distant-object", "observer-eye"], ["objective-eyepiece-tube", "eyepiece-eye-path"], ["把三个目镜物距区间都误作正常无焦工作", "把虚像点误读为实际会聚点"]),
            Contract(ArticleScientificFigureCandidateKind.GalileanAngularMagnification, ["角放大率", "焦距", "无焦条件", "放松观察"], ["optical-axis", "angle-ray", "afocal-ray", "tube-length", "magnification-card"], 16, Counts(("angle-ray", 2), ("afocal-ray", 2), ("tube-length", 3)), ["telescope-tube", "objective-mount", "eyepiece-mount", "distant-object", "observer-eye"], ["objective-eyepiece-tube", "eyepiece-eye-path"], ["把镜筒长度当作放大率", "把焦点重合理解为焦距相等"])),
    ];

    public static IReadOnlyCollection<string> PackageIds => Profiles.Select(profile => profile.PackageId).ToArray();

    public static bool IsSupportedPackage(string packageId) =>
        Profiles.Any(profile => string.Equals(profile.PackageId, packageId, StringComparison.Ordinal));

    public static bool TryResolve(ArticleScientificFigureCandidate candidate, out ArticleHighStandardFigureProfile profile)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var resolved = Profiles.FirstOrDefault(item => item.CandidateContracts.ContainsKey(candidate.Kind))
            ?? Profiles.FirstOrDefault(item => candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard
                && item.TitleMarkers.Any(marker => candidate.ArticleTitle.Contains(marker, StringComparison.Ordinal)));
        if (resolved is null)
        {
            profile = null!;
            return false;
        }

        profile = resolved;
        return true;
    }

    public static ArticleHighStandardEffectiveContract? TryGetEffectiveContract(ArticleScientificFigureCandidate candidate)
    {
        if (!TryResolve(candidate, out var profile)
            || !profile.CandidateContracts.TryGetValue(candidate.Kind, out var contract)) return null;
        return new ArticleHighStandardEffectiveContract(profile.PackageId, contract.CandidateKind, contract.RequiredLabels, contract.RequiredRoles, contract.MinimumRoleCounts, contract.MinimumGraphicCount, contract.RequiredConcreteObjectRoles, contract.RequiredConnectionIds, contract.CounterMisreadings);
    }

    private static ArticleHighStandardFigureProfile Profile(string packageId, IReadOnlyList<string> titleMarkers, params ArticleHighStandardFigureCandidateContract[] contracts) =>
        new(packageId, titleMarkers, contracts.ToDictionary(contract => contract.CandidateKind));

    private static ArticleHighStandardFigureCandidateContract Contract(ArticleScientificFigureCandidateKind kind, IReadOnlyList<string> labels, IReadOnlyList<string> roles, int minimumGraphicCount, IReadOnlyDictionary<string, int> minimumRoleCounts, IReadOnlyList<string>? concreteObjects = null, IReadOnlyList<string>? connections = null, IReadOnlyList<string>? counterMisreadings = null) =>
        new(kind, labels, roles, minimumRoleCounts, minimumGraphicCount, concreteObjects ?? [], connections ?? [], counterMisreadings ?? []);

    private static IReadOnlyDictionary<string, int> Counts(params (string Role, int Count)[] values) =>
        values.ToDictionary(value => value.Role, value => value.Count, StringComparer.Ordinal);
}
