page 50100 "UP Customer Ledger Entries API"
{
    PageType = API;
    APIPublisher = 'techworks';
    APIGroup = 'universityPortal';
    APIVersion = 'v1.0';
    EntityName = 'customerLedgerEntry';
    EntitySetName = 'customerLedgerEntries';
    SourceTable = "Cust. Ledger Entry";
    ODataKeyFields = SystemId;
    Caption = 'customerLedgerEntries';

    // Read-only by design: the portal must never change posted ledger entries
    Editable = false;
    InsertAllowed = false;
    ModifyAllowed = false;
    DeleteAllowed = false;
    DelayedInsert = true;

    layout
    {
        area(Content)
        {
            repeater(Group)
            {
                field(id; Rec.SystemId) { Caption = 'Id'; }
                field(entryNumber; Rec."Entry No.") { Caption = 'Entry No.'; }
                field(customerNumber; Rec."Customer No.") { Caption = 'Customer No.'; }
                field(postingDate; Rec."Posting Date") { Caption = 'Posting Date'; }
                field(documentType; Rec."Document Type") { Caption = 'Document Type'; }
                field(documentNumber; Rec."Document No.") { Caption = 'Document No.'; }
                field(externalDocumentNumber; Rec."External Document No.") { Caption = 'External Document No.'; }
                field(description; Rec.Description) { Caption = 'Description'; }
                field(dueDate; Rec."Due Date") { Caption = 'Due Date'; }
                field(currencyCode; Rec."Currency Code") { Caption = 'Currency Code'; }
                field(amount; Rec.Amount) { Caption = 'Amount'; }
                field(remainingAmount; Rec."Remaining Amount") { Caption = 'Remaining Amount'; }
                field(open; Rec.Open) { Caption = 'Open'; }
                field(closedAtDate; Rec."Closed at Date") { Caption = 'Closed at Date'; }
                field(closedByEntryNumber; Rec."Closed by Entry No.") { Caption = 'Closed by Entry No.'; }
                field(reversed; Rec.Reversed) { Caption = 'Reversed'; }
                field(lastModifiedDateTime; Rec.SystemModifiedAt) { Caption = 'Last Modified'; }
            }
        }
    }

    trigger OnAfterGetRecord()
    begin
        // Amount and Remaining Amount are calculated fields: they are blank unless calculated
        Rec.CalcFields(Amount, "Remaining Amount");
    end;
}