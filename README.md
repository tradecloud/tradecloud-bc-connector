# Tradecloud Business Central connector

## Goal

The primary objective of the Tradecloud (TC) Business Central (BC) connector is to master the development of a bridge between TC and various ERP systems that offer an API interface. This entails understanding and crafting the necessary design, workflows, and models essential for integration. It's important to emphasize that this serves as a reference implementation, which means it has not been rigorously proven in a production environment.

## Architecture Design

The connector acts as an intermediary between TC and the ERP system. It is architecturally agnostic and can be deployed on any cloud platform that supports ASP.NET Core, making it independent of both Tradecloud and the ERP system. This design provides an overview of the connector's components and its operational flows. The flows associated with the BC side are complex due to BC's constrained API offerings.

[TC connector design](tc-bc-connector-design.png)

## Functional scope

The current features are:

- send order (updates) from BC to TC
- split order lines in BC and send them to TC
- send order response (updates) from TC to BC
- split ordes lines in TC and send them to BC

On the back log are:

- renewing the BC webhook subscription
- send BC supplier item numbers to TC
- send BC buyer and supplier contacts to TC
- send BC documents to TC
- send TC documents to BC
- adding API specs (swagger) for the end points

## Technical scope

### ASP.NET Core

The connector uses the free, cross platform and open source [ASP.NET Core framework](https://dotnet.microsoft.com/en-us/apps/aspnet) and the [C# programming language](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/), further relying on:

- the [MVC framework](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview) in ASP.NET Core, which provides dependency injection and controllers that can receive requests.
- the standard HTTP client, which is included in ASP.NET Core.
- the [Json.NET](http://Json.NET) library, which is the de facto .NET JSON library.
- Azure configuration & logging extensions.

### Azure App Service

The PoC has been deployed on Azure App Service as an example, with Github Actions facilitating its build and deployment. The technical Azure setup is detailed in [Azure set up](#azure-set-up).
For local execution, the PoC is designed to run seamlessly using ASP.NET Core, Visual Code, specific Visual Code extensions, and ngrok. Instructions for this setup are outlined in [Local set up](#local-set-up).

### Microsoft Oauth

The connector uses the OAuth 2.0 [client credentials flow](https://auth0.com/docs/get-started/authentication-and-authorization-flow/client-credentials-flow) also known as [Service-to-Service Authentication](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/administration/automation-apis-using-s2s-authentication) to authenticate against BC.

### BC API's

superThis connector leverages the standard [BC API v2](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/api-reference/v2.0/api/dynamics_purchaseorder_get) and [OData](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/webservices/odata-web-services) endpoints, alongside with an optional BC API extension. There are some differences between the v2 versus OData API: The API v2 works with technical identifiers, like `orderId`, while the OData API works with composed functional keys, like `number`. The connector uses the BC API v2 to get the composed functional key, which is then used with the OData API. The API v2 only contains a subset of by Tradecloud required fields, and the OData API contains all fields.

### BC extension

There is a minimal BC extension in folder `tc-bc-app` which provides a custom API to "reopen" and "release" purchase orders. It uses the [AL programming language](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-programming-in-al) and is deployed and executed inside BC.

### TCP API's

The connector uses [JSON Web Tokens](https://docs.tradecloud1.com/api/introduction/security/authentication#basic-authentication-with-json-web-tokens) to authenticate against TC. It makes use of the [One single delivery per order line](https://docs.tradecloud1.com/api/processes/order/buyer/issue/delivery-schedule#single-delivery) as BC also has only one delivery per order line. It sends orders using the [Issue an order](https://docs.tradecloud1.com/api/processes/order/buyer/issue) API and receives order responses using the [single delivery order event](https://docs.tradecloud1.com/api/processes/order/buyer/receive/single-delivery-order-event) webhook endpoint.

## Set up

### Local set up

- [Install .NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Install Visual Code](https://code.visualstudio.com/)
- [Install Visual Code C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- Clone the repo `git clone git@github.com:tradecloud/tradecloud-bc-connector-poc.git`
- In Visual Code open the repo root folder.
- Use `Ctrl-Shift P` for the command pallete, and filter on `.NET`.
- Use `Ctrl-Shift B` to build.
- Copy `appsettings.Development.template.json` to `appsettings.Development.json` and configure.
- Use `CTRL-F5` to deploy and run the app locally, the webhooks will run on [localhost URL](http://localhost:5213/)
- Use `F5` to deploy and debug the app locally, please note that the app has been run locally at least once before, else you will get an error.
- Use `dotnet run` from the terminal to simulate a production environment, it will detect being stopped and unsubscribe the BC webhook.
- [Setup ngrok](https://ngrok.com/docs/getting-started/) to test webhooks with a local deployment.
- Run `ngrok http http://localhost:5213 --domain earwig-smart-officially.ngrok-free.app` (the ngrok domain is an example)
- Configure `BaseURL` in `appsettings.Development.json` with the ngrok tunnel URL, like `https://earwig-smart-officially.ngrok-free.app` (w/o trailing slash)

### Azure set up

- [Create an App Service](https://portal.azure.com/#view/HubsExtension/BrowseResource/resourceType/Microsoft.Web%2Fsites).
- Select Continuous Delivery en connect to your forked repo.
- Azure will create a Github Actions workflow to Build and deploy ASP.Net Core app to the Azure Web App.
- Azure will commit the Github Actions workflow yaml file in `.github/worklflows`.
- [Github workflow runs](https://github.com/tradecloud/tradecloud-bc-connector-poc/actions) (replace by your forked repo).
- [Github deployments](https://github.com/tradecloud/tradecloud-bc-connector-poc/deployments) (replace by your forked repo).

#### Set up service-to-service authentication

[Set up service-to-service authentication](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/administration/automation-apis-using-s2s-authentication#set-up-service-to-service-authentication)

- [Register an app](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
- Note the `Application (client) ID`
- Note the `Directory (tenant) ID`
- Select the `Authentication` sub menu on the left.
- Select `Add a platform` and `Web`
- Fill in `https://businesscentral.dynamics.com/OAuthLanding.htm`
- Select `Configure`
- Select the `Certificates & Secrets` sub menu on the left.
- Select `New client secret`, copy the value
- Select `API permissions`, `Add a permission`, `Dynamics 365 Business Central`, `Application permissions`
- Add `API.ReadWrite.All`, `Automation.ReadWrite.All` permissions.
- And similar `Delegated permissions`, add `Financials.ReadWrite.All` (This is related to the `D365 FIN. & PURCH.` permission in BC, TODO: test these are really necessary)
- Select `Grant admin consent` and confirm

#### Application settings

[Configure an App Service app](https://learn.microsoft.com/en-us/azure/app-service/configure-common)

You need to set these Name-Value pairs:

- `Connector__MS__ClientId` - Oauth2 service-to-service client id (`Application (client) ID`)
- `Connector__MS__ClientSecret` - Oauth2 service-to-service client secret (`Client secret`)
- `Connector__BC__TenantId` - Business Central TenantId (`Directory (tenant) ID`)
- `Connector__BC__Environment` - Buniness Central environment, for example `Test`
- `Connector__BC__CompanyId` - Business Central Company ID (Fetch via `https://api.businesscentral.dynamics.com/v2.0/<TenantId>/<EnvironmentName>/ODataV4/company('<CompanyName>')`)
- `Connector__BC__CompanyName` - Business Central Company name
- `Connector__BC__SharedSecret` - A random generated password string for the BC webhook hand shake
- `Connector__TC__BuyerId` - Tradecloud buyer ID
- `Connector__TC__IntegrationUsername` - Tradecloud integration user email
- `Connector__TC__IntegrationPassword` - Tradecloud integration user password
- `Connector__TC__WebhookBearerToken` - Tradecloud webhook bearer token

### Business Central set up

#### Set up the Microsoft Entra application in Business Central

[Set up the Microsoft Entra application in Business Central](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/administration/automation-apis-using-s2s-authentication#task-2-set-up-the-microsoft-entra-application-in-)

- In Business Central, search for `Microsoft Entra Applications`, open the page, select `New`.
- Fill in the `Application (client) ID`.
- Enter a `Description` which becomes the user name.
- Add 3 permissions by searching on `pur`: `D365 FIN. & PURCH.`, `D365 PURCH DOC, EDIT`, `D365 PURCH DOC, POST`.
- Select Grant Consent and follow the wizard.

#### Set up the `purchaseOrders` OData API

- in Business Central, search for `Web Service`, open the page.
- search for `purchaseorder` (Object ID 50)
- if its not there, add a Page object with Object Id `50` and Object Name `Purchase Order` and enter Service Name `purchaseOrders`
- Make sure the ODdata v4 URL is generated.

#### Install the Tradecloud extension

You can remotely push and deploy the Tradecloud extension in your sandbox environment:

- In Visual Code: [Install AL Language extension for Microsoft Dynamics 365 Business Central](https://marketplace.visualstudio.com/items?itemName=ms-dynamics-smb.al).
- Open the `tc-bc-app` subfolder in Visual Code.
- Set `environmentName` to your sandbox environment name like `Test` in `.vscode/launch.json`.
- Use `F5` to deploy the extension.

Please note that after a BC version update in a sandbox environment, you need to redeploy the extension.

You can manual upload and install the Tradecloud extension in your production environment on the "Extension Management" page.
