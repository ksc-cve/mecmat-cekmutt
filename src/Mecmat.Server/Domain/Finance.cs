namespace Mecmat.Domain;

public enum AccountKind { FundingSource, Project, BudgetCategory, Wallet, Other }
/// <summary>NRIIS budget lines.</summary>
public enum BudgetCategory { Compensation, Operating, Materials, Equipment, InstitutionFee, Other }
public enum LedgerKind { Income, Transfer, Expense, AdvanceOut, AdvanceReturn, AdvanceClear, Adjustment, Reversal }
public enum MoneyRequestKind { Reimbursement, Payment, Advance, AdvanceClearance, Transfer }
public enum MoneyRequestStatus { Draft, Submitted, InApproval, Approved, Rejected, Returned, Cancelled, Settled }

/// <summary>A node in the money tree. Only leaves carry ledger entries; a parent's balance is
/// the sum of its subtree, computed on read, so totals can never drift (ADR 0004).</summary>
[IdPrefix("ACC")]
public class Account : Entity
{
    public string Code { get; set; } = "";
    public string NameTh { get; set; } = "";
    public string? NameEn { get; set; }
    public string? ParentAccountId { get; set; }
    public AccountKind Kind { get; set; } = AccountKind.Wallet;
    public string? ProjectId { get; set; }
    public BudgetCategory? BudgetCategory { get; set; }
    public string? CustodianPersonId { get; set; }
    public bool AllowNegative { get; set; }
    public string? Description { get; set; }
}

[IdPrefix("ABG")]
public class AccountBudget : Entity
{
    public string AccountId { get; set; } = "";
    public int FiscalYear { get; set; }
    public long Amount { get; set; }
    public string? Note { get; set; }
}

/// <summary>Immutable: corrections are a Reversal entry plus a new entry, never an edit.</summary>
[IdPrefix("LED")]
public class LedgerEntry : Entity
{
    public DateOnly EntryDate { get; set; }
    public LedgerKind Kind { get; set; }
    public string? FromAccountId { get; set; }
    public string? ToAccountId { get; set; }
    public long Amount { get; set; }
    public string Description { get; set; } = "";
    public string? CounterpartyPersonId { get; set; }
    public string? CounterpartyOrganizationId { get; set; }
    public string? CounterpartyText { get; set; }
    public string? MoneyRequestId { get; set; }
    public string? ReversesEntryId { get; set; }
    public BudgetCategory? BudgetCategory { get; set; }
    public string? ReferenceText { get; set; }
}

[IdPrefix("REQ")]
public class MoneyRequest : Entity
{
    public MoneyRequestKind Kind { get; set; }
    public string RequesterPersonId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string? ToAccountId { get; set; }
    public string? ProjectId { get; set; }
    public string Purpose { get; set; } = "";
    public string? PayeePersonId { get; set; }
    public string? PayeeOrganizationId { get; set; }
    public string? PayeeText { get; set; }
    public long TotalAmount { get; set; }
    public DateOnly? NeededBy { get; set; }
    public DateOnly? AdvanceDueDate { get; set; }
    public string? RelatedAdvanceId { get; set; }
    public MoneyRequestStatus Status { get; set; } = MoneyRequestStatus.Draft;
    public string? ApprovalInstanceId { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? SettledBy { get; set; }
}

[IdPrefix("RQL")]
public class MoneyRequestLine : Entity
{
    public string RequestId { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Qty { get; set; } = 1;
    public long UnitPrice { get; set; }
    public long Amount { get; set; }
    public BudgetCategory? BudgetCategory { get; set; }
}
