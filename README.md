# What is Mail2Bug? 

## Overview 
Mail2Bug is a service that allows you to create a bug from an e-mail thread simply by adding a specific recipient to the mail thread. It also keeps the bug up-to-date with information from the mail thread by adding any subsequent replies on the thread as comments to the bug. 


## Why Mail2Bug? 
Simply put, the idea is to reduce friction and effort. Ever been on an e-mail thread where some issue was discussed, when at some point someone asked you to “Please open a bug for this issue”? Mail2Bug tries to reduce the effort associated with that scenario by allowing you to easily create a bug with all the information from the thread with the simple action of adding the relevant alias to the thread. It also keeps the TFS item up to date with any new information from the thread, making sure that information is not lost and is easy to find by looking at the bug

Another common scenario is for support organizations - for automatically creating a ticket for incoming emails, and keeping further communiations on the email thread updated in the ticket.

# Key Features 
* Creates work items from email threads
  * Supports MS Team Foundation Server (TFS)
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

# Building Mail2Bug
  * Requires Visual Studio 2012
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
