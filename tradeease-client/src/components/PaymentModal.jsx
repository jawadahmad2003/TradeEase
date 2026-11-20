import React, { useState, useEffect } from "react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { api } from "../api/client";
import { useToast } from "./Toast";

// Use the exact publishable key you configured in backend appsettings.json
const stripePromise = loadStripe("pk_test_51TiectFCh5hxVdDoxvNbiCsVEz6ibUUP0FwSwa8rOIcC1iFKHAXIJxmArAAZGnnfxs0FV1irzKKcJb0H0zLjKaTd004g1x7ui8");

function CheckoutForm({ onSuccess, onCancel, amount }) {
  const stripe = useStripe();
  const elements = useElements();
  const showToast = useToast();
  const [processing, setProcessing] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!stripe || !elements) return;

    setProcessing(true);

    const { error, paymentIntent } = await stripe.confirmPayment({
      elements,
      redirect: "if_required", // We handle the result without redirecting if possible
    });

    if (error) {
      showToast(error.message || "Payment failed", "error");
      setProcessing(false);
    } else if (paymentIntent && paymentIntent.status === "succeeded") {
      showToast("Payment successful!");
      onSuccess();
    } else {
      showToast("Payment processing.", "info");
      onSuccess();
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ width: "100%" }}>
      <PaymentElement />
      <div className="modal-footer" style={{ marginTop: "24px" }}>
        <button type="button" className="btn-ghost" onClick={onCancel} disabled={processing}>
          Cancel
        </button>
        <button type="submit" className="btn-primary" disabled={!stripe || processing}>
          {processing ? "Processing..." : `Pay $${amount.toFixed(2)} & Book`}
        </button>
      </div>
    </form>
  );
}

export default function PaymentModal({ amount = 50, currency = "usd", onSuccess, onCancel }) {
  const [clientSecret, setClientSecret] = useState("");
  const showToast = useToast();

  useEffect(() => {
    // Create PaymentIntent as soon as the modal loads
    api.createPaymentIntent(amount, currency)
      .then((data) => setClientSecret(data.clientSecret))
      .catch((err) => {
        showToast(err.message || "Failed to initialize payment.", "error");
        onCancel();
      });
  }, [amount, currency]);

  return (
    <div className="modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onCancel(); }}>
      <div className="modal-card" style={{ maxWidth: "500px" }}>
        <div className="modal-header">
          <h3>Booking Fee Payment</h3>
          <button type="button" className="icon-btn" onClick={onCancel}>✕</button>
        </div>
        <div className="modal-body">
          <p style={{ marginBottom: "20px", color: "var(--muted)" }}>
            A standard booking fee of <strong>${amount.toFixed(2)} {currency.toUpperCase()}</strong> is required to finalize your shipment.
          </p>
          {!clientSecret ? (
            <p>Loading payment gateway...</p>
          ) : (
            <Elements stripe={stripePromise} options={{ clientSecret, appearance: { theme: 'stripe' } }}>
              <CheckoutForm onSuccess={onSuccess} onCancel={onCancel} amount={amount} />
            </Elements>
          )}
        </div>
      </div>
    </div>
  );
}
