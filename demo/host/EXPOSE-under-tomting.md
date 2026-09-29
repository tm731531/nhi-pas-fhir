# 把 demo 掛到同一個網域(<your-subdomain>)

目標:`https://<your-subdomain>` → 這台(<DEMO_HOST_IP>)的 demo :5099,走**現有的 Cloudflare tunnel**
(tunnel 跑在**<edge-host>**,不是這台 .48)。

## 前提(在 .48 這台)

1. demo 常駐在 :5099 且綁 0.0.0.0 → 用 `host/nhi-pas-demo.service`(見該檔頂部安裝步驟)。
   驗證:`curl -s -o /dev/null -w '%{http_code}\n' http://<DEMO_HOST_IP>:5099/` 應為 200。
2. (要看「查」)CQL 引擎 server 也要在:`cd cql-engine/server && docker compose up -d && node load-libraries.mjs`。

## 在 .93(邊緣機)加一條 ingress

編輯 .93 上的 cloudflared 設定(通常 `~/.cloudflared/config.yml` 或 `/etc/cloudflared/config.yml`)。
**在最後那條 `- service: http_status:404` catch-all 之前**,加入:

```yaml
  - hostname: <your-subdomain>
    service: http://<DEMO_HOST_IP>:5099
```

⚠️ 這個檔同時管著 <internal-service>、<internal-service> 等 —— **先備份**(`cp config.yml config.yml.bak`),
只 append 這一條,別動其他行。

## 加 DNS route(讓 pas 這個子網域指向 tunnel)

在 .93 上(有 tunnel 憑證的地方):

```bash
cloudflared tunnel route dns <你的-tunnel-名稱或ID> <your-subdomain>
```

或到 Cloudflare 後台手動加一筆 CNAME:`pas` → `<tunnel-id>.cfargotunnel.com`(Proxied)。

## 套用 + 驗證

```bash
# 重載 cloudflared(看它怎麼跑的,擇一)
systemctl restart cloudflared          # 若是系統服務
# 或 systemctl --user restart cloudflared
# 或 kill 掉再 `cloudflared tunnel run <name>`

# 幾秒後,從外面(手機行動網路也行):
curl -sI https://<your-subdomain> | head -1     # 期待 HTTP/2 200
```

## 安全(選配)

公開後任何人有網址就能打(demo 全是**假資料**,低風險)。要限本人:在 Cloudflare Zero Trust →
Access → 為 `<your-subdomain>` 加一條 policy(限你的 email 才進)。
