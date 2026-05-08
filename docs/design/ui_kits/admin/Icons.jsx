/* @jsxRuntime classic */
const { useState } = React;

function Icon({ d, size = 20, stroke = 1.5 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
         stroke="currentColor" strokeWidth={stroke}
         strokeLinecap="round" strokeLinejoin="round">
      <g dangerouslySetInnerHTML={{ __html: d }} />
    </svg>
  );
}

// Lucide-style icon path bodies (inner markup)
const ICONS = {
  building:    `<rect x="6" y="3" width="12" height="18" rx="1"/><path d="M9 7h6M9 11h6M9 15h2"/>`,
  users:       `<circle cx="9" cy="8" r="3"/><circle cx="17" cy="9" r="2"/><path d="M3 19c0-3 3-5 6-5s6 2 6 5"/><path d="M16 14c2.2 0 4 1.5 4 4"/>`,
  layers:      `<path d="M12 3l9 5-9 5-9-5 9-5z"/><path d="M3 13l9 5 9-5"/>`,
  bars:        `<path d="M3 21V5"/><path d="M7 21v-8"/><path d="M11 21V9"/><path d="M15 21v-5"/><path d="M19 21v-3"/>`,
  triangle:    `<path d="M12 3l9 16H3z"/><path d="M12 9v5"/><circle cx="12" cy="17" r=".7" fill="currentColor"/>`,
  invoice:     `<path d="M16 4h-7a2 2 0 0 0-2 2v12l5-3 5 3V6a2 2 0 0 0-2-2"/><path d="M9 9h7M9 13h5"/>`,
  webhook:     `<path d="M14 17a4 4 0 0 1-7 0"/><circle cx="17" cy="7" r="2.5"/><path d="M5 12a4 4 0 0 1 7-2"/>`,
  shapes:      `<rect x="3" y="3" width="8" height="8" rx="1"/><circle cx="17" cy="7" r="4"/><path d="M3 17l4-4 4 4-4 4z"/>`,
  key:         `<circle cx="9" cy="9" r="3"/><path d="M11 11l9 9"/><circle cx="18" cy="18" r="2"/>`,
  scroll:      `<path d="M5 4h13a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6"/><path d="M8 4v14"/><path d="M11 8h6M11 12h6M11 16h4"/>`,
  search:      `<circle cx="11" cy="11" r="7"/><path d="m20 20-3-3"/>`,
  chevron:     `<path d="m9 18 6-6-6-6"/>`,
  chevron_d:   `<path d="m6 9 6 6 6-6"/>`,
  more:        `<circle cx="5" cy="12" r="1.3"/><circle cx="12" cy="12" r="1.3"/><circle cx="19" cy="12" r="1.3"/>`,
  flask:       `<path d="M9 3v6l-5 8h16l-5-8V3"/><path d="M8 3h8"/>`,
  shield:      `<path d="M12 3 4 7v6c0 4 3.5 7.5 8 8 4.5-.5 8-4 8-8V7z"/><path d="m9 12 2 2 4-4"/>`,
  filter:      `<path d="M4 6h16M7 12h10M10 18h4"/>`,
  download:    `<path d="M12 4v12"/><path d="m7 11 5 5 5-5"/><path d="M5 20h14"/>`,
  refresh:     `<path d="M3 12a9 9 0 0 1 15-6.7L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-15 6.7L3 16"/><path d="M3 21v-5h5"/>`,
  external:    `<path d="M7 17 17 7"/><path d="M9 7h8v8"/>`,
  plus:        `<path d="M12 5v14M5 12h14"/>`,
  copy:        `<rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V5a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h3"/>`,
  bell:        `<path d="M6 17V11a6 6 0 0 1 12 0v6"/><path d="M4 17h16"/><path d="M10 20a2 2 0 0 0 4 0"/>`,
  rss:         `<path d="M5 5a14 14 0 0 1 14 14"/><path d="M5 11a8 8 0 0 1 8 8"/><circle cx="6" cy="18" r="1.5"/>`,
  signature:   `<path d="M3 17c4-2 5-9 8-9s2 7 6 7c2 0 3-1 4-2"/><path d="M3 21h18"/>`,
};

window.Icon = Icon;
window.ICONS = ICONS;
