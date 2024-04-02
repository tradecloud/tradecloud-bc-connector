namespace Microsoft.API.V2;

using Microsoft.Integration.Entity;
using Microsoft.Purchases.Document;

// https://yzhums.com/34483/
// https://github.com/yzhums/Release-a-Sales-Order-with-Power-Automate/blob/main/ZYAPIV2SalesOrders.al
// https://businesscentralgeek.com/what-are-bound-actions-in-business-central-apis
page 50100 "APIV2 Purchase Order"
{
    APIVersion = 'v2.0';
    EntityCaption = 'Purchase Order';
    EntitySetCaption = 'Purchase Orders';
    ChangeTrackingAllowed = true;
    DelayedInsert = true;
    EntityName = 'purchaseOrder';
    EntitySetName = 'purchaseOrders';
    ODataKeyFields = "No.";
    PageType = API;
    APIPublisher = 'tradecloud';
    APIGroup = 'connector';
    SourceTable = "Purchase Order Entity Buffer";
    Extensible = false;

    layout
    {
        area(Content)
        {
            repeater(Group)
            {
                field(id; Rec.Id)
                {
                    Caption = 'Id';
                    Editable = false;
                }
                field(number; Rec."No.")
                {
                    Caption = 'No.';
                    Editable = false;
                }
                field(status; Rec.Status)
                {
                    Caption = 'Status';
                    Editable = false;
                }
                field(lastModifiedDateTime; Rec.SystemModifiedAt)
                {
                    Caption = 'Last Modified Date';
                    Editable = false;
                }
            }
        }
    }

    [ServiceEnabled]
    [Scope('Cloud')]
    procedure Release(var ActionContext: WebServiceActionContext)
    var
        PurchaseHeader: Record "Purchase Header";
        ReleasePurchaseDocument: Codeunit "Release Purchase Document";
    begin
        PurchaseHeader.GetBySystemId(Rec.Id);
        ReleasePurchaseDocument.PerformManualRelease(PurchaseHeader);
    end;

    [ServiceEnabled]
    [Scope('Cloud')]
    procedure Reopen(var ActionContext: WebServiceActionContext)
    var
        PurchaseHeader: Record "Purchase Header";
        ReleasePurchaseDocument: Codeunit "Release Purchase Document";
    begin
        PurchaseHeader.GetBySystemId(Rec.Id);
        ReleasePurchaseDocument.PerformManualReopen(PurchaseHeader);
    end;
}
