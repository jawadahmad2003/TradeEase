import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

export default function ComplianceWizard({ setActivePage, onUploadDoc }) {
  const showToast = useToast();
  const [steps, setSteps] = useState([]);
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState(null);

  useEffect(() => {
    api.compliance()
      .then((data) => {
        setSteps(data);
        const firstPending = data.find((s) => !s.isCompleted);
        setExpanded(firstPending?.stepId ?? data[0]?.stepId);
      })
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  }, []);

  const doneCount = steps.filter((s) => s.isCompleted).length;
  const pct = steps.length ? Math.round((doneCount / steps.length) * 100) : 0;

  const toggle = async (step) => {
    const newValue = !step.isCompleted;
    try {
      const updated = await api.updateComplianceStep(step.stepId, { isCompleted: newValue, notes: step.notes });
      setSteps((prev) => prev.map((s) => (s.stepId === step.stepId ? updated : s)));
      showToast(newValue ? `${step.stepName} marked complete.` : `${step.stepName} marked pending.`);
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading compliance wizard...</p></div>;

  return (
    <div className="compliance-page registration-wizard">
      <div className="compliance-main">
        <div className="card progress-card">
          <div className="progress-header">
            <div>
              <h2>Trade Registration Compliance Wizard</h2>
              <div className="progress-sub">Step-by-step guide for NTN, SECP, Chamber, WeBOC, and trade bank account</div>
            </div>
            <div className="progress-pct">{pct}%</div>
          </div>
          <div className="progress-bar-wrap">
            <div className="progress-bar-track">
              <div className="progress-bar-fill" style={{ width: `${pct}%` }} />
            </div>
          </div>
          <div className="progress-sub">{doneCount} of {steps.length} steps completed</div>
        </div>

        <div className="wizard-steps">
          {steps.map((step, index) => {
            const isOpen = expanded === step.stepId;
            return (
              <div key={step.stepId} className={`wizard-step-card ${step.isCompleted ? "done" : ""} ${isOpen ? "open" : ""}`}>
                <button className="wizard-step-head" onClick={() => setExpanded(isOpen ? null : step.stepId)}>
                  <div className={`wizard-step-num ${step.isCompleted ? "done" : ""}`}>
                    {step.isCompleted ? "✓" : index + 1}
                  </div>
                  <div className="wizard-step-title-block">
                    <strong>{step.stepName}</strong>
                    <span>Est. time: {step.estimatedTime || "—"}</span>
                  </div>
                  <span className={`wizard-status ${step.isCompleted ? "complete" : "pending"}`}>
                    {step.isCompleted ? "Completed" : "Pending"}
                  </span>
                </button>

                {isOpen && (
                  <div className="wizard-step-body">
                    {step.description && <p>{step.description}</p>}
                    {step.portalUrl && (
                      <a href={step.portalUrl} target="_blank" rel="noreferrer" className="portal-link">
                        Open portal →
                      </a>
                    )}
                    <label className="wizard-check">
                      <input type="checkbox" checked={step.isCompleted} onChange={() => toggle(step)} />
                      <span>I have completed this registration step</span>
                    </label>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      <div className="compliance-side">
        <div className="card tip-card">
          <div className="tip-header">
            <span className="tip-icon">💡</span>
            <strong>Getting Started</strong>
          </div>
          <p className="tip-text">
            Complete steps in order. NTN and SECP are required before WeBOC registration. Progress is saved automatically per user.
          </p>
        </div>

        <div className="card req-docs-card">
          <h3>Registration Checklist</h3>
          <div className="req-docs-list">
            {steps.map((step) => (
              <div key={step.stepId} className="req-doc-item">
                <span className={`req-doc-icon ${step.isCompleted ? "done" : "pending"}`}>
                  {step.isCompleted ? "✓" : "○"}
                </span>
                <div>
                  <div className="req-doc-name">{step.stepName.split(" (")[0]}</div>
                  <div className="req-doc-req">{step.isCompleted ? "Done" : "Required"}</div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="card quick-actions-card">
          <h3>Quick Actions</h3>
          <button type="button" className="action-btn" onClick={() => (onUploadDoc ? onUploadDoc() : setActivePage?.("documents"))}>Upload Document</button>
        </div>
      </div>
    </div>
  );
}
