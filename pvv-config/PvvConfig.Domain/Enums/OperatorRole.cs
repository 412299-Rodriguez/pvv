namespace PvvConfig.Domain.Enums;

public enum OperatorRole
{
    /// <summary>Operator scoped to a single company's configuration.</summary>
    CompanyOperator = 0,

    /// <summary>System administrator: manages every company and its operators.</summary>
    SystemAdmin = 1,
}
