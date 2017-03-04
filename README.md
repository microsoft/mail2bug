# What is Mail2Bug?

## Overview
Mail2Bug is a service that allows you to create a bug from an e-mail thread simply by adding a specific recipient to the mail thread. It also keeps the bug up-to-date with information from the mail thread by adding any subsequent replies on the thread as comments to the bug.


## Why Mail2Bug?
Simply put, the idea is to reduce friction and effort. Ever been on an e-mail thread where some issue was discussed, when at some point someone asked you to “Please open a bug for this issue”? Mail2Bug tries to reduce the effort associated with that scenario by allowing you to easily create a bug with all the information from the thread with the simple action of adding the relevant alias to the thread. It also keeps the TFS item up to date with any new information from the thread, making sure that information is not lost and is easy to find by looking at the bug

Another common scenario is for support organizations - for automatically creating a ticket for incoming emails, and keeping further communications on the email thread updated in the ticket.

# Key Features
* Creates work items from email threads
  * Supports MS Team Foundation Server (TFS) and Visual Studio Team Services (VSTS)
* Updates the work item with further emails on the original thread, keeping it up to date without requiring manual copying of that information to the item
* Adds attachments from the email to the work item (as file attachments)
* Default values for work item fields set by the tool administrator
* Work item fields can be overridden with a specific value by including special text in the message body
  * By specifying an explicit override (i.e. FieldName:FieldValue)
  * By using "mnemonics", defined in the config, for commonly overridden fields (e.g. the area path)
  * Based on the current date (useful for iteration paths)
* Supports Exchange e-mail accounts, including Office365
  * Requires EWS to be enabled
  * Requires Exchange2010 or newer
* Supports Unicode text
* Secure credential/secrets storage using DPAPI or Azure KeyVault
  * Read this [blog post](https://www.jeff.wilcox.name/2017/02/mail2bug/) on how to set up Mail2Bug as an Azure service

# Usage
Once Mail2Bug is deployed and configured, just add the appropriate email address to the 'To' or 'Cc' line of an email to have a work item created for the thread.
* You can specify an explicit override in the form of `###FieldName:Value` simply by putting that text in the body of the email. This will set the specified field to the specified value.
* You can specify a mnemonic in the form of `@@@mnemonic` simply by putting that text in the body of the email. This will set all the relevant fields defined for the mnemonic. Mnemonics are defined in the configuration by the tool administrator.
* You can "link" a thread to an existing item by putting a string of the form `work item #1234` in the subject or alternatively put a string of the form `!!!work item #1234` in the email body
* The actual format for specifying overrides, mnemonics, and append-only threads is configurable - the format specified above is the standard default configuration

# How to build Mail2Bug
  * Requires Visual Studio 2012 or newer Visual Studio
  * Clone the repository locally
  * Open the solution file (Mail2Bug.sln) in Visual Studio
  * Make sure that NuGet is allowed to download packages automatically
    * This setting is under Tools-\>Options-\>PackageManager-\>General-\>Allow NuGet to download missing packages during build
    * This is needed because the packages are not checked in as part of the project, so they will be synced from the web during the first build
  * Build the solution
  * All the required binaries can will be in the output folders
    * For Mail2Bug itself, all binaries are under `<projectRoot>\Mail2Bug\Bin\(Debug|Release)\...`
    * For the DpapiTool, binaries are under `<projectRoot>\Tools\DpapiTool\Bin\(Debug|Release)\...`
  * See basic setup and configuration instructions in the wiki - [Basic Setup](https://github.com/Microsoft/mail2bug/wiki/Basic-Setup)

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
