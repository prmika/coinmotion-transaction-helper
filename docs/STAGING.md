# Test-branch staging (manual deployment)

This is a **synthetic-data staging environment**, not a production deployment. CI builds and exercises the containers. A successful push to `test` publishes commit-SHA-tagged images to GHCR plus a moving `test` tag; nothing on the home server is updated automatically.

## Safety and scope

- The frontend is published to the host on **`127.0.0.1` only**, default port `18080`. The API has no host-published port. Do not change the loopback bind or add an Nginx Proxy Manager host.
- Reach the UI from a client with an SSH local-forwarding tunnel. No public hostname, inbound application port, TLS proxy, or deployment secret is required.
- The app has **no authentication**. Only use synthetic CSVs; anyone who can access the server locally or establish an SSH tunnel can use it.
- Each CSV is capped at 10 MiB (10,485,760 bytes); the API request allowance includes 64 KiB of multipart overhead. Nginx allows up to 11 MiB to pass the request to the API.
- Generated ZIP/PDF reports are stored only in process memory, expire after 60 minutes, and are removed on the first successful download. A container restart also discards them. The cleanup worker sweeps at one-minute intervals; requests at or after expiry are rejected immediately.
- No real tax/financial data, persistent storage, or production tax-policy change is in scope.

## CI and image publishing

`.github/workflows/ci.yml` runs the backend tests and frontend lint/build for pushes to `main`/`test` and PRs targeting those branches. After both pass, `container-smoke` builds the Docker images and tests the UI shell, `/healthz`, synthetic CSV upload, ZIP/PDF contents, and one-time download behavior. Only a successful **push to `test`** proceeds to `publish-images`; PRs and `main` do not publish.

The publish job has `packages: write` and pushes both images under the full validated commit SHA and the mutable `test` tag:

- `ghcr.io/prmika/coinmotion-transaction-helper-api:<commit-sha>`
- `ghcr.io/prmika/coinmotion-transaction-helper-web:<commit-sha>`

New GHCR packages may initially be private. Before unauthenticated host pulls, use GitHub's package settings to confirm/set **both** packages to Public. Treat the commit SHA as the release identifier, inspect the Actions run for that same SHA, and prefer those SHA tags over `:test` when operating the server. The `:test` tags are conveniences and move on subsequent successful pushes.

## One-time host setup (operator-run; not performed by CI)

1. After the change has landed on `test`, wait for the matching `CI` run to finish successfully, including `container-smoke` and `publish-images`. Record the full commit SHA from that run.
2. Confirm both GHCR packages are public and that the two SHA-tagged images exist.
3. On `home-server`, check that the candidate port is unused before starting:

   ```sh
   ss -lnt '( sport = :18080 )'
   ```

   If it is already occupied, choose an unused host port by setting `STAGING_PORT` below. The published address must remain loopback-only.
4. Use an already-authorized administrative shell for Docker operations. The normal operational account was observed without permission to access `/var/run/docker.sock`; do not add it to the `docker` group just for this deployment.
5. Create an isolated deployment directory and fetch the Compose file from the exact validated commit (replace `<FULL_SHA>` with the value from step 1):

   ```sh
   install -d -m 0750 /opt/stacks/coinmotion-staging
   cd /opt/stacks/coinmotion-staging
   curl --fail --location \
     https://raw.githubusercontent.com/prmika/coinmotion-transaction-helper/<FULL_SHA>/deploy/staging/compose.yaml \
     --output compose.yaml
   chmod 0640 compose.yaml
   ```
6. Create `/opt/stacks/coinmotion-staging/.env` with the same SHA for both image tags:

   ```dotenv
   API_IMAGE=ghcr.io/prmika/coinmotion-transaction-helper-api:<FULL_SHA>
   WEB_IMAGE=ghcr.io/prmika/coinmotion-transaction-helper-web:<FULL_SHA>
   REPORT_RETENTION_MINUTES=60
   STAGING_PORT=18080
   ```

   The file contains no credentials. Set its permissions to `0640`. Replace `STAGING_PORT` only if step 3 found a conflict.
7. Validate, pull, and start the stack manually:

   ```sh
   docker compose --env-file .env -f compose.yaml config
   docker compose --env-file .env -f compose.yaml pull
   docker compose --env-file .env -f compose.yaml up -d --no-build --remove-orphans
   docker compose --env-file .env -f compose.yaml ps
   curl --fail http://127.0.0.1:18080/healthz
   ```

   Use the authorized Docker command path available on the host (for example, a root/admin shell). Do not put credentials into the Compose file or shell history. Expected health JSON is `{"status":"ok"}`.

## Client access through SSH

From the client, open a local tunnel to the server's loopback-bound port. Use the server's documented Tailscale address or another already-working SSH destination, and substitute the correct SSH account and local port:

```sh
ssh -N -L 127.0.0.1:18080:127.0.0.1:18080 <ssh-user>@100.80.230.33
```

Keep that terminal open, then browse to <http://127.0.0.1:18080>. If using a different `STAGING_PORT`, change the remote-side port in the SSH command and browser URL accordingly. SSH encrypts the forwarded traffic; the app itself does not provide login or HTTPS. Do not publish the port, add a proxy host, or upload real exports.

## Manual updates, verification, and rollback

For each update, repeat the successful `test`-branch CI/publish checks, back up the current `compose.yaml` and `.env`, fetch the Compose file from the new validated commit SHA, and change both image tags in `.env` to that same SHA. Then run `config`, `pull`, `up -d --no-build --remove-orphans`, `ps`, and the loopback health check above. Confirm the UI through the SSH tunnel and run the synthetic smoke path if useful.

To roll back, restore the prior Compose file and `.env` (including the previous SHA tags), then run `docker compose ... pull` and `up -d --no-build`. This is manual; no CI job can pull or restart services on the host. To stop staging, use `docker compose --env-file .env -f compose.yaml down` from the deployment directory. There are no named data volumes to retain.

## Local container smoke test

With Docker Engine available and Compose v2 installed, from the repository root:

```sh
API_IMAGE=ghcr.io/local/coinmotion-transaction-helper-api:smoke \
WEB_IMAGE=ghcr.io/local/coinmotion-transaction-helper-web:smoke \
STAGING_PORT=18080 REPORT_RETENTION_MINUTES=60 \
  docker compose --file deploy/staging/compose.yaml up --detach --build --wait --wait-timeout 120

tests/smoke/container-smoke.sh

docker compose --file deploy/staging/compose.yaml down --volumes --remove-orphans
```

The smoke script uses only `tests/fixtures/synthetic-coinmotion.csv` and checks that the returned archive contains a readable PDF. It never uses or stores a real account export.
