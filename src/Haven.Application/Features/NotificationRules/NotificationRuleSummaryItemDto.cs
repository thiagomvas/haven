namespace Haven.Application.Features.NotificationRules;

public record NotificationRuleSummaryItemDto(
    string Name,
    string I18NKey,
    int RuleCount,
    bool IsOverridden = false,
    int GlobalRuleCount = 0);
