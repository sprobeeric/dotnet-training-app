// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

const initializePaymentReceiptCreate = () => {
    const form = document.querySelector("[data-payment-receipt-create-form]");
    const lookupButton = document.querySelector("[data-invoice-lookup-button]");
    const amountPaidInput = document.querySelector("[data-amount-paid-input]");
    const paymentMethodInput = document.querySelector("[data-payment-method-input]");
    const referenceNumberInput = document.querySelector("[data-reference-number-input]");
    const paymentSummary = document.querySelector("[data-payment-summary]");
    const currentPaymentTarget = document.querySelector("[data-current-payment]");
    const balanceAfterPaymentTarget = document.querySelector("[data-balance-after-payment]");

    if (!form) {
        return;
    }

    lookupButton?.addEventListener("click", () => {
        const lookupUrl = form.dataset.lookupUrl || window.location.pathname;
        const invoiceNumberInput = form.querySelector("#InvoiceNumber");

        if (!(invoiceNumberInput instanceof HTMLInputElement)) {
            return;
        }

        const invoiceNumber = invoiceNumberInput.value.trim();
        const query = new URLSearchParams();

        if (invoiceNumber.length > 0) {
            query.set("invoiceNumber", invoiceNumber);
        }

        window.location.assign(query.size > 0 ? `${lookupUrl}?${query.toString()}` : lookupUrl);
    });

    const formatMoney = (value) => value.toLocaleString(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const updatePaymentSummary = () => {
        if (!paymentSummary || !currentPaymentTarget || !balanceAfterPaymentTarget || !(amountPaidInput instanceof HTMLInputElement)) {
            return;
        }

        const remainingBalance = Number(paymentSummary.dataset.remainingBalance || "0");
        const currentPayment = Number(amountPaidInput.value || "0");
        const balanceAfterPayment = remainingBalance - currentPayment;

        currentPaymentTarget.textContent = formatMoney(currentPayment);
        balanceAfterPaymentTarget.textContent = formatMoney(balanceAfterPayment);

        balanceAfterPaymentTarget.classList.remove("text-danger", "text-success");
        balanceAfterPaymentTarget.classList.add(balanceAfterPayment === 0 ? "text-success" : "text-danger");
    };

    const updateReferenceNumberState = () => {
        if (!(paymentMethodInput instanceof HTMLSelectElement) || !(referenceNumberInput instanceof HTMLInputElement)) {
            return;
        }

        const paymentDetailsDisabled = paymentMethodInput.disabled;
        const isCash = paymentMethodInput.value === "Cash";

        referenceNumberInput.disabled = paymentDetailsDisabled || isCash;
        if (isCash) {
            referenceNumberInput.value = "";
        }
    };

    amountPaidInput?.addEventListener("input", updatePaymentSummary);
    paymentMethodInput?.addEventListener("change", updateReferenceNumberState);
    updatePaymentSummary();
    updateReferenceNumberState();
};

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initializePaymentReceiptCreate);
} else {
    initializePaymentReceiptCreate();
}
