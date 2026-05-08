/* @jsxRuntime classic */
const { useState: useState_S, useEffect: useEffect_S } = React;

function Capsule({ kind = "default", children, dot = false }) {
  return (
    <span className={`capsule ${kind === "default" ? "" : kind}`}>
      {dot && <span className="live-dot" style={{ animation: kind === "live" ? undefined : "none" }} />}
      {children}
    </span>
  );
}

function KindBadge({ kind = "production" }) {
  return (
    <span className={`kind-badge ${kind}`}>
      {kind === "sandbox" ? "SANDBOX" : "PRODUCTION"}
    </span>
  );
}

function QuotaMeter({ value, quota, dim, threshold = 80 }) {
  const pct = Math.round((value / quota) * 1000) / 10;
  const fill = pct >= 100 ? "over" : pct >= threshold ? "warn" : "";
  const ink = pct >= 100 ? "var(--over-ink)" : pct >= threshold ? "var(--warn-ink)" : "var(--live)";
  return (
    <div className="col gap-2">
      <div className="row gap-3" style={{ alignItems: "baseline" }}>
        <span className="mono" style={{ color: "var(--fg-1)", textTransform: "none", letterSpacing: 0 }}>{dim}</span>
        <span className="mono tabular fg-2" style={{ marginLeft: "auto" }}>
          {value.toLocaleString()} / {quota.toLocaleString()}
        </span>
        <span className="mono-num tabular" style={{ color: ink, minWidth: 56, textAlign: "right" }}>
          {pct >= 100 ? Math.round(pct) : pct}%
        </span>
      </div>
      <div className="quota">
        <div className={`quota-fill ${fill}`} style={{ width: Math.min(pct, 100) + "%" }} />
        {threshold < 100 && (
          <div style={{ position: "absolute", top: -2, bottom: -2, left: threshold + "%", width: 1, background: "var(--ink-3)" }} />
        )}
      </div>
    </div>
  );
}

function StatCard({ label, value, sub, kind }) {
  return (
    <div className="card" style={{ flex: 1, padding: "20px 22px" }}>
      <div className="mono-label fg-2">{label}</div>
      <div className="mono-num tabular" style={{ fontSize: 36, lineHeight: "44px", marginTop: 4, color: "var(--fg-1)" }}>{value}</div>
      <div className="caption" style={{ marginTop: 6, display: "flex", gap: 8, alignItems: "center" }}>
        {kind && <Capsule kind={kind}>{kind === "live" ? "Live" : kind === "warn" ? "80%" : kind}</Capsule>}
        <span>{sub}</span>
      </div>
    </div>
  );
}

window.Capsule = Capsule;
window.KindBadge = KindBadge;
window.QuotaMeter = QuotaMeter;
window.StatCard = StatCard;
