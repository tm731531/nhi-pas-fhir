# Expose the demo under your own domain (Cloudflare Tunnel)

Goal: `https://<your-subdomain>` → the demo on `:5099`, via a Cloudflare Tunnel. A common topology is
a tunnel running on a small **edge host** that forwards to the **host running the demo** over the LAN;
adjust to your setup.

Placeholders below: `<DEMO_HOST_IP>` = LAN IP of the machine running the demo · `<your-subdomain>` =
the hostname you want (e.g. `pas.example.com`) · `<tunnel>` = your Cloudflare tunnel name/ID.

## On the demo host

1. Keep the demo up on `:5099`, bound to `0.0.0.0` → use `host/nhi-pas-demo.service` (see its header).
   Verify: `curl -s -o /dev/null -w '%{http_code}\n' http://<DEMO_HOST_IP>:5099/` → `200`.
2. (For the CQL 查 step) start the engine: `cd cql-engine/server && docker compose up -d && node load-libraries.mjs`.

## On the tunnel host — add one ingress rule

Edit your cloudflared config (`~/.cloudflared/config.yml` or `/etc/cloudflared/config.yml`). **Before**
the final `- service: http_status:404` catch-all, add:

```yaml
  - hostname: <your-subdomain>
    service: http://<DEMO_HOST_IP>:5099
```

⚠️ This file may route other services too — **back it up first** (`cp config.yml config.yml.bak`) and
append only this one rule; don't touch existing entries.

## DNS route

```bash
cloudflared tunnel route dns <tunnel> <your-subdomain>
```

Or add a CNAME in the Cloudflare dashboard: `<your-subdomain>` → `<tunnel-id>.cfargotunnel.com` (Proxied).

## Apply + verify

```bash
systemctl restart cloudflared        # or however your tunnel runs
curl -sI https://<your-subdomain> | head -1     # expect HTTP/2 200
```

## Security (optional)

Anyone with the URL can hit the demo (it uses only **fabricated data**, low risk). To limit access, add
a Cloudflare Zero Trust → Access policy for `<your-subdomain>` (e.g. restrict to your email).
