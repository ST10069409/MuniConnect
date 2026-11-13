# MuniConnect

MuniConnect is a Harbourford civic-services program written in C#. From the landing window you can report an issue, browse local events and announcements, and follow a service request after the civic desk replies.

The engagement is two-way. You send a report, the desk posts a reply on that same request, and you can answer. A further reply moves the request along, and a reply on a resolved request reopens it.

## Compile and run

You need the .NET 10 SDK. From this folder:

```powershell
cd C:\Users\ditse\StudioProjects\MuniConnect
dotnet test
dotnet run --project MuniConnect.App
```

`dotnet test` checks the data structures and the report flow. `dotnet run` opens the Windows application.

To build a program you can start without the SDK command:

```powershell
dotnet build MuniConnect.App -c Release
```

The program is `MuniConnect.App\bin\Release\net10.0-windows\MuniConnect.App.exe`.

Reports are saved in your Documents folder, in `MuniConnect\muni_connect.json`. Photos are copied into `MuniConnect\photos`. Deleting `muni_connect.json` restores the three sample requests the next time the program starts.

## How to use it

1. Start the program. The landing window shows three tasks: **Report an issue**, **Events and announcements**, and **Service request status**.
2. Open **Report an issue**. Enter a location (or click a place), choose a category, and describe the problem. Categories are Road works, Sanitary, Utilities, Parks, and Public safety.
3. Optionally attach up to four photos. This opens the Windows file picker.
4. Submit. A message shows the reference, for example `HD-1003`, and the civic desk’s first reply. Choose **Track request** or **Back to menu**.
5. Open **Service request status** to see every request, newest first, with its status. Filter by status, or open a request to read the progress timeline and the conversation.
6. Reply in the box at the bottom and choose **Send to the desk**. The desk answers on the same thread and the status moves one step: Received, In review, Assigned, In progress, Resolved. If you reply after it is resolved, the request is reopened.
7. **Check progress** asks the desk for the next update without writing a note. **Advance the most urgent request** moves the open request with the highest severity, and then the one that has been waiting longest.
8. Open **Events and announcements** to see what is coming up. Search by a word such as `market` or `water`. Recent searches are kept, and **Suggested for you** changes to match them. The trail at the bottom is one pass through the week’s listings.
9. **How data is organised** on the landing window explains which structure is holding the live data.

The form shows a desk priority before you submit. Wording such as “leak” or “outage”, a utilities or sanitary category, road works, and places such as a clinic or market all change that score.

## Where the data structures are used

| Structure | What it does in the program |
| --- | --- |
| Hash table | Reports by reference, events by id, and the word index used by search |
| Set | The category list, and the tags that belong to one search hit |
| Queue | New reports wait here until the desk writes the first reply |
| Stack | The most recent search terms |
| Binary heap and priority queue | The desk’s “most urgent” order, and the ranking of search hits and suggestions |
| Binary tree | The questions that turn a report into Critical, High, Standard, or Routine |
| Binary search tree | Exact event titles |
| AVL tree | The status list, newest first, and the sorted event dictionary |
| Sorted dictionary | Upcoming events in time order |
| Red-black tree | Open requests ranked for the desk |
| Graph, breadth-first and depth-first search | Related service areas and related event tags |
| Minimum spanning tree | The short trail that links the week’s listings |

The implementations live in `MuniConnect.Core\Dsa`. `CivicRepository` uses them for reports. `EventCatalog` uses them for events, search, and suggestions.
