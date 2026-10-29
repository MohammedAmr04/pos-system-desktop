# Branch Agent

`backend-cs/BranchAgent` is a small Windows process that runs beside the local branch backend.
It pushes pending local operations to the central API and pulls central change-log entries back to
the branch. It persists its pull cursor in `data/sync-cursor.txt`, so restarting the machine does
not lose progress.

The agent authenticates with the branch key only. There is no user access token and no refresh
cycle. It reads its central URL, branch id, and branch key from the local backend every loop, so a
branch configured through the setup screen starts syncing without editing environment variables or
restarting the agent.

## Branch setup

1. On the central node, create the branch. The central generates the branch key once and optionally
   creates the first terminal.
2. Copy the setup code shown after branch creation.
3. On the branch computer, complete the first-run setup and paste the setup code. The code fills the
   central URL, branch id, terminal id, and branch key.
4. `start.bat` starts `pos-branch-agent.exe` automatically. The agent idles until setup is complete,
   then begins pushing and pulling.

Optional environment overrides:

```text
POS_CENTRAL_URL=https://central.example.com
POS_BRANCH_LOCAL_URL=http://localhost:3001
POS_SYNC_POLL_SECONDS=5
```

The local agent endpoints (`/api/local-agent/*`) are loopback-only. The central branch endpoints
(`/api/branch-sync/*`) require `X-Branch-Id` and `X-Branch-Key`.

The central URL must use HTTPS in production so the long-lived branch key is not sent in clear
text. Rotate a branch key from Settings → Branches if it is lost or exposed; the old key stops
working immediately.
