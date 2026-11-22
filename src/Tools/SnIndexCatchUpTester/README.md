# Interactive index catch-up test

[Magyar változat](readme-hu.md)

This .NET 8 console app walks you through backing up, restoring, and verifying an index. Use a test instance configured with a local Lucene index and `AddSenseNetExecutionFrameIndexCatchUpProvider()`. The SQL-backed TokenAuth and LocalAuth sample hosts store the index in `App_Data/LocalIndex` under the web app's working directory.

Set either `SN_INDEX_CATCHUP_API_KEY` or `SN_INDEX_CATCHUP_BEARER_TOKEN` with administrator credentials. From the repository root, run the console app with the test instance URL and an unused run name.

```powershell
dotnet run --project src/Tools/SnIndexCatchUpTester -- https://localhost:44362 demo-01
```

The app pauses at each manual step. Press **Enter** after completing the step. To cancel the test, type **q** and press **Enter**.

| Step | What to do |
| --- | --- |
| 1–2 | Start the web app. The tester creates the `/Root/Content/index-catchup-<run-name>` test folder, prepares the test content, and waits for the index commit. |
| 3–4 | Shut down the web app and back up the entire index directory to a separate location. |
| 5–6 | Restart the web app. The tester updates one folder 24 times, discards a draft after 12 updates, and creates 8 temporary folders. It then creates 210 child folders in a subtree, deletes the subtree containing the temporary folders and the subtree at the destination path, and moves the large subtree to the vacated destination. |
| 7–9 | Shut down the web app. Set aside the current index directory, restore the backup from step 4, and start the web app with the SQL database from the end of step 6. |
| 10 | The tester verifies the index documents, checks that the deleted version IDs are absent, and checks the activity checkpoint. It prints an `OK` or `FAIL` line for each check. |
| 11 | If verification passes, the tester deletes its test folder and waits for you to tidy up the index copies you set aside. If verification fails, type `t` and press Enter to delete the test folder, or press Enter to keep it. |

At the end of step 6, the tester prints the expected activity range. Verification checks that the discarded draft and temporary folders are absent, that the updated folder reflects its final state, that all 212 versions in the moved subtree have the new paths, and that the untouched control item still matches.

For a detailed comparison, enable the `Index` trace and look for `Execution frame catch-up from A… through A…`. The tester prints the `VersionId` values for the repeatedly updated folder, the discarded draft, and the eight temporary folders so you can find them in the index write logs. The 24 folder updates can be coalesced into a single `Upsert by VersionId` entry. The draft and temporary folders appear in `Delete by VersionId` entries. The `processed frame` lines mark the frame boundaries. When processing `AddTree` for the large subtree, the 200-version batch limit splits the 212 versions across at least two `Upsert by VersionId` lines.

The `Skipped activities` and `Coalesced activities` lists contain activity IDs. The `Delete by VersionId` and `Upsert by VersionId` lists show the batch's index operations. Each list compresses runs of three or more consecutive IDs into ranges, such as `[100, 101, 105-107]`.

The app exits with `0` on success, `1` on failure, and `130` on cancellation. If the test is canceled or an API call fails after the test folder was created, the app prints the folder's path so you can inspect or remove it later.
