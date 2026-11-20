import { Icon } from "./Icons";

export default function TradeLogo({ showText = true, size = "md", centered = false }) {
  const boxClass = size === "sm" ? "logo-icon-sm" : "logo-icon";
  const textClass = size === "sm" ? "logo-text-sm" : "logo-text";

  return (
    <div className={`trade-logo ${centered ? "trade-logo-center" : ""}`}>
      <div className={boxClass}>
        <Icon name="package" size={size === "sm" ? 16 : 18} />
      </div>
      {showText && <span className={textClass}>TradeEase</span>}
    </div>
  );
}
