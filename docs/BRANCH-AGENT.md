# Branch Agent

`backend-cs/BranchAgent` is a small Windows process that runs beside the local branch backend.
It pushes pending local operations to the central API and pulls central change-log entries back to
the branch. It persists its pull cursor in `data/sync-cursor.txt`, so restarting the machine does
not lose progress.

Required environment variables:

```text
POS_CENTRAL_URL=https://central.example.com
POS_SYNC_TOKEN=<dedicated service token>
```

Optional variables:

```text
POS_BRANCH_LOCAL_URL=http://localhost:3001
POS_SYNC_POLL_SECONDS=5
```

For the Friboles branch, create `branch-agent.env` beside `start.bat` from
`branch-agent.env.example`, set the central URL and branch service token, and
use `POS_BRANCH_ID=friboles` as the installation label.

`start.bat` starts the local POS server first and then starts the BranchAgent
when the required central URL and token are present. The agent also retries
receipt jobs stored in the local `PrintQueue`; a retry uses a private request
header so a failed retry does not create a second queue entry.

The central URL must use HTTPS in production. The token should be dedicated to the branch agent,
rotated periodically, and never committed to source control. Branch filtering on central change
pulls and a dedicated branch-key handshake remain production hardening work.
