# What is Mail2Bug? 

## Overview 
Mail2Bug is a service that allows you to create a bug from an e-mail thread simply by adding a specific recipient to the mail thread. It also keeps the bug up-to-date with information from the mail thread by adding any subsequent replies on the thread as comments to the bug. 


## Why Mail2Bug? 
Simply put, the idea is to reduce friction and effort. Ever been on an e-mail thread where some issue was discussed, when at some point someone asked you to “Please open a bug for this issue”? Mail2Bug tries to reduce the effort associated with that scenario by allowing you to easily create a bug with all the information from the thread with the simple action of adding the relevant alias to the thread. It also keeps the TFS item up to date with any new information from the thread, making sure that information is not lost and is easy to find by looking at the bug


# Key Features: 
* Creates work items from email threads
  * Supports MS Team Foundation Server (TFS)
  * Support for other issue tracking platforms may be added in the future
* Updates the work item with further emails on the original thread, keeping it up to date without requiring manual copying of that information to the item
* Adds attachments from the email to the work item (as file attachments)
* Default values for work item fields set by the tool administrator
* Work item fields can be overridden with a specific value by including special text in the message body
  * By specifying an explicit override (i.e. FieldName:FieldValue) 
  * By using "mnemonics", defined in the config, for commonly overridden fields (e.g. the area path)
  * Based on the current date (useful for iteration paths)
  * Based on specific recipients of the message
* Supports Exchange e-mail accounts, including Office365
  * Support for other email platforms may be added in the future
* Supports Unicode text
