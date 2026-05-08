/* @jsxRuntime classic */

function PageHeader({ crumbs = [], title, actions }) {
  return (
    <div className="col gap-2" style={{ marginBottom: 24 }}>
      <div className="caption" style={{ display: "flex", gap: 6, alignItems: "center" }}>
        {crumbs.map((c, i) => (
          <React.Fragment key={i}>
            <span style={{ color: i === crumbs.length - 1 ? "var(--fg-1)" : "var(--fg-2)" }}>{c}</span>
            {i < crumbs.length - 1 && <Icon d={ICONS.chevron} size={12} />}
          </React.Fragment>
        ))}
      </div>
      <div className="row" style={{ alignItems: "flex-end", gap: 16 }}>
        <h1 className="h1">{title}</h1>
        <div style={{ marginLeft: "auto", display: "flex", gap: 8 }}>{actions}</div>
      </div>
    </div>
  );
}

// --- Screen 1: Subscriptions list ----------------------------------

function SubsList({ onOpen }) {
  const rows = [
    { id: "sub_a01", customer: "Acme Robotics",   cid: "cust_4F8a93b2", plan: "Pro · v3",     mtd: "USD 1,240.00", state: "live",  pct: 41 },
    { id: "sub_b22", customer: "Bramble Mfg",     cid: "cust_77da12c0", plan: "Pro · v3",     mtd: "USD 980.50",   state: "warn",  pct: 82 },
    { id: "sub_c01", customer: "Conduit Labs",    cid: "cust_a01ff344", plan: "Starter · v1", mtd: "USD 120.00",   state: "over",  pct: 114 },
    { id: "sub_d44", customer: "Driftwood Studio", cid: "cust_3b9e1100", plan: "Pro · v3",    mtd: "USD 2,300.00", state: "live",  pct: 58 },
    { id: "sub_e09", customer: "Estuary Logistics", cid: "cust_887a02e5", plan: "Enterprise · v2", mtd: "USD 14,200.00", state: "live", pct: 12 },
    { id: "sub_f55", customer: "Foreshore Health", cid: "cust_4498d3b1", plan: "Pro · v3",    mtd: "USD 740.20",   state: "warn",  pct: 88 },
  ];
  return (
    <div>
      <PageHeader
        crumbs={["Subscriptions"]}
        title="Subscriptions"
        actions={<>
          <button className="btn btn-secondary"><Icon d={ICONS.filter} size={16}/>Filter</button>
          <button className="btn btn-secondary"><Icon d={ICONS.download} size={16}/>Export</button>
          <button className="btn btn-primary"><Icon d={ICONS.plus} size={16}/>New Subscription</button>
        </>}
      />
      <div className="row gap-3" style={{ marginBottom: 16 }}>
        <StatCard label="Active" value="248" sub="across 4 Plans" kind="live" />
        <StatCard label="MTD revenue" value="USD 142,890" sub="vs USD 128,440 last period" />
        <StatCard label="Threshold crossings" value="14" sub="last 24h · 9 unique Subscriptions" kind="warn" />
        <StatCard label="Late Events" value="892" sub="within window" />
      </div>
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <table className="tbl">
          <thead><tr>
            <th>Customer</th>
            <th>Plan</th>
            <th>Period MTD</th>
            <th>Headline Quota</th>
            <th>State</th>
            <th style={{ width: 40 }}></th>
          </tr></thead>
          <tbody>
            {rows.map(r => (
              <tr key={r.id} style={{ cursor: "pointer" }} onClick={() => onOpen(r)}>
                <td>
                  <div className="ui">{r.customer}</div>
                  <div className="caption mono" style={{ color: "var(--fg-3)" }}>{r.cid}</div>
                </td>
                <td><span className="mono">{r.plan}</span></td>
                <td className="mono tabular" style={{ textAlign: "right" }}>{r.mtd}</td>
                <td style={{ width: 240 }}>
                  <div className="quota" style={{ height: 6 }}>
                    <div className={`quota-fill ${r.state === "over" ? "over" : r.state === "warn" ? "warn" : ""}`}
                         style={{ width: Math.min(r.pct, 100) + "%" }} />
                  </div>
                  <div className="caption mono tabular" style={{ marginTop: 4, color: "var(--fg-2)" }}>{r.pct}% of api_calls</div>
                </td>
                <td>
                  {r.state === "live" && <Capsule kind="live" dot>Live</Capsule>}
                  {r.state === "warn" && <Capsule kind="warn">⚠ 80%</Capsule>}
                  {r.state === "over" && <Capsule kind="over">Overage</Capsule>}
                </td>
                <td><Icon d={ICONS.chevron} size={16} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// --- Screen 2: Subscription detail ----------------------------------

function SubsDetail({ row, onBack }) {
  return (
    <div>
      <PageHeader
        crumbs={["Subscriptions", row?.customer || "Acme Robotics"]}
        title={row?.customer || "Acme Robotics"}
        actions={<>
          <button className="btn btn-ghost" onClick={onBack}>← Back</button>
          <button className="btn btn-secondary"><Icon d={ICONS.refresh} size={16}/>Recompute Rollups</button>
          <button className="btn btn-secondary"><Icon d={ICONS.copy} size={16}/>sub_a01ff344</button>
          <button className="btn btn-ink">Plan Transition</button>
        </>}
      />
      <div className="row gap-3" style={{ marginBottom: 16, alignItems: "stretch" }}>
        <div className="card col gap-3" style={{ flex: 2 }}>
          <div className="mono-label fg-2">Subscription</div>
          <div className="row gap-5" style={{ alignItems: "baseline" }}>
            <div>
              <div className="caption">Bound to</div>
              <div className="ui mono">Pro · v3</div>
            </div>
            <div>
              <div className="caption">Since</div>
              <div className="ui mono">2026-02-14</div>
            </div>
            <div>
              <div className="caption">Currency</div>
              <div className="ui mono">USD</div>
            </div>
            <div>
              <div className="caption">Cycle Anchor</div>
              <div className="ui mono">anchor-day · 14</div>
            </div>
            <div>
              <div className="caption">Interval</div>
              <div className="ui mono">monthly</div>
            </div>
            <div style={{ marginLeft: "auto" }}><Capsule kind="live" dot>Live</Capsule></div>
          </div>
          <div style={{ borderTop: "1px solid var(--border-1)", paddingTop: 14, display: "flex", flexDirection: "column", gap: 14 }}>
            <QuotaMeter value={412008}  quota={1000000} dim="api_calls" threshold={80} />
            <QuotaMeter value={820}     quota={1000}    dim="storage_gb" threshold={80} />
            <QuotaMeter value={11420}   quota={10000}   dim="unique_active_users" threshold={80} />
            <QuotaMeter value={48}      quota={100}     dim="seats (time_weighted_last)" threshold={80} />
          </div>
        </div>
        <div className="card col gap-3" style={{ flex: 1 }}>
          <div className="mono-label fg-2">Period</div>
          <div className="display-2" style={{ fontSize: 36, lineHeight: "40px" }}>23h 14m</div>
          <div className="caption">until Final Close</div>
          <div style={{ borderTop: "1px solid var(--border-1)", paddingTop: 12, display: "flex", flexDirection: "column", gap: 8 }}>
            <div className="row gap-3"><span className="caption">Period start</span><span className="mono tabular" style={{ marginLeft: "auto" }}>2026-05-14 00:00</span></div>
            <div className="row gap-3"><span className="caption">Period Close</span><span className="mono tabular" style={{ marginLeft: "auto" }}>2026-06-14 00:00</span></div>
            <div className="row gap-3"><span className="caption">Final Close (+24h)</span><span className="mono tabular" style={{ marginLeft: "auto" }}>2026-06-15 00:00</span></div>
            <div className="row gap-3"><span className="caption">Timezone</span><span className="mono tabular" style={{ marginLeft: "auto" }}>America/Los_Angeles</span></div>
          </div>
        </div>
      </div>
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <div style={{ padding: "14px 18px", borderBottom: "1px solid var(--border-1)", display: "flex", alignItems: "center", gap: 12 }}>
          <h3 className="h3">Threshold history · this Period</h3>
          <Capsule kind="warn" >2 fired</Capsule>
        </div>
        <table className="tbl">
          <thead><tr><th>Fired</th><th>Dimension</th><th>Threshold</th><th>Rollup at fire</th><th>Webhook</th><th>Latency</th></tr></thead>
          <tbody>
            <tr><td className="mono tabular">2026-05-29 14:02 UTC</td><td className="mono">storage_gb</td><td className="mono">0.80</td><td className="mono tabular" style={{textAlign:"right"}}>800.4</td><td><Capsule kind="live" dot>Delivered</Capsule></td><td className="mono tabular">312ms</td></tr>
            <tr><td className="mono tabular">2026-05-22 03:48 UTC</td><td className="mono">api_calls</td><td className="mono">0.50</td><td className="mono tabular" style={{textAlign:"right"}}>503,201</td><td><Capsule kind="live" dot>Delivered</Capsule></td><td className="mono tabular">198ms</td></tr>
            <tr><td className="mono tabular">2026-05-19 11:20 UTC</td><td className="mono">unique_active_users</td><td className="mono">1.00</td><td className="mono tabular" style={{textAlign:"right"}}>10,001</td><td><Capsule kind="warn">Retrying (3/8)</Capsule></td><td className="mono tabular">—</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}

// --- Screen 3: Plans ----------------------------------

function Plans() {
  return (
    <div>
      <PageHeader crumbs={["Plans"]} title="Plans" actions={<button className="btn btn-primary"><Icon d={ICONS.plus} size={16}/>Create Plan</button>} />
      <div className="card" style={{ padding: 0 }}>
        {[
          { name: "Starter", versions: ["v1 · 2025-01-12 · current"], excl: false, components: ["Flat Fee · USD 49 / mo", "Metered · api_calls · Quota 100k"] },
          { name: "Pro",     versions: ["v3 · 2026-02-14 · current", "v2 · 2025-09-01", "v1 · 2025-01-12"], excl: false, components: ["Flat Fee · USD 499 / mo", "Per-Seat · USD 8 / seat", "Metered · api_calls · Quota 1M · Tiered (graduated)", "Metered · storage_gb · Quota 1k"] },
          { name: "Enterprise", versions: ["v2 · 2026-04-01 · current", "v1 · 2025-06-01"], excl: true, components: ["Flat Fee · USD 4,999 / mo", "Per-Seat · USD 24 / seat", "Commitment · 50M api_calls / yr"] },
        ].map((p, i) => (
          <div key={p.name} style={{ padding: 20, borderTop: i ? "1px solid var(--border-1)" : "none" }}>
            <div className="row gap-3" style={{ alignItems: "baseline", marginBottom: 12 }}>
              <Icon d={ICONS.layers} size={18} />
              <h3 className="h3">{p.name}</h3>
              <span className="caption mono">{p.versions.length} Plan Versions</span>
              {p.excl && <Capsule>exclusive per Customer</Capsule>}
              <span style={{ marginLeft: "auto", display: "flex", gap: 8 }}>
                <button className="btn btn-ghost"><Icon d={ICONS.copy} size={16}/></button>
                <button className="btn btn-secondary">New Plan Version</button>
              </span>
            </div>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1.5fr", gap: 16 }}>
              <div className="col gap-2">
                <div className="mono-label fg-2">Versions</div>
                {p.versions.map(v => <div key={v} className="mono tabular" style={{ color: "var(--fg-1)" }}>{v}</div>)}
              </div>
              <div className="col gap-2">
                <div className="mono-label fg-2">Pricing components · current version</div>
                {p.components.map(c => <div key={c} className="mono" style={{ color: "var(--fg-1)", textTransform: "none", letterSpacing: 0 }}>{c}</div>)}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// --- Screen 4: Webhooks ----------------------------------

function Webhooks() {
  const deliveries = [
    { t: "2026-05-29 14:02:31 UTC", k: "usage.threshold.crossed", url: "https://hooks.acme.com/saasy", code: 200, lat: "312ms", state: "live" },
    { t: "2026-05-29 13:11:08 UTC", k: "invoice.generated",       url: "https://hooks.acme.com/saasy", code: 200, lat: "198ms", state: "live" },
    { t: "2026-05-29 11:20:00 UTC", k: "usage.threshold.crossed", url: "https://hooks.bramble.io/billing", code: 502, lat: "30s",  state: "over" },
    { t: "2026-05-29 09:44:52 UTC", k: "subscription.changed",    url: "https://hooks.driftwood.studio/saasy", code: 204, lat: "84ms",  state: "live" },
    { t: "2026-05-28 22:10:11 UTC", k: "customer.created",        url: "https://hooks.estuary.com/saasy", code: 200, lat: "121ms", state: "live" },
  ];
  return (
    <div>
      <PageHeader crumbs={["Webhooks"]} title="Webhook Subscriptions"
                  actions={<button className="btn btn-primary"><Icon d={ICONS.plus} size={16}/>Add endpoint</button>} />
      <div className="row gap-3" style={{ marginBottom: 16 }}>
        <StatCard label="Endpoints" value="6" sub="across 4 kinds" />
        <StatCard label="Deliveries · 24h" value="2,418" sub="98.4% on first attempt" kind="live" />
        <StatCard label="Failed · retrying" value="12" sub="3 distinct endpoints" kind="warn" />
        <StatCard label="Dead-lettered" value="0" sub="48h" kind="live" />
      </div>
      <div className="card" style={{ padding: 0 }}>
        <div style={{ padding: "14px 18px", borderBottom: "1px solid var(--border-1)", display: "flex", alignItems: "center", gap: 12 }}>
          <h3 className="h3">Recent deliveries</h3>
          <Capsule>last 24h</Capsule>
        </div>
        <table className="tbl">
          <thead><tr><th>When</th><th>Kind</th><th>Endpoint</th><th>HTTP</th><th>Latency</th><th>State</th></tr></thead>
          <tbody>
            {deliveries.map((d, i) => (
              <tr key={i}>
                <td className="mono tabular">{d.t}</td>
                <td><span className="mono" style={{ color: "var(--fg-1)" }}>{d.k}</span></td>
                <td className="mono" style={{ color: "var(--fg-2)" }}>{d.url}</td>
                <td className="mono tabular">{d.code}</td>
                <td className="mono tabular">{d.lat}</td>
                <td>{d.state === "live" ? <Capsule kind="live" dot>Delivered</Capsule> : <Capsule kind="over">Retrying (4/8)</Capsule>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

window.SubsList = SubsList;
window.SubsDetail = SubsDetail;
window.Plans = Plans;
window.Webhooks = Webhooks;
window.PageHeader = PageHeader;
