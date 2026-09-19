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

The central URL must use HTTPS in production. The token should be dedicated to the branch agent,
rotated periodically, and never committed to source control. The current agent synchronizes the
durable operation/change queues; printer queue integration and a dedicated branch-key handshake
remain follow-up hardening work before production rollout.
