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
        Profile(
            "article-dry-ice-v1", ["干冰"],
            Contract(ArticleScientificFigureCandidateKind.DryIceWaterMechanism, ["干冰", "水", "二氧化碳气泡", "冰晶", "冰壳", "白烟≠液态水雾"], ["beaker", "water", "dry-ice", "co2-bubble", "ice-crystal", "ice-shell", "crystal-egress", "dry-ice-water-contact"], 24, Counts(("beaker", 1), ("water", 1), ("dry-ice", 1), ("co2-bubble", 4), ("ice-crystal", 5), ("ice-shell", 1), ("crystal-egress", 3), ("dry-ice-water-contact", 1)), ["beaker", "water", "dry-ice", "ice-shell"], ["dry-ice-water-contact", "gas-carries-crystals"], ["把白烟画成液态水雾", "把气泡与冰晶画成同一物质"]),
            Contract(ArticleScientificFigureCandidateKind.DryIceHeatTransferComparison, ["空气", "水", "油/酒精", "传热速率", "白烟", "清澈气泡"], ["comparison-panel", "air-vessel", "water-vessel", "oil-vessel", "dry-ice", "heat-arrow", "smoke-crystal", "clear-bubble"], 24, Counts(("comparison-panel", 3), ("air-vessel", 1), ("water-vessel", 1), ("oil-vessel", 1), ("dry-ice", 3), ("heat-arrow", 6), ("smoke-crystal", 4), ("clear-bubble", 4)), ["air-vessel", "water-vessel", "oil-vessel", "dry-ice"], ["air-heat", "water-heat", "oil-heat"], ["把背景颜色当作传热证据", "把油/酒精中的清澈气泡误作冰晶"]),
            Contract(ArticleScientificFigureCandidateKind.DryIceIsolationVerification, ["薄塑料袋", "小孔", "袋外结冰", "清澈二氧化碳气泡", "隔离接触"], ["water-bath", "plastic-bag", "dry-ice", "vent-hole", "clear-bubble", "ice-on-bag", "isolation-barrier"], 22, Counts(("water-bath", 1), ("plastic-bag", 1), ("dry-ice", 1), ("vent-hole", 1), ("clear-bubble", 4), ("ice-on-bag", 3), ("isolation-barrier", 1)), ["water-bath", "plastic-bag", "dry-ice", "vent-hole"], ["bag-isolates-contact", "gas-escapes-hole"], ["把袋内接触画成水直接接触干冰", "把清澈气泡画成白烟"])),
        Profile(
            "article-lever-forces-v1", ["杠杆动力", "阻力"],
            Contract(ArticleScientificFigureCandidateKind.LeverRockContact, ["支点 O", "石块", "地面", "压力", "F压", "摩擦", "f", "阻力合力", "F阻", "向左下"], ["ground", "rock", "lever", "pivot", "normal-force", "friction-force", "resultant-force", "rock-slide"], 24, Counts(("ground", 1), ("rock", 1), ("lever", 1), ("pivot", 1), ("normal-force", 1), ("friction-force", 1), ("resultant-force", 1), ("rock-slide", 2)), ["ground", "rock", "lever", "pivot"], ["lever-rock-contact", "ground-rock-contact"], ["把阻力固定为竖直向下", "忽略翻转时的相对滑动"]),
            Contract(ArticleScientificFigureCandidateKind.LeverSeesawFriction, ["人体", "杠杆", "重力分量 G2", "静摩擦 f1", "滑动摩擦 f2", "f2 < G2"], ["seesaw", "person", "pivot", "gravity-component", "static-friction", "kinetic-friction", "force-resultant", "sliding-state"], 22, Counts(("seesaw", 1), ("person", 1), ("pivot", 1), ("gravity-component", 2), ("static-friction", 1), ("kinetic-friction", 1), ("force-resultant", 2), ("sliding-state", 1)), ["seesaw", "person", "pivot"], ["person-seesaw-contact", "friction-balances-component"], ["把摩擦方向画反", "把滑动摩擦仍画成等于重力分量"]),
            Contract(ArticleScientificFigureCandidateKind.LeverTwoForceMember, ["二力平衡", "直杆 AC", "弯曲撑杆", "沿杆方向", "约束力合力"], ["straight-member", "bent-member", "support", "force-along-member", "constraint-force", "crane-arm", "two-force-line"], 22, Counts(("straight-member", 1), ("bent-member", 1), ("support", 2), ("force-along-member", 2), ("constraint-force", 2), ("crane-arm", 1), ("two-force-line", 1)), ["straight-member", "bent-member", "support", "crane-arm"], ["two-force-collinear", "bent-member-to-arm"], ["把所有杆力都画成沿杆", "把二力杆结论推广到多约束弯杆"])),
        Profile(
            "article-rest-definition-v1", ["静止"],
            Contract(ArticleScientificFigureCandidateKind.RestIntervalDefinition, ["时间区间", "同一参考系", "位置不变", "x(t)=x0", "Δx=0"], ["time-axis", "rest-positions", "motion-positions", "reference-frame", "interval-bracket"], 20, Counts(("time-axis", 1), ("rest-positions", 4), ("motion-positions", 4), ("reference-frame", 1), ("interval-bracket", 2)), ["time-axis", "rest-positions", "motion-positions"], ["rest-zero-displacement", "motion-position-change"], ["把瞬时点当作静止过程", "换参考系后仍声称绝对静止"]),
            Contract(ArticleScientificFigureCandidateKind.RestZeroVelocityTurningPoint, ["上抛物体", "最高点", "v=0", "a向下", "前后位置不同"], ["trajectory", "object-before", "object-apex", "object-after", "velocity-arrow", "acceleration-arrow", "time-sequence"], 14, Counts(("trajectory", 1), ("object-before", 1), ("object-apex", 1), ("object-after", 1), ("velocity-arrow", 2), ("acceleration-arrow", 1), ("time-sequence", 2)), ["trajectory", "object-before", "object-apex", "object-after"], ["apex-velocity-zero", "gravity-downward", "positions-change"], ["把最高点画成停留状态", "把 v=0 当成 a=0"]),
            Contract(ArticleScientificFigureCandidateKind.RestStateComparison, ["静止", "匀速直线运动", "变速运动", "v=0", "高阶导数"], ["state-lane", "rest-lane", "uniform-lane", "accelerated-lane", "position-trace", "derivative-note"], 20, Counts(("state-lane", 3), ("rest-lane", 1), ("uniform-lane", 1), ("accelerated-lane", 1), ("position-trace", 6), ("derivative-note", 3)), ["rest-lane", "uniform-lane", "accelerated-lane", "position-trace"], ["rest-special-case", "zero-velocity-not-rest"], ["把三种状态仅用颜色区分", "把高阶导数符号当作瞬时静止判据"])),
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
