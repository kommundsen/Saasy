/* @jsxRuntime classic */
const { useState: useState_TB } = React;

function TopBar({ integrator, kind, onSwitch }) {
  return (
    <div style={{
      height: "var(--topbar-h)", background: "var(--paper)",
      borderBottom: "1px solid var(--border-1)", padding: "0 24px",
      display: "flex", alignItems: "center", gap: 16, position: "sticky", top: 0, zIndex: 10,
      backdropFilter: "blur(12px)"
    }}>
      <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
        <img src="../../assets/logo-mark.svg" width={26} height={26} alt="" />
        <span style={{ font: "700 18px/1 var(--font-sans)", letterSpacing: "-0.02em" }}>Saasy</span>
      </div>
      <div style={{
        marginLeft: 24, display: "flex", alignItems: "center", gap: 8,
        padding: "6px 10px", border: "1px solid var(--border-1)", borderRadius: 8,
        background: "var(--paper-2)", cursor: "pointer"
      }} onClick={onSwitch}>
        <Icon d={ICONS.building} size={16} />
        <span className="ui">{integrator}</span>
        <KindBadge kind={kind} />
        <Icon d={ICONS.chevron_d} size={14} />
      </div>
      <div style={{
        marginLeft: 8, flex: 1, maxWidth: 480, display: "flex", alignItems: "center", gap: 8,
        padding: "0 12px", height: 36, border: "1px solid var(--border-1)", borderRadius: 8, background: "var(--paper-2)"
      }}>
        <Icon d={ICONS.search} size={16} />
        <input className="input mono" placeholder="Find a Customer, Plan, or Subscription..."
               style={{ border: "none", background: "transparent", flex: 1, padding: 0, height: 28 }} />
        <span className="caption mono" style={{ background: "var(--paper)", padding: "1px 6px", borderRadius: 4, border: "1px solid var(--border-1)" }}>⌘K</span>
      </div>
      <button className="btn btn-ghost" style={{ marginLeft: "auto" }}>
        <Icon d={ICONS.bell} size={18} />
      </button>
      <div style={{ width: 32, height: 32, borderRadius: 999, background: "var(--ink)", color: "var(--fg-inv)",
                    display: "flex", alignItems: "center", justifyContent: "center", font: "600 13px var(--font-sans)" }}>
        KO
      </div>
    </div>
  );
}

function StatusBar({ kind = "sandbox" }) {
  if (kind !== "sandbox") return null;
  return (
    <div style={{
      height: "var(--statusbar-h)", background: "var(--sandbox-wash)",
      borderBottom: "1px solid var(--border-1)", padding: "0 24px",
      display: "flex", alignItems: "center", gap: 12, color: "var(--sandbox-ink)",
      font: "500 13px var(--font-sans)"
    }}>
      <Icon d={ICONS.flask} size={16} />
      <span>You are looking at a <strong style={{ fontFamily: "var(--font-mono)" }}>SANDBOX</strong> Integrator. Sandbox and production are separate Integrators with no shared data (ADR-0009).</span>
      <a href="#" style={{ marginLeft: "auto", color: "var(--sandbox-ink)", textDecoration: "underline" }}>Switch to production →</a>
    </div>
  );
}

function Sidebar({ active, onNav }) {
  const items = [
    { id: "subs",     icon: "signature", label: "Subscriptions", count: 248 },
    { id: "customers",icon: "users",     label: "Customers",     count: 184 },
    { id: "plans",    icon: "layers",    label: "Plans",         count: 12 },
    { id: "products", icon: "shapes",    label: "Product Types", count: 3 },
    { id: "rollups",  icon: "bars",      label: "Rollups" },
    { id: "invoices", icon: "invoice",   label: "Invoices",      count: 1024 },
    { id: "webhooks", icon: "webhook",   label: "Webhooks" },
    { id: "audit",    icon: "scroll",    label: "Audit Log" },
    { id: "keys",     icon: "key",       label: "API Keys" },
  ];
  return (
    <nav style={{ width: "var(--sidebar-w)", borderRight: "1px solid var(--border-1)",
                  background: "var(--paper)", padding: "16px 12px",
                  position: "sticky", top: "calc(var(--topbar-h) + var(--statusbar-h))",
                  height: "calc(100vh - var(--topbar-h) - var(--statusbar-h))",
                  overflow: "auto", display: "flex", flexDirection: "column", gap: 2 }}>
      {items.map(it => (
        <a key={it.id} href="#" onClick={e => { e.preventDefault(); onNav(it.id); }}
           style={{
             display: "flex", alignItems: "center", gap: 10, padding: "8px 10px",
             borderRadius: 8, color: active === it.id ? "var(--ink)" : "var(--fg-2)",
             background: active === it.id ? "var(--sass-pink-wash)" : "transparent",
             font: "500 14px var(--font-sans)", textDecoration: "none",
             borderLeft: active === it.id ? "2px solid var(--sass-pink)" : "2px solid transparent",
             paddingLeft: 8
           }}>
          <Icon d={ICONS[it.icon]} size={18} />
          <span>{it.label}</span>
          {it.count != null && (
            <span className="mono tabular" style={{ marginLeft: "auto", color: "var(--fg-3)", fontSize: 12 }}>
              {it.count.toLocaleString()}
            </span>
          )}
        </a>
      ))}
      <div style={{ marginTop: "auto", padding: 12, border: "1px solid var(--border-1)", borderRadius: 12, background: "var(--paper-2)" }}>
        <div className="mono-label fg-2">Final Close in</div>
        <div className="mono-num tabular" style={{ fontSize: 22, marginTop: 2 }}>23h 14m</div>
        <div className="caption" style={{ marginTop: 4 }}>2026-05-31 24h Late Event Window.</div>
      </div>
    </nav>
  );
}

window.TopBar = TopBar;
window.StatusBar = StatusBar;
window.Sidebar = Sidebar;
